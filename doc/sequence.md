# Sequence Diagrams — Mobile & Web

> Tài liệu sequence tổng hợp, có phân nhóm rõ **[MOBILE]** và **[WEB]**.

---

## A. [MOBILE] Sequences

## M-01 Startup Routing (Loading -> Scan/Main/Language)
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant LoadingPage
    participant Pref as Preferences
    participant QrService
    participant DevicePrefApi
    participant Shell

    User->>LoadingPage: Mở app
    LoadingPage->>Pref: Read device_id

    alt Không có DeviceId
      LoadingPage->>Shell: GoToAsync("//ScanPage")
    else Có DeviceId
      LoadingPage->>QrService: IsAccessValid()
      alt QR không hợp lệ
        LoadingPage->>Shell: GoToAsync("//ScanPage")
      else QR hợp lệ
        LoadingPage->>Pref: Load local preference
        alt Có local preference
          LoadingPage->>Shell: GoToAsync("//MainPage")
        else Chưa có local preference
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

## M-02 QR Scan & Verify (camera/gallery)
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant ScanPage
    participant ScanVM
    participant DeviceService
    participant QrService
    participant Shell

    User->>ScanPage: Quét QR hoặc chọn ảnh gallery
    ScanPage->>ScanVM: ScanResultCommand(text)
    ScanVM->>DeviceService: GetOrCreateDeviceId()
    ScanVM->>QrService: VerifyAsync(code, deviceId)

    alt API/network lỗi
      ScanVM-->>User: ErrorMessage "Không thể kết nối"
    else QR không hợp lệ
      ScanVM-->>User: ErrorMessage từ API
    else QR hợp lệ
      ScanVM->>QrService: SaveAccess(expiryAt)
      ScanVM->>Shell: GoToAsync("LanguagePage")
    end
```

## M-03 Chọn Language + Voice và lưu DevicePreference
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
    LanguageService-->>LanguageVM: language list

    User->>LanguageVM: Chọn language
    LanguageVM->>VoiceService: GetVoicesByLanguageAsync(languageId)
    VoiceService-->>LanguageVM: voice list

    User->>LanguageVM: ConfirmSelectionCommand
    LanguageVM->>DevicePrefApi: UpsertAsync(dto)

    alt Success
      LanguageVM->>Shell: GoToAsync("//MapPage")
    else Failed
      LanguageVM-->>User: ErrorMessage
    end
```

## M-04 Map load (cache-first) + pin render
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
        StallService-->>MapVM: mapped stalls
      else
        StallService->>API: GET /api/geo/stalls?deviceId=...
        API-->>StallService: stalls
        StallService-->>MapVM: stalls from API
      end
    end

    MapVM-->>MapPage: PinsRefreshRequested
    MapPage->>MapPage: RenderPins() + RenderCircles()
```

## M-05 Tap pin -> Popup -> Play narration
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant MapPage
    participant StallPopup
    participant MapVM
    participant AudioGuideService

    User->>MapPage: Tap pin stall
    MapPage->>MapVM: SelectStall(stall)
    MapPage->>StallPopup: Init(stall)
    MapPage->>StallPopup: ShowPopupAsync()

    User->>StallPopup: Nhấn "Phát"
    StallPopup->>MapVM: PlayStall(stall)
    MapVM->>AudioGuideService: PlayAsync(audioUrl)
    StallPopup-->>MapPage: CloseAsync()
```

## M-06 Geofence auto-play queue
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
      alt Không đang phát và queue có item
        MapVM->>Audio: PlayAsync(nextAudio)
      end
    end

    Audio-->>MapVM: PlaybackCompleted
    alt Queue còn item
      MapVM->>Audio: PlayAsync(item kế tiếp)
    end
```

## M-07 Background sync + location flush
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

    Note over SyncBg,API: Khi reconnect mạng, trigger sync ngay
```

---

## B. [WEB] Sequences

