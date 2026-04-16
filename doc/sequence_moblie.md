# Sequence Diagrams - Mobile App

Tài liệu này tập trung vẽ sẵn các sequence diagram quan trọng cho phần Mobile (.NET MAUI).

## 1) Startup Routing (Loading -> Scan/Main/Language)
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant LoadingPage
    participant Pref as Preferences
    participant QrService
    participant DevicePrefApi
    participant Shell

    User->>LoadingPage: Mở ứng dụng
    LoadingPage->>Pref: Read device_id

    alt Không có device_id
        LoadingPage->>Shell: GoToAsync("//ScanPage")
    else Có device_id
        LoadingPage->>QrService: IsAccessValid()

        alt QR không hợp lệ/hết hạn
            LoadingPage->>Shell: GoToAsync("//ScanPage")
        else QR còn hạn
            LoadingPage->>Pref: Load local preference
            alt Có local preference
                LoadingPage->>Shell: GoToAsync("//MainPage")
            else Chưa có local
                LoadingPage->>DevicePrefApi: GetAsync(deviceId)
                alt API có preference
                    LoadingPage->>Pref: Save(preference)
                    LoadingPage->>Shell: GoToAsync("//MainPage")
                else API chưa có preference
                    LoadingPage->>Shell: GoToAsync("LanguagePage")
                end
            end
        end
    end
```

## 2) QR Scan & Verify (Camera/Gallery)
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant ScanPage
    participant ScanVM
    participant DeviceService
    participant QrService
    participant Shell

    User->>ScanPage: Quét QR hoặc chọn ảnh từ thư viện
    ScanPage->>ScanVM: ScanResultCommand(text)
    ScanVM->>DeviceService: GetOrCreateDeviceId()
    ScanVM->>QrService: VerifyAsync(code, deviceId)

    alt Mất mạng / timeout
        ScanVM-->>User: ErrorMessage("Không thể kết nối")
    else QR không hợp lệ
        ScanVM-->>User: ErrorMessage(message từ API)
    else QR hợp lệ
        ScanVM->>QrService: SaveAccess(expiryAt)
        ScanVM->>Shell: GoToAsync("LanguagePage")
    end
```

## 3) Chọn ngôn ngữ + giọng đọc và lưu preference
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant LanguagePage
    participant LanguageVM
    participant LanguageService
    participant VoiceService
    participant DevicePrefApi
    participant Shell

    User->>LanguagePage: Mở trang chọn ngôn ngữ
    LanguagePage->>LanguageVM: LoadLanguagesAsync()
    LanguageVM->>LanguageService: GetActiveLanguagesAsync()
    LanguageService-->>LanguageVM: List<Language>

    User->>LanguageVM: Chọn language
    LanguageVM->>VoiceService: GetVoicesByLanguageAsync(languageId)
    VoiceService-->>LanguageVM: List<Voice>

    User->>LanguageVM: ConfirmSelectionCommand
    LanguageVM->>DevicePrefApi: UpsertAsync(dto)

    alt Lưu thành công
        LanguageVM->>Shell: GoToAsync("//MapPage")
    else Lưu thất bại
        LanguageVM-->>User: ErrorMessage
    end
```

## 4) Tải stall (cache-first) và render map
```mermaid
sequenceDiagram
    autonumber
    participant MapPage
    participant MapVM
    participant StallService
    participant LocalRepo as SQLite LocalRepo
    participant API as Geo API

    MapPage->>MapVM: InitializeAsync()
    MapVM->>StallService: GetStallsAsync(forceRefresh=false)

    alt Memory cache còn hạn
        StallService-->>MapVM: cached stalls
    else
        StallService->>LocalRepo: GetAllAsync()
        alt SQLite có dữ liệu
            LocalRepo-->>StallService: local stalls
            StallService-->>MapVM: mapped GeoStallDto
        else
            StallService->>API: GET /api/geo/stalls?deviceId=...
            API-->>StallService: ApiResult<List<GeoStallDto>>
            StallService-->>MapVM: stalls from API
        end
    end

    MapVM-->>MapPage: PinsRefreshRequested
    MapPage->>MapPage: RenderPins() + RenderCircles()
```

## 5) Tap pin -> Popup -> Play narration
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant MapPage
    participant StallPopup
    participant MapVM
    participant AudioGuideService

    User->>MapPage: Tap pin gian hàng
    MapPage->>MapVM: SelectStall(stall)
    MapPage->>StallPopup: Init(stall)
    MapPage->>StallPopup: ShowPopupAsync()

    User->>StallPopup: Nhấn "Phát"
    StallPopup->>MapVM: PlayStall(stall)
    MapVM->>AudioGuideService: PlayAsync(audioUrl)
    StallPopup-->>MapPage: CloseAsync()
```

## 6) Geofence auto-play queue
```mermaid
sequenceDiagram
    autonumber
    participant GPS as GpsPollingService
    participant MapVM
    participant Audio as AudioGuideService

    loop Mỗi tick GPS
        GPS->>MapVM: OnLocationUpdated(lat,lng,accuracy)
        MapVM->>MapVM: CheckGeofencesAsync()
        MapVM->>MapVM: Enqueue stall mới vào vùng

        alt Không đang phát & queue có item
            MapVM->>Audio: PlayAsync(nextAudioUrl)
        end
    end

    Audio-->>MapVM: PlaybackCompleted
    alt Queue còn item
        MapVM->>Audio: PlayAsync(audio kế tiếp)
    end
```

## 7) Background sync + flush location logs
```mermaid
sequenceDiagram
    autonumber
    participant App
    participant SyncBg as SyncBackgroundService
    participant SyncService
    participant LocalRepo as SQLite
    participant AudioCache
    participant LocationLog
    participant API

    App->>SyncBg: Start()
    SyncBg->>SyncService: SyncAsync() (initial nếu online)

    loop Every 3 minutes
        SyncBg->>SyncService: SyncAsync()
        SyncService->>API: GET /api/geo/stalls
        SyncService->>LocalRepo: UpsertBatchAsync(stalls)
        SyncService->>AudioCache: EnsureDownloadedAsync(...)
    end

    loop Every 1 minute
        SyncBg->>LocationLog: FlushAsync()
        LocationLog->>API: POST /api/device-location-log/batch
    end

    Note over SyncBg,API: Khi mạng reconnect, SyncBg trigger sync ngay
```
