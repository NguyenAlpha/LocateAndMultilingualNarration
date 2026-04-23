> Các sequence diagram của app Mobile (.NET MAUI 10.0). Mobile hoàn toàn anonymous — dùng `DeviceId` sinh tự động, không có tài khoản. Ký hiệu: **USER** = khách tham quan, **VM** = ViewModel, **SVC** = Service (Singleton), **API** = ASP.NET Core Web API, **Pref** = `Microsoft.Maui.Storage.Preferences`, **SQLite** = `stalls.db3` via `sqlite-net-pcl`.

| Mã | Tên |
|----|-----|
| [SD-M01](#sd-m01-startup-routing) | Startup Routing (LoadingPage) |
| [SD-M02](#sd-m02-quét-mã-qr) | Quét Mã QR |
| [SD-M03](#sd-m03-chọn-ngôn-ngữ--giọng-đọc) | Chọn Ngôn ngữ & Giọng đọc |
| [SD-M04](#sd-m04-mainpage--điều-hướng-chính) | MainPage & Điều hướng chính |
| [SD-M05](#sd-m05-bản-đồ--cache-first-stall) | Bản đồ & Cache-First Stall |
| [SD-M06](#sd-m06-tap-pin--popup--phát-narration) | Tap pin → Popup → Phát narration |
| [SD-M07](#sd-m07-geofence-auto-play) | Geofence Auto-Play |
| [SD-M08](#sd-m08-background-sync--flush-gps) | Background Sync & Flush GPS |
| [SD-M09](#sd-m09-tour-flow) | Tour Flow (chọn → bắt đầu → hoàn tất) |
| [SD-M10](#sd-m10-pull-cờ-reset-thiết-bị) | Pull cờ Reset Thiết bị |
| [SD-M11](#sd-m11-offline-notification--app-lifecycle) | Offline Notification & App Lifecycle |
| [SD-M12](#sd-m12-stall-list-page) | Stall List Page |

---

### SD-M01: Startup Routing

```mermaid
sequenceDiagram
    actor USER as Khách
    participant PAGE as LoadingPage
    participant PREF as Preferences
    participant QR as QrService
    participant LP as LocalPreferenceService
    participant API as DevicePreferenceApiService
    participant Shell

    USER->>PAGE: Mở app
    PAGE->>PREF: Read "device_id" (raw, không qua DeviceService)

    alt device_id null (app cài lần đầu)
        PAGE->>Shell: GoToAsync("//ScanPage")
    else Có device_id
        PAGE->>QR: IsAccessValid()
        Note over QR: Đọc "qr_verified" + "qr_expiry" từ Preferences,<br/>so sánh với DateTimeOffset.UtcNow

        alt QR không hợp lệ / hết hạn
            PAGE->>Shell: GoToAsync("//ScanPage")
        else QR còn hạn
            PAGE->>LP: Load()
            alt Có local preference
                Note over PAGE: Không gọi API — tối ưu cold start
                PAGE->>Shell: GoToAsync("//MainPage")
            else Chưa có local preference
                PAGE->>API: GetAsync(deviceId)
                alt API trả về preference
                    PAGE->>LP: Save(preference)
                    PAGE->>Shell: GoToAsync("//MainPage")
                else 404 / chưa có
                    PAGE->>Shell: GoToAsync(nameof(LanguagePage))
                end
            end
        end
    end
```

---

### SD-M02: Quét Mã QR

```mermaid
sequenceDiagram
    actor USER as Khách
    participant PAGE as ScanPage
    participant VM as ScanViewModel
    participant DEV as DeviceService
    participant QR as QrService
    participant API as POST /api/qrcodes/verify
    participant Shell

    Note over VM: Dùng ICommand + Command thủ công<br/>(không [RelayCommand])

    alt Quét camera
        USER->>PAGE: ZXing detect QR code
        PAGE->>VM: ScanResultCommand.Execute(text)
    else Chọn ảnh từ thư viện
        USER->>PAGE: Nhấn nút gallery
        PAGE->>VM: PickImageFromGalleryCommand
        VM->>VM: MediaPicker → decode ảnh
    end

    VM->>DEV: GetOrCreateDeviceId()
    Note over DEV: Đây là NƠI ĐẦU TIÊN sinh device_id<br/>(LoadingPage chỉ read raw, không create)
    VM->>QR: VerifyAsync(code, deviceId) với timeout 5s

    alt Timeout (>5s) / network error
        VM-->>USER: ErrorMessage "Không thể kết nối"
    else API trả isValid=false
        VM-->>USER: ErrorMessage từ API<br/>(mã đã dùng / không tồn tại)
    else isValid=true
        API-->>QR: {expiryAt}
        QR->>QR: SaveAccess(expiryAt) — ghi "qr_verified"/"qr_expiry"
        VM->>Shell: GoToAsync(nameof(LanguagePage))
        Note over Shell: Route tương đối (không //) —<br/>ScanPage vẫn trên stack
    end
```

---

### SD-M03: Chọn Ngôn ngữ & Giọng đọc

```mermaid
sequenceDiagram
    actor USER as Khách
    participant PAGE as LanguagePage
    participant VM as LanguageViewModel
    participant LS as LanguageService
    participant VS as VoiceService
    participant DEV as DeviceService
    participant API_LANG as GET /api/languages/active
    participant API_VOICE as GET /api/tts-voice-profiles/active
    participant API_PREF as POST /api/device-preference
    participant LP as LocalPreferenceService
    participant Shell

    Note over VM: INotifyPropertyChanged thủ công (không ObservableObject)

    USER->>PAGE: Mở LanguagePage
    PAGE->>VM: LoadLanguagesAsync()
    VM->>LS: GetActiveLanguagesAsync()
    Note over LS: Cache 15 phút (memory)
    LS->>API_LANG: (miss cache) GET active languages
    API_LANG-->>LS: List<Language>
    LS-->>VM: List<Language>

    USER->>VM: Chọn language
    VM->>VS: GetVoicesByLanguageAsync(languageId)
    VS->>API_VOICE: GET /api/tts-voice-profiles/active?languageId=...
    API_VOICE-->>VS: List<Voice>
    VS-->>VM: List<Voice>

    USER->>VM: ConfirmSelectionCommand
    VM->>DEV: GetOrCreateDeviceId()
    VM->>API_PREF: UpsertAsync(dto)
    Note over API_PREF: API trả DevicePreferenceDetailDto

    alt Lưu thành công
        API_PREF-->>VM: DevicePreferenceDetailDto
        Note over VM,LP: Overload Shared DTO tự gọi LocalPreferenceService.Save(dto)<br/>— VM không phải gọi trực tiếp
        VM->>Shell: GoToAsync("//MapPage")
    else Lưu thất bại
        VM-->>USER: ErrorMessage
    end
```

---

### SD-M04: MainPage & Điều hướng chính

```mermaid
sequenceDiagram
    actor USER as Khách
    participant PAGE as MainPage
    participant VM as MainViewModel
    participant SS as StallService
    participant QR as QrService
    participant Shell

    USER->>PAGE: Vào MainPage sau Loading/Language
    PAGE->>VM: Init
    VM->>SS: GetAllStallsAsync() — cache-first
    SS-->>VM: _allStalls (memory)
    VM->>VM: Paging client-side, PageSize = 3 featured
    VM-->>PAGE: FeaturedStalls hiển thị

    alt Nhấn "Bản đồ"
        USER->>VM: MapCommand
        VM->>Shell: GoToAsync("//MapPage")
    else Nhấn "Tour"
        USER->>VM: ToursCommand
        VM->>Shell: GoToAsync("//TourListPage")
    else Nhấn "Ngôn ngữ"
        USER->>VM: LanguageCommand
        VM->>Shell: GoToAsync(nameof(LanguagePage))
    else Nhấn "Đăng xuất"
        USER->>VM: LogoutCommand
        VM->>QR: ClearAccess()
        Note over QR: Xóa "qr_verified" + "qr_expiry"
        VM->>Shell: GoToAsync("//ScanPage")
    end
```

---

### SD-M05: Bản đồ & Cache-First Stall

```mermaid
sequenceDiagram
    participant PAGE as MapPage
    participant VM as MapViewModel
    participant SYNC as SyncService
    participant SS as StallService
    participant REPO as LocalStallRepository
    participant API as GET /api/geo/stalls
    participant GPS as GpsPollingService
    participant AUD as AudioGuideService

    Note over VM: State = Uninitialized / Syncing / Loading / Ready / Error<br/>_readyGate: SemaphoreSlim(1,1)

    PAGE->>VM: OnAppearing → EnsureReadyAsync(forceReload=false)
    VM->>VM: _readyGate.WaitAsync()

    alt forceReload = false
        VM->>SYNC: EnsureSyncedAsync()
        Note over SYNC: Chỉ sync nếu chưa từng sync hoặc cache hết hạn
    else forceReload = true (pull-to-refresh)
        VM->>SYNC: SyncAsync() — force
    end

    SYNC->>API: GET /api/geo/stalls?deviceId=X
    API-->>SYNC: ApiResult<List<GeoStallDto>>
    SYNC->>REPO: UpsertBatchAsync(stalls) — diff check HasChanged

    VM->>SS: GetAllStallsAsync()
    alt Memory cache còn hạn (10 phút)
        SS-->>VM: cached stalls
    else
        SS->>REPO: GetAllAsync()
        REPO-->>SS: local stalls
        SS->>SS: Map → GeoStallDto + update memory cache
        SS-->>VM: stalls
    end

    VM->>VM: State = Ready
    VM-->>PAGE: StallsRefreshRequested event
    PAGE->>PAGE: Render pins + circles (Mapsui)

    %% Subscribe 3 event Singleton
    VM->>AUD: subscribe PlaybackCompleted
    VM->>GPS: subscribe LocationUpdated
    VM->>SYNC: subscribe AudioDownloaded

    Note over PAGE,VM: MapPage.Unloaded → VM.Dispose()<br/>→ unsubscribe cả 3 event tránh memory leak
```

---

### SD-M06: Tap pin → Popup → Phát narration

```mermaid
sequenceDiagram
    actor USER as Khách
    participant PAGE as MapPage
    participant VM as MapViewModel
    participant POPUP as StallPopup
    participant AUD as AudioGuideService
    participant CACHE as AudioCacheService

    USER->>PAGE: Tap pin gian hàng
    PAGE->>VM: OnPinClickedAsync(stall)
    VM->>VM: SelectedStall = stall
    Note over VM: SelectedStall CHỈ được set ở đây<br/>và SelectStall() — không bị geofence chạm
    PAGE->>POPUP: Init(stall) + ShowPopupAsync()
    POPUP-->>USER: Hiển thị thông tin + nút Phát

    USER->>POPUP: Nhấn "Phát"
    POPUP->>POPUP: async void OnPlayClicked
    POPUP->>VM: await PlayStallAsync(stall)
    Note over VM: PlayStallAsync là Task thuần (awaitable),<br/>KHÔNG phải async void

    VM->>CACHE: GetOrDownloadAsync(audioUrl, stallId, lang)
    alt File đã cache local ({AppDataDirectory}/audio/{lang}/{stallId}.mp3)
        CACHE-->>VM: local path
    else Chưa cache
        CACHE->>CACHE: HttpClient "download" (timeout 30s)<br/>tải MP3 từ Blob URL tuyệt đối
        CACHE-->>VM: local path
    end

    VM->>AUD: PlayAsync(localPath)
    POPUP->>POPUP: CloseAsync()

    Note over AUD,VM: Khi phát xong:<br/>AudioGuideService raise PlaybackCompleted<br/>→ VM subscriber xử lý queue geofence nếu có
```

---

### SD-M07: Geofence Auto-Play

```mermaid
sequenceDiagram
    participant GPS as GpsPollingService
    participant VM as MapViewModel
    participant ENGINE as GeofenceEngine
    participant LP as LocalPreferenceService
    participant AUD as AudioGuideService

    Note over ENGINE: Class thuần (không DI), sở hữu bởi MapVM.<br/>State: _triggeredIds, _queue.<br/>Haversine radius Trái Đất 6_371_000m.

    Note over VM,ENGINE: Khi vào tour mode:<br/>VM.SetActiveTourAsync(tourId)<br/>→ ENGINE.SetActiveTour(tourStallIds)<br/>→ Chỉ stall trong set mới auto-trigger

    loop Mỗi GPS update (~1s)
        GPS-->>VM: LocationUpdated(lat, lng, accuracy)
        VM->>ENGINE: CheckAsync(lat, lng, stalls)
        ENGINE->>ENGINE: Haversine tới từng stall, so với radiusMeters

        alt Vào vùng stall mới (chưa trigger trước) AND (tour null HOẶC stall trong tour set)
            ENGINE->>ENGINE: _triggeredIds.Add(stallId)
            ENGINE->>ENGINE: _queue.Enqueue(stall)
            ENGINE-->>VM: Raise AutoPlayRequested(stall) — Func<GeoStallDto, Task>
            Note over VM: Awaitable, không fire-and-forget

            alt Đang phát
                Note over VM: Giữ trong queue, chờ PlaybackCompleted
            else Không phát
                VM->>AUD: PlayAsync(stall.AudioUrl)
            end

            opt Tour mode
                VM->>LP: AddCompletedStop(stallId)
                VM->>VM: RecomputeNextStop()
                alt Hoàn tất mọi stop
                    VM->>VM: CompleteTourAsync() → alert + ClearTourProgress
                end
            end
        end
    end

    AUD-->>VM: PlaybackCompleted
    alt Queue còn item
        VM->>VM: Dequeue next stall
        VM->>AUD: PlayAsync(nextAudioUrl)
    end
```

---

### SD-M08: Background Sync & Flush GPS

```mermaid
sequenceDiagram
    participant APP as App
    participant SB as SyncBackgroundService
    participant SYNC as SyncService
    participant REPO as LocalStallRepository
    participant CACHE as AudioCacheService
    participant LOC as LocationLogService
    participant API_STALL as GET /api/geo/stalls
    participant API_RESET as GET /api/device-preference/reset-flag
    participant API_BATCH as POST /api/device-location-log/batch
    participant CONN as Connectivity

    APP->>SB: Start() (từ App.OnStart và OnResume)
    Note over SB: Không gọi Stop() → gọi CleanupInternal()<br/>(tránh gửi NotifyOffline nhầm lúc restart)
    SB->>CONN: Subscribe ConnectivityChanged
    SB->>SB: Khởi động 2 PeriodicTimer

    par Stall sync timer (3 phút)
        loop Mỗi 3 phút
            SB->>SYNC: SyncAsync()
            Note over SYNC: IsSyncing dùng Interlocked.CompareExchange —<br/>skip nếu đang chạy tick trước
            SYNC->>API_STALL: GET stalls kèm deviceId
            API_STALL-->>SYNC: List<GeoStallDto>
            SYNC->>REPO: UpsertBatchAsync (diff check HasChanged)
            SYNC->>CACHE: EnsureDownloadedAsync (semaphore 3 concurrent)

            Note over SB,API_RESET: Piggyback check reset flag NGAY sau sync
            SB->>API_RESET: GET reset-flag?deviceId=X
            alt needsReset = true
                SB->>SB: Preferences.Clear() + xóa folder audio/
                SB->>APP: GoToAsync("//LoadingPage")
            end
        end
    and Flush GPS timer (20 giây)
        loop Mỗi 20 giây
            SB->>LOC: FlushAsync()
            alt Buffer có điểm
                LOC->>API_BATCH: POST batch (max 500 điểm)
                API_BATCH-->>LOC: OK + API piggyback cập nhật LastSeenAt
                LOC->>LOC: Clear buffer
            end
        end
    end

    CONN-->>SB: ConnectivityChanged(online)
    Note over SB: Capture local _cts trước khi dùng<br/>(tránh race với Stop())
    SB->>SYNC: Trigger SyncAsync() ngay không chờ hết chu kỳ
```

---

### SD-M09: Tour Flow

```mermaid
sequenceDiagram
    actor USER as Khách
    participant MAIN as MainPage
    participant LIST as TourListPage
    participant DETAIL as TourDetailPage
    participant TOUR as TourService
    participant LP as LocalPreferenceService
    participant MAP as MapPage
    participant VM as MapViewModel
    participant ENGINE as GeofenceEngine
    participant API as /api/tours
    participant Shell

    USER->>MAIN: Nhấn "Tour"
    MAIN->>Shell: GoToAsync("//TourListPage")
    LIST->>TOUR: GetToursAsync()
    Note over TOUR: Cache-first MEMORY ONLY (10 phút) — không SQLite
    TOUR->>API: GET /api/tours (AllowAnonymous)
    API-->>TOUR: List<TourListItemDto>
    LIST-->>USER: Hiển thị danh sách tour active

    USER->>LIST: Tap tour
    LIST->>Shell: GoToAsync($"TourDetailPage?tourId={id}")
    Note over DETAIL: [QueryProperty("TourId", "tourId")]
    DETAIL->>TOUR: GetTourDetailAsync(id)
    TOUR->>API: GET /api/tours/{id}
    API-->>TOUR: TourDetailDto kèm stops có tọa độ
    DETAIL-->>USER: Hiển thị stops đã order

    USER->>DETAIL: Nhấn "Bắt đầu"
    DETAIL->>LP: SetActiveTourId(id)
    DETAIL->>Shell: GoToAsync($"//MapPage?tourId={id}")

    MAP->>VM: OnAppearing → ResolveTourAsync()
    alt Query có tourId
        VM->>VM: tourId = query
    else Query rỗng
        VM->>LP: GetActiveTourId() — fallback resume sau kill
    end

    opt tourId != null (tour mode)
        VM->>TOUR: GetTourDetailAsync(tourId)
        VM->>ENGINE: SetActiveTour(tourStallIds)
        VM-->>MAP: TourRouteChanged event
        MAP->>MAP: Vẽ polyline theo sequence stops
    end

    Note over VM,ENGINE: Geofence trigger chỉ cho stall trong tour set (SD-M07)

    loop Đi qua từng stop
        VM->>LP: AddCompletedStop(stallId)
        VM->>VM: RecomputeNextStop()
    end

    alt Hoàn tất mọi stop
        VM->>VM: CompleteTourAsync()
        VM-->>USER: Alert "Hoàn thành tour"
        VM->>LP: ClearTourProgress() — xóa active_tour_id + tour_completed_stops
        VM->>ENGINE: SetActiveTour(null)
    end

    Note over USER,VM: Resume sau kill app:<br/>LoadingPage → MainPage → vào MapPage<br/>GetActiveTourId() còn giá trị → tour mode tự kích hoạt
```

---

### SD-M10: Pull cờ Reset Thiết bị

```mermaid
sequenceDiagram
    participant ADMIN as Web Admin
    participant API_ADMIN as POST /api/device-preference/{id}/reset
    participant API_FLAG as GET /api/device-preference/reset-flag
    participant SB as SyncBackgroundService
    participant PREF as Preferences
    participant FS as FileSystem (AppDataDirectory)
    participant Shell

    Note over ADMIN: Admin bấm Reset trên ActiveDevices view (SD-W19)

    ADMIN->>API_ADMIN: POST reset (AdminOnly)
    API_ADMIN->>API_ADMIN: ExecuteUpdateAsync SET NeedsReset = true

    Note over SB: Không có push — Mobile phải pull

    loop Mỗi 3 phút (piggyback sau stall sync — xem SD-M08)
        SB->>API_FLAG: GET reset-flag?deviceId=X
        API_FLAG->>API_FLAG: Đọc NeedsReset, nếu true thì atomic set về false
        API_FLAG-->>SB: bool

        alt needsReset = true
            SB->>PREF: Preferences.Clear()
            Note over PREF: Xóa tất cả 8 pref_* + device_id + qr_verified + qr_expiry
            SB->>FS: Delete folder audio/
            SB->>Shell: GoToAsync("//LoadingPage")
            Note over Shell: Loading thấy device_id = null → ScanPage<br/>Khách phải quét QR mới để dùng lại
        end
    end
```

---

### SD-M11: Offline Notification & App Lifecycle

```mermaid
sequenceDiagram
    participant APP as App
    participant MAP as MapPage
    participant SB as SyncBackgroundService
    participant API_OFF as POST /api/device-preference/{id}/offline

    Note over APP: App.xaml.cs lifecycle hooks

    APP->>SB: OnStart → Start()
    Note over APP,APP: User đang dùng app

    alt App bị đưa xuống background
        APP->>SB: OnSleep → Stop()
        SB->>API_OFF: POST offline (fire-and-forget)
        Note over API_OFF: API set LastSeenAt = DateTimeOffset.MinValue<br/>→ thiết bị rớt ngay khỏi ActiveDevices list
        SB->>SB: Dispose timers + cts.Cancel()
    end

    alt App quay trở lại foreground
        APP->>SB: OnResume → Start()
        SB->>SB: CleanupInternal() + khởi tạo lại timers
        Note over SB: Sync tick đầu tiên sẽ chạy sau ngắn,<br/>khôi phục LastSeenAt = now
    end

    alt MapPage rời khỏi (back button hoặc nav sang page khác)
        MAP->>SB: OnDisappearing → Stop() (fire-and-forget offline notify)
        Note over MAP: Fix thiết bị đang xem bản đồ<br/>nhưng user nhảy ra page khác
    end
```

---

### SD-M12: Stall List Page

```mermaid
sequenceDiagram
    actor USER as Khách
    participant PAGE as StallListPage
    participant VM as StallListViewModel
    participant SS as StallService
    participant API as GET /api/geo/stalls

    USER->>PAGE: Vào StallListPage
    PAGE->>VM: LoadStallsAsync()
    VM->>SS: GetAllStallsAsync(forceRefresh: true)
    Note over SS: LƯU Ý — forceRefresh=true bỏ qua cache memory/SQLite<br/>→ luôn gọi API mỗi lần vào page
    SS->>API: GET /api/geo/stalls?deviceId=X
    API-->>SS: List<GeoStallDto>
    SS-->>VM: _allStalls

    VM->>VM: Filter client-side theo SearchText + CurrentFilter
    VM->>VM: Paging (PageSize = 10)
    VM-->>PAGE: Hiển thị trang hiện tại

    USER->>PAGE: Gõ vào ô search
    PAGE->>VM: SearchText setter
    Note over VM: Setter trigger LoadStallsAsync MỖI LẦN GÕ<br/>— chưa có debounce (known issue)
    VM->>SS: GetAllStallsAsync(forceRefresh: true)
    VM->>VM: Filter + paging lại
    VM-->>PAGE: Refresh list

    USER->>PAGE: Nhấn Trang trước / Trang sau
    PAGE->>VM: PreviousPageCommand / NextPageCommand
    VM->>VM: Page++/Page-- + paging _allStalls
    VM-->>PAGE: Refresh list
```