## W-01 Login
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Browser
    participant WebCtrl as AuthController
    participant ApiClient
    participant API as /api/auth/login

    User->>Browser: Nhập email/password
    Browser->>WebCtrl: POST /Auth/Login
    WebCtrl->>ApiClient: LoginAsync(dto)
    ApiClient->>API: POST /api/auth/login

    alt Login thành công
      API-->>ApiClient: token + refresh token
      ApiClient-->>WebCtrl: success
      WebCtrl-->>Browser: Store token session + Redirect Home
    else Thất bại
      API-->>ApiClient: error
      ApiClient-->>WebCtrl: failed
      WebCtrl-->>Browser: Render lỗi đăng nhập
    end
```

## W-02 Register BusinessOwner
```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Browser
    participant WebCtrl as AuthController
    participant ApiClient
    participant API as /api/auth/register-business-owner

    User->>Browser: Điền form đăng ký
    Browser->>WebCtrl: POST /Auth/Register
    WebCtrl->>ApiClient: RegisterBusinessOwnerAsync(dto)
    ApiClient->>API: POST /api/auth/register-business-owner

    alt Thành công
      API-->>ApiClient: success
      WebCtrl-->>Browser: Redirect /Auth/Login
    else Lỗi validation/trùng
      API-->>ApiClient: error
      WebCtrl-->>Browser: Render lỗi
    end
```

## W-03 Business list/search
```mermaid
sequenceDiagram
    autonumber
    actor Admin
    participant Browser
    participant BusinessCtrl
    participant BusinessApiClient
    participant API as /api/business

    Admin->>Browser: Mở trang Business
    Browser->>BusinessCtrl: GET /Business/Index?page=&search=
    BusinessCtrl->>BusinessApiClient: GetBusinessesAsync(...)
    BusinessApiClient->>API: GET /api/business?... 
    API-->>BusinessApiClient: PagedResult
    BusinessApiClient-->>BusinessCtrl: data
    BusinessCtrl-->>Browser: Render bảng + pagination
```

## W-04 Create/Update Stall
```mermaid
sequenceDiagram
    autonumber
    actor User as Admin/BusinessOwner
    participant Browser
    participant StallCtrl
    participant StallApiClient
    participant API as /api/stall

    User->>Browser: Submit form Stall
    Browser->>StallCtrl: POST /Stall/Create hoặc /Stall/Update
    StallCtrl->>StallApiClient: CreateStallAsync / UpdateStallAsync

    alt Tạo mới
      StallApiClient->>API: POST /api/stall
    else Cập nhật
      StallApiClient->>API: PUT /api/stall/{id}
    end

    alt Success
      API-->>StallApiClient: success
      StallCtrl-->>Browser: Redirect + success message
    else Failed
      API-->>StallApiClient: error
      StallCtrl-->>Browser: Render validation/error
    end
```

## W-05 Stall Location + GeoFence quản lý
```mermaid
sequenceDiagram
    autonumber
    actor Admin
    participant Browser
    participant LocCtrl as StallLocationController
    participant GeoCtrl as StallGeoFenceController
    participant LocApiClient
    participant GeoApiClient
    participant API

    Admin->>Browser: Mở map vị trí stall
    Browser->>LocCtrl: GET /StallLocation/CreateMap
    LocCtrl->>LocApiClient: GetStallsAsync + GetLocationsAsync
    LocApiClient->>API: GET /api/stall + GET /api/stall-location
    API-->>LocApiClient: data map
    LocCtrl-->>Browser: Render map + markers

    Admin->>Browser: Chọn tọa độ và lưu
    Browser->>LocCtrl: POST /StallLocation/Create
    LocCtrl->>LocApiClient: CreateLocationAsync
    LocApiClient->>API: POST /api/stall-location
    API-->>LocApiClient: success

    Admin->>Browser: Tạo geofence
    Browser->>GeoCtrl: POST /StallGeoFence/Create
    GeoCtrl->>GeoApiClient: CreateGeoFenceAsync
    GeoApiClient->>API: POST /api/stall-geofence
    API-->>GeoApiClient: success
