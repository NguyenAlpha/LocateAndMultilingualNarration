# Developer Notes Mobile App

Tài liệu giải thích luồng hoạt động và các quyết định kỹ thuật của app Mobile (.NET MAUI 10.0). Mobile hoàn toàn anonymous — khách không đăng nhập, app định danh thiết bị bằng `DeviceId` (GUID) sinh tự động.

---

## Mục lục

- [SD-M01: Startup Routing](#sd-m01-startup-routing)
- [SD-M02: Quét Mã QR](#sd-m02-quét-mã-qr)
- [SD-M03: Chọn Ngôn ngữ & Giọng đọc](#sd-m03-chọn-ngôn-ngữ--giọng-đọc)
- [SD-M04: MainPage & Điều hướng chính](#sd-m04-mainpage--điều-hướng-chính)
- [SD-M05: Bản đồ & Cache-First Stall](#sd-m05-bản-đồ--cache-first-stall)
- [SD-M06: Tap pin → Popup → Phát narration](#sd-m06-tap-pin--popup--phát-narration)
- [SD-M07: Geofence Auto-Play](#sd-m07-geofence-auto-play)
- [SD-M08: Background Sync & Flush GPS](#sd-m08-background-sync--flush-gps)
- [SD-M09: Tour Flow](#sd-m09-tour-flow)
- [SD-M10: Pull cờ Reset Thiết bị](#sd-m10-pull-cờ-reset-thiết-bị)
- [SD-M11: Offline Notification & App Lifecycle](#sd-m11-offline-notification--app-lifecycle)
- [SD-M12: Stall List Page](#sd-m12-stall-list-page)

---

## SD-M01: Startup Routing

Khi app khởi động, `LoadingPage` quyết định điều hướng dựa trên ba điều kiện xếp chồng: có `device_id` không, QR còn hạn không, và đã có preference ngôn ngữ chưa.

**Đọc `device_id` raw từ Preferences:** Khác với các màn hình khác gọi `DeviceService.GetOrCreateDeviceId()`, `LoadingPage` đọc trực tiếp `Preferences.Get("device_id", null)` để tránh side-effect sinh mới. Nếu `device_id = null` thì app thực sự ở trạng thái cài lần đầu — chưa quét QR bao giờ — và phải đi `//ScanPage`. `DeviceId` chỉ được sinh lần đầu trong `ScanViewModel` (sau khi có QR hợp lệ) hoặc `LanguageViewModel`.

**QR check trước preference:** Thứ tự cố ý — nếu QR hết hạn thì dù có local preference cũng vô nghĩa, phải quét lại. `QrService.IsAccessValid()` đọc hai khóa `qr_verified` và `qr_expiry` từ Preferences rồi so với `DateTimeOffset.UtcNow`.

**Local preference trước API:** Khi `LocalPreferenceService.Load()` thấy đã có snapshot thì điều hướng thẳng `//MainPage`, **không gọi API** — tối ưu cold start, không phụ thuộc mạng. Chỉ khi local trống mới gọi `DevicePreferenceApiService.GetAsync(deviceId)`: nếu server có thì tải về, nếu không (404) thì đưa vào `LanguagePage` để khách chọn lần đầu.

**Không có reset-flag check ở startup:** Cờ reset do Admin set chỉ được poll bởi `SyncBackgroundService` chạy sau khi vào `MapPage`. `LoadingPage` không cần kiểm — nếu Admin vừa reset, pref và QR đã bị `SyncBackgroundService` clear ở chu kỳ trước đó và đẩy về `LoadingPage`, lúc này `device_id = null` sẽ đưa về `ScanPage` tự nhiên.

---

## SD-M02: Quét Mã QR

`ScanViewModel` là một trong hai ViewModel **không dùng** `[RelayCommand]`/`[ObservableProperty]` của CommunityToolkit (cùng với `LanguageViewModel`) — dùng `ICommand` + `Command` + `INotifyPropertyChanged` thủ công. Đây là nợ kỹ thuật cần refactor nhưng không ảnh hưởng chức năng.

**Hai nguồn QR:** `ScanResultCommand` cho camera (ZXing detect), `PickImageFromGalleryCommand` cho chọn ảnh từ thư viện (MediaPicker → ZXing decode). Cùng đi chung nhánh verify sau khi đã trích được text.

**Timeout 5 giây tách biệt HttpClient 10 giây:** `ScanViewModel.VerifyAsync` bọc thêm `cts.CancelAfter(TimeSpan.FromSeconds(5))`. Lý do: UX quét QR cần phản hồi nhanh — 10 giây là quá lâu để khách đứng chờ, 5 giây đủ để gọi API thông thường nhưng sẽ timeout sớm khi mạng kém, hiển thị "Không thể kết nối" thay vì đứng im.

**Device ID sinh tại ScanViewModel, không phải LoadingPage:** Gọi `DeviceService.GetOrCreateDeviceId()` trước khi verify — đây là nơi đầu tiên tạo và lưu `device_id` vào Preferences. Thiết kế này đảm bảo: chỉ tạo `DeviceId` khi khách thực sự tương tác (quét QR), không tạo âm thầm lúc mở app.

**Route tương đối sang LanguagePage:** Dùng `GoToAsync(nameof(LanguagePage))` thay vì `GoToAsync("//LanguagePage")` — giữ `ScanPage` trên stack để khi khách back trong trường hợp lỗi nửa chừng vẫn quay lại được. `LanguagePage` được đăng ký relative route trong `AppShell.xaml.cs`.

---

## SD-M03: Chọn Ngôn ngữ & Giọng đọc

Trang này chọn cả ngôn ngữ và giọng đọc trong cùng một màn hình (không tách 2 page). Đây là **single source of truth** cho `LocalPreferenceService` — mọi bước lưu preference về sau đều đi qua flow này.

**LanguageService cache 15 phút:** Khác với `StallService` (10 phút) vì danh sách ngôn ngữ ít thay đổi. Cache chỉ ở memory, không persist SQLite — khi app restart thì gọi lại API là đủ nhanh.

**VoiceService load theo languageId:** Mỗi ngôn ngữ có thể có nhiều giọng (nam/nữ, chuẩn/địa phương). Khi khách đổi language, VoiceService gọi lại `/api/tts-voice-profiles/active?languageId=...` — không cache chéo giữa các ngôn ngữ để tránh dirty state.

**Lưu chỉ qua DevicePrefApi, local save ẩn bên trong:** `LanguageViewModel` **chỉ gọi `DevicePreferenceApiService.UpsertAsync`**, không gọi `LocalPreferenceService.Save` trực tiếp. Overload của `UpsertAsync` trong service tự gọi `LocalPreferenceService.Save(dto)` sau khi API trả về — trả lời câu hỏi "tại sao VM không save local": vì service đã làm thay, giữ VM mỏng.

**Navigate sang `//MapPage` không phải MainPage:** Lý do UX — khách vừa thiết lập ngôn ngữ xong, bước tiếp theo thường là xem bản đồ ngay. Từ `MapPage`, `SyncBackgroundService` sẽ tự khởi động và sync stall với ngôn ngữ mới.

---

## SD-M04: MainPage & Điều hướng chính

`MainPage` là home màn hình sau khi đã xác thực QR và chọn ngôn ngữ. Ba nút bottom nav chính (Map/Tour/Ngôn ngữ) + nút Đăng xuất + khu featured stalls.

**Featured stalls PageSize = 3:** Hiển thị 3 gian hàng nổi bật, cho nhấn "Trước/Sau" để xem các bộ 3 khác. Paging **client-side** trên `_allStalls` đã lấy qua `StallService.GetAllStallsAsync()` — không gọi lại API mỗi lần chuyển trang. `StallService` có cache memory 10 phút nên gọi này gần như instant sau lần đầu.

**Đăng xuất = xóa QR access:** `LogoutCommand` gọi `QrService.ClearAccess()` xóa `qr_verified` + `qr_expiry`, rồi `GoToAsync("//ScanPage")`. **Không** xóa language/voice preference — nếu khách quét QR mới lại thì pref cũ vẫn dùng được. Đây là "đăng xuất nhẹ" phù hợp với model anonymous: chỉ vô hiệu hóa mã QR một lần, không reset toàn bộ trạng thái.

**UserName hardcode "Du khách":** Vì không có tài khoản, tên hiển thị chỉ là placeholder. Nếu tương lai có login, đổi ở đây.

---

## SD-M05: Bản đồ & Cache-First Stall

`MapViewModel` là ViewModel phức tạp nhất, quản lý state machine và ba nguồn dữ liệu (memory / SQLite / API) song song.

**State machine 5 state:** `Uninitialized` (chưa vào page bao giờ) → `Syncing` (đang gọi API) → `Loading` (đang query SQLite + map pins) → `Ready` (hiển thị) / `Error` (lỗi sync). Bảo vệ transition bằng `SemaphoreSlim _readyGate(1, 1)` — tránh race khi user quick-tap back rồi forward gây ra 2 lần `EnsureReadyAsync` chạy đồng thời.

**`EnsureReadyAsync(bool forceReload, CancellationToken ct)`:** Method idempotent. `forceReload = false` (default) gọi `SyncService.EnsureSyncedAsync` — chỉ sync nếu chưa từng sync hoặc cache hết hạn; `forceReload = true` (pull-to-refresh) gọi `SyncService.SyncAsync` — force bỏ qua cache. Tách 2 path để cold start nhanh và user refresh vẫn work.

**Cache-first 3 lớp qua StallService:** Memory cache 10 phút → SQLite (`stalls.db3`) → API (`/api/geo/stalls?deviceId=...`). Lớp SQLite đảm bảo offline mode vẫn xem được bản đồ. `LocalStallRepository.UpsertBatchAsync` có **diff check `HasChanged`** trước khi ghi — tránh ghi đè toàn bộ bảng mỗi 3 phút nếu server không có thay đổi thực sự.

**Subscribe 3 event Singleton:** `AudioGuideService.PlaybackCompleted` (cho queue geofence), `GpsPollingService.LocationUpdated` (cho geofence check), `SyncService.AudioDownloaded` (refresh UI khi audio mới cache xong). Vì ba service này Singleton còn VM Transient, **phải unsubscribe trong `Dispose()`** để tránh memory leak — `MapPage.Unloaded` handler gọi `vm.Dispose()`.

---

## SD-M06: Tap pin → Popup → Phát narration

**`SelectedStall` tách rời audio target:** Đây là bug fix quan trọng (commit `22a0179`). Trước đây khi geofence trigger stall B trong lúc khách đang xem popup stall A, `SelectedStall` bị ghi đè thành B làm popup nhảy loạn. Giải pháp: `SelectedStall` chỉ được set từ hai nơi là `SelectStall()` và `OnPinClickedAsync()` (tương tác UI rõ ràng); geofence auto-play đi qua event `AutoPlayRequested` và gọi thẳng `PlayStallAsync(stall)`, không chạm `SelectedStall`.

**PlayStallAsync là Task thuần, không async void:** `async void` chỉ dành cho event handler UI (như `OnPlayClicked` trong popup). ViewModel method phải `Task`/`Task<T>` để có thể await và bắt exception. `StallPopup.OnPlayClicked` là `async void` (đúng vì là button handler) nhưng trong đó gọi `await _vm.PlayStallAsync(stall)` trong try/catch — nếu PlayStallAsync là `async void` thì try/catch này không bắt được lỗi.

**AudioCacheService + HttpClient "download":** Named client `"download"` riêng với timeout 30 giây (khác `"ApiHttp"` 10s) vì file audio có thể vài MB trên mạng chậm. Lưu tại `{AppDataDirectory}/audio/{lang}/{stallId}.mp3` — phân theo ngôn ngữ để đổi language không phải xóa cache cũ. Download URL tuyệt đối từ Azure Blob (không có BaseAddress) — đó là lý do client này không set `BaseAddress`.

---

## SD-M07: Geofence Auto-Play

**`GeofenceEngine` là class thuần, không DI:** Được khởi tạo và sở hữu bởi `MapViewModel`. Tách khỏi VM sau commit `22a0179` để tránh bug "SelectedStall ghi đè" (xem SD-M06). State nội bộ: `_triggeredIds` (set các stall đã trigger trong session này, không trigger lại dù khách đứng trong vùng) và `_queue` (stall chờ phát).

**Haversine với bán kính Trái Đất 6_371_000m:** Dùng metre để khớp với `radiusMeters` của stall (thường 8–30m). Không dùng SQL GEOGRAPHY ở phía server — cả API và Mobile đều tính Haversine ở tầng ứng dụng.

**Event `AutoPlayRequested` là `Func<GeoStallDto, Task>` awaitable:** Không phải `Action` hay `EventHandler` — subscriber (MapViewModel) có thể await để đảm bảo xử lý xong một stall mới tính tới cái tiếp theo. Quan trọng với `AudioGuideService.PlayAsync` cần lock tuần tự.

**Tour mode filter:** `SetActiveTour(IEnumerable<Guid>?)` nhận null = tắt tour mode (trigger tất cả stall), hoặc tập Guid = chỉ trigger auto-play cho stall trong tập này. Stall ngoài tour vẫn render pin trên map và tap để phát thủ công được — chỉ không auto-trigger khi geofence.

**Queue khi đang phát:** Nếu `AudioGuideService.IsPlaying = true` khi trigger stall mới, stall được enqueue thay vì bị drop. Khi `PlaybackCompleted` raise, MapVM dequeue tiếp. Đây là lý do subscribe `PlaybackCompleted` ở SD-M05.

---

## SD-M08: Background Sync & Flush GPS

`SyncBackgroundService` là orchestrator của mọi tác vụ nền: sync stall (3 phút), flush GPS (20 giây), pull reset flag (piggyback), và trigger qua ConnectivityChanged.

**Start() gọi `CleanupInternal()` chứ không `Stop()`:** Tinh tế nhưng quan trọng. `Stop()` gửi `offline` notification (xem SD-M11); còn `CleanupInternal()` chỉ dispose timers và cts nội bộ. Khi App.OnResume gọi `Start()` lại thì không muốn gửi offline nhầm (lúc này thiết bị đang online trở lại), nên phải cleanup mà không notify.

**Stall sync 3 phút vs Flush GPS 20 giây:** Hai chu kỳ khác nhau có lý do rõ: stall ít đổi (3 phút đủ), còn GPS cần bắn dày để heatmap mượt và `LastSeenAt` cập nhật thường xuyên (piggyback heartbeat — xem API SD-A09).

**`IsSyncing` atomic qua Interlocked:** `SyncService.IsSyncing` dùng `Interlocked.CompareExchange(ref _isSyncing, 1, 0)` để claim — nếu trả về 1 tức là đang có instance khác chạy, skip tick này. `Volatile.Read/Write` cho các field read-heavy. Đây là fix từ commit `22a0179`, trước đây chỉ là `bool` thường có race.

**Piggyback reset flag check NGAY sau sync stall tick:** Không phải timer riêng. Sau mỗi `SyncAsync()` thành công, `SyncBackgroundService` gọi luôn `GET /api/device-preference/reset-flag?deviceId=...` — lý do: nếu vừa sync thì chắc chắn online, check flag cùng lúc tiết kiệm một chu kỳ HTTP.

**ConnectivityChanged capture local `_cts`:** Event handler của `Connectivity.ConnectivityChanged` phải `var cts = _cts;` trước khi dùng trong callback — nếu dùng `_cts` trực tiếp mà lúc đó `Stop()` đang chạy và null-out `_cts` thì NullReferenceException. Fix từ commit `22a0179`.

**AudioCacheService semaphore 3 concurrent:** `SyncService.EnsureDownloadedAsync` tải audio cho nhiều stall song song nhưng giới hạn 3 để không quá tải băng thông di động.

---

## SD-M09: Tour Flow

**TourService cache memory only (10 phút), không SQLite:** Tour ít hơn stall nhiều (thường < 20), cold start gọi lại API không đắt. Không lưu SQLite để đơn giản — khi app restart thì tour info lấy lại từ API, mà `SetActiveTourId` đã lưu id nên vẫn resume được tour đang chạy.

**QueryProperty cho tourId:** `TourDetailPage` và `MapPage` đều có `[QueryProperty("TourId", "tourId")]`. Shell tự parse query string và set property — đây là pattern MAUI chuẩn để truyền tham số giữa page, thay thế constructor parameter.

**Resume sau kill app:** `LocalPreferenceService.SetActiveTourId(Guid)` + `AddCompletedStop(Guid)` lưu vào Preferences. Khi app bị kill rồi mở lại: `LoadingPage` → `MainPage` → khách vào `MapPage`; `MapPage.OnAppearing` → `ResolveTourAsync()` đọc `QueryProperty` trước (rỗng vì không có nav), fallback `GetActiveTourId()` từ Preferences — tìm được id → tự kích hoạt tour mode.

**Hoàn tất tour:** Khi `AddCompletedStop` đủ mọi stall trong tour, `CompleteTourAsync` hiện alert rồi gọi `ClearTourProgress()` xóa cả hai khóa (`pref_active_tour_id` + `pref_tour_completed_stops`) và `GeofenceEngine.SetActiveTour(null)`. Khách về lại hành vi bình thường — tour sau muốn chạy lại phải vào TourDetailPage bấm Bắt đầu lần nữa.

**Polyline qua TourRouteChanged event:** MapVM raise event khi vào tour mode, MapPage subscribe để vẽ `Mapsui` polyline theo sequence stops. Event này không truyền qua Messaging Center — direct event subscription vì chỉ có 1 subscriber.

---

## SD-M10: Pull cờ Reset Thiết bị

Pattern "pull-based flag" cho push từ Admin → Mobile. Vì app mobile không kết nối liên tục (có thể offline, background, hoặc bị kill), Admin không gọi thẳng được — phải để cờ trong DB rồi Mobile tự pull.

**Check piggyback sau stall sync:** Xem SD-M08 — không có timer riêng cho reset flag. Tần suất check = tần suất sync stall = 3 phút. Đánh đổi: phản hồi reset chậm tối đa 3 phút, nhưng tiết kiệm 1 chu kỳ HTTP và đơn giản hóa code.

**Atomic clear phía API:** API set flag về false ngay khi Mobile đọc thành công (xem API SD-A10). Điều này quan trọng: nếu Mobile crash sau khi đọc cờ mà chưa kịp clear Preferences, lần sync tiếp không trigger reset lần hai (cờ đã false). Nhưng nếu Mobile lỡ miss hoàn toàn (app tắt luôn), Admin cần bấm Reset lần nữa — chấp nhận được vì Admin thấy ngay trên dashboard thiết bị đã offline.

**Xóa cả Preferences lẫn audio folder:** `Preferences.Clear()` xóa toàn bộ 10 khóa (device_id + 8 pref_* + qr_*). Thêm bước xóa folder `{AppDataDirectory}/audio/` vì có thể audio file được tạo với URL cũ của Blob đã thay đổi. Sau đó `GoToAsync("//LoadingPage")` — Loading sẽ thấy `device_id = null` và đá về `ScanPage`.

---

## SD-M11: Offline Notification & App Lifecycle

**Fire-and-forget offline notify:** Khi `Stop()` được gọi, `SyncBackgroundService` gửi `POST /api/device-preference/{id}/offline` không await. Lý do: nếu await mà mạng chậm, `App.OnSleep` sẽ block 30 giây — Android/iOS có thể kill app sớm. Chấp nhận mất notification trong trường hợp tệ (network die lúc vừa sleep), API đã có fallback time-based.

**Ba điểm trigger `Stop()`:**
- `App.OnSleep` — app bị đưa xuống background (home button, task switcher)
- `MapPage.OnDisappearing` — khách rời MapPage sang page khác (vẫn trong app)
- Reset flag detected — trước khi điều hướng về LoadingPage

**`App.OnResume` không thực sự resume state mà khởi động lại:** Gọi `Start()` → `CleanupInternal()` + tạo timers mới. Sync tick đầu tiên chạy ngay (không chờ hết 3 phút) để khôi phục `LastSeenAt = now`. Đây là lý do dashboard Admin thấy thiết bị "online lại" trong vài giây sau khi khách mở app từ background.

**Tại sao MapPage.OnDisappearing cũng gọi Stop?** Nếu khách rời MapPage (ví dụ vào TourListPage), GPS polling dừng nhưng sync stall có thể vẫn cần. Hiện tại code chọn: rời MapPage = coi như rời flow chính, stop luôn để báo offline. Đây là đánh đổi UX — khi khách quay lại MapPage, sync sẽ chạy lại từ đầu. Có thể cân nhắc refactor tương lai nếu cần sync liên tục ở các page khác.

---

## SD-M12: Stall List Page

**`forceRefresh: true` mỗi lần load — known issue:** `StallListViewModel.LoadStallsAsync` gọi `GetAllStallsAsync(forceRefresh: true)`, bỏ qua cache memory/SQLite. Lý do lịch sử: muốn đảm bảo list luôn fresh khi khách vào page. Hệ quả: mỗi lần vào page đều gọi API dù MapPage vừa sync xong cùng data. Có thể refactor để dùng cache chung với MapPage.

**SearchText setter trigger LoadStallsAsync mỗi lần gõ:** Chưa có debounce — gõ "pizza" sẽ trigger 5 request API (`p`, `pi`, `piz`, `pizz`, `pizza`). Với `forceRefresh: true` thì càng tốn băng thông. Thêm debounce 300ms là fix nhỏ nên làm.

**Filter client-side:** Sau khi có `_allStalls` từ API, filter `SearchText` + `CurrentFilter` trên memory rồi phân trang. Server không có endpoint search — chủ ý vì dataset stall nhỏ (vài chục), filter client-side đủ nhanh và đơn giản.

**`CurrentFilter = "All"` placeholder:** Hiện chưa dùng — dự phòng cho filter theo category/business trong tương lai. Không xóa vì XAML đã bind sẵn.