```

## W-06 Narration content + audio
```mermaid
sequenceDiagram
    autonumber
    actor User as Admin/BusinessOwner
    participant Browser
    participant NarrationCtrl
    participant NarrationApiClient
    participant API

    User->>Browser: Tạo narration content
    Browser->>NarrationCtrl: POST /Narration/Create
    NarrationCtrl->>NarrationApiClient: CreateContentAsync
    NarrationApiClient->>API: POST /api/stall-narration-content
    API-->>NarrationApiClient: success

    User->>Browser: Upload/Generate audio
    Browser->>NarrationCtrl: POST action audio
    NarrationCtrl->>NarrationApiClient: Upload/Generate
    NarrationApiClient->>API: /api/narration-audio...
    API-->>NarrationApiClient: success/fail
    NarrationCtrl-->>Browser: Render status + danh sách audio
```

## W-07 User/Role Admin
```mermaid
sequenceDiagram
    autonumber
    actor Admin
    participant Browser
    participant AdminCtrl
    participant UserApiClient
    participant API as /api/users

    Admin->>Browser: Mở User Role Management
    Browser->>AdminCtrl: GET /Admin/UserRoleManagement
    AdminCtrl->>UserApiClient: GetUsersAsync + GetRolesAsync
    UserApiClient->>API: GET /api/users + GET /api/users/roles
    API-->>UserApiClient: users/roles
    AdminCtrl-->>Browser: Render bảng user + roles

    Admin->>Browser: Đổi role / toggle active
    Browser->>AdminCtrl: POST action
    AdminCtrl->>UserApiClient: UpdateRoleAsync / ToggleActiveAsync
    UserApiClient->>API: PUT /api/users/{id}/role hoặc /toggle-active
    API-->>UserApiClient: success
    AdminCtrl-->>Browser: Redirect + message
```

## W-08 Subscription order flow
```mermaid
sequenceDiagram
    autonumber
    actor User as BusinessOwner/Admin
    participant Browser
    participant SubCtrl as SubscriptionController
    participant SubOrderApiClient
    participant API as /api/subscription-orders

    User->>Browser: Chọn gói tại Plans
    Browser->>SubCtrl: GET /Subscription/Checkout?plan=...
    SubCtrl-->>Browser: Render checkout

    User->>Browser: Nhập thông tin thanh toán
    Browser->>SubCtrl: POST /Subscription/ProcessPayment
    SubCtrl->>SubOrderApiClient: CreateOrderAsync
    SubOrderApiClient->>API: POST /api/subscription-orders

    alt Thanh toán mock thành công
      API-->>SubOrderApiClient: status Completed
      SubCtrl-->>Browser: Redirect Success
    else Thất bại
      API-->>SubOrderApiClient: status Failed
      SubCtrl-->>Browser: Render lỗi
    end
```

## W-09 QR admin + kiosk auto flow
```mermaid
sequenceDiagram
    autonumber
    actor Admin
    participant Browser
    participant AdminCtrl
    participant QrApiClient
    participant API as /api/qrcodes

    Admin->>Browser: Tạo QR mới (ValidDays)
    Browser->>AdminCtrl: POST /Admin/CreateQrCode
    AdminCtrl->>QrApiClient: CreateQrCodeAsync
    QrApiClient->>API: POST /api/qrcodes
    API-->>QrApiClient: QR detail
    AdminCtrl-->>Browser: Refresh danh sách QR

    Admin->>Browser: Mở Kiosk Auto QR
    loop Poll trạng thái mã QR
      Browser->>AdminCtrl: GET /Admin/AutoQr data
      AdminCtrl->>QrApiClient: GetQrCodesAsync / GetQrCodeImageAsync
      QrApiClient->>API: GET endpoints
      API-->>QrApiClient: trạng thái used/chưa used
      alt QR đã dùng
        AdminCtrl-->>Browser: Tạo QR mới
      else Chưa dùng
        AdminCtrl-->>Browser: Giữ QR hiện tại
      end
    end
```
