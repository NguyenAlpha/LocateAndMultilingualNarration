> Các sequence diagram mô tả luồng tương tác chính của Mobile App. Ký hiệu: **KTV** = Khách tham quan, **APP** = MAUI App, **SEC** = SecureStorage, **API** = Web API, **DB** = SQLite Cache.

| Mã | Tên |
|----|-----|
| [SD-M01](#sd-m01-khởi-động-ứng-dụng--kiểm-tra-session--deviceid) | Khởi động ứng dụng & kiểm tra Session + DeviceId |
| [SD-M02](#sd-m02-chọn-ngôn-ngữ--giọng-đọc--lưu-devicepreference) | Chọn ngôn ngữ & giọng đọc + lưu DevicePreference |
| [SD-M03](#sd-m03-quét-qr-code-để-dùng-app) | Quét QR Code để dùng app |
| [SD-M04](#sd-m04-tự-động-phát-thuyết-minh-khi-vào-vùng-geofence) | Tự động phát thuyết minh khi vào vùng Geofence |
| [SD-M05](#sd-m05-tải-danh-sách-gian-hàng--cache-first--background-sync) | Tải danh sách gian hàng – Cache-First + Background Sync |
| [SD-M06](#sd-m06-tính-đường-đi--chỉ-đường-osrm) | Tính đường đi & chỉ đường (OSRM) |

---

### SD-M01: Khởi động ứng dụng & kiểm tra Session + DeviceId

```mermaid
sequenceDiagram
    actor KTV as Khách tham quan
    participant SP as StartPage
    participant SS as SessionService
    participant DS as DeviceService
    participant SEC as SecureStorage
    participant DP as DevicePreferenceApiService

    KTV->>SP: Mở ứng dụng
    SP->>SS: Kiểm tra session
    SS-->>SP: Trạng thái session
    SP->>DS: Lấy DeviceId
    DS->>SEC: Đọc DeviceId
    alt DeviceId chưa tồn tại
        DS->>SEC: Sinh và lưu DeviceId mới
    end
    SEC-->>DS: DeviceId hợp lệ
    DS-->>SP: Trả DeviceId
    SP->>DP: GET /api/device-preference/{deviceId}
    alt Có preference
        SP-->>KTV: Điều hướng MainPage
    else Chưa có preference
        SP-->>KTV: Điều hướng LanguagePage
    end
```

---

### SD-M02: Chọn ngôn ngữ & giọng đọc + lưu DevicePreference

```mermaid
sequenceDiagram
    actor KTV as Khách tham quan
    participant LP as LanguagePage
    participant LVM as LanguageViewModel
    participant LS as LanguageService
    participant VP as VoicePage
    participant VS as VoiceService
    participant DPS as DevicePreferenceApiService

    KTV->>LP: Mở chọn ngôn ngữ
    LP->>LVM: LoadLanguagesAsync()
    LVM->>LS: GET /api/languages/active
    LS-->>LVM: Danh sách ngôn ngữ
    LVM-->>LP: Hiển thị danh sách
    KTV->>LP: Chọn ngôn ngữ
    LP-->>VP: Điều hướng VoicePage
    VP->>VS: GET voices theo languageId
    VS-->>VP: Danh sách giọng đọc
    KTV->>VP: Chọn giọng đọc
    VP->>DPS: POST /api/device-preference
    alt Thành công
        DPS-->>VP: Lưu thành công
        VP-->>KTV: Điều hướng MainPage
    else Lỗi mạng/API
        DPS-->>VP: Thất bại
        VP-->>KTV: Thông báo & thử lại
    end
```

---

### SD-M03: Quét QR Code để dùng app

```mermaid
sequenceDiagram
    actor KTV as Khách tham quan
    participant SC as ScanPage
    participant SVM as ScanViewModel
    participant APP as App / MainPage

    KTV->>SC: Quét QR tại khu vực Phố Ẩm Thực
    SC->>SVM: Nhận payload QR
    SVM->>SVM: Xác thực mã QR hợp lệ
    alt QR hợp lệ
        SVM-->>APP: Kích hoạt app / điều hướng MainPage
        APP-->>KTV: Vào app và bắt đầu trải nghiệm
    else QR không hợp lệ
        SVM-->>KTV: Thông báo mã QR không hợp lệ
    end
```

---

### SD-M04: Tự động phát thuyết minh khi vào vùng Geofence

```mermaid
sequenceDiagram
    actor KTV as Khách tham quan
    participant GPS as Geolocation
    participant MAP as MapViewModel
    participant GE as GeofenceEngine
    participant NS as NarrationService
    participant AC as AudioCacheService
    participant AP as AudioPlayer

    loop Mỗi lần GPS update
        GPS-->>MAP: Vị trí hiện tại
        MAP->>GE: Check geofence
        GE->>GE: Tính khoảng cách Haversine
        alt Vào vùng geofence mới
            GE->>NS: Trigger narration(stallId)
            NS->>AC: Kiểm tra audio cache
            alt Có cache local
                NS->>AP: Play local audio
            else Chưa có cache
                NS->>AP: Stream audio từ URL
                NS->>AC: Lưu cache nền
            end
            AP-->>KTV: Phát thuyết minh tự động
        else Không vào vùng
            GE-->>MAP: Không hành động
        end
    end
```

---

### SD-M05: Tải danh sách gian hàng – Cache-First + Background Sync

```mermaid
sequenceDiagram
    actor KTV as Khách tham quan
    participant MP as MainPage
    participant MVM as MainViewModel
    participant ST as StallService
    participant DB as LocalStallRepository
    participant API as GeoController
    participant SYNC as SyncBackgroundService

    KTV->>MP: Mở MainPage
    MP->>MVM: LoadFeaturedStallsAsync()
    MVM->>ST: GetFeaturedStallsAsync()
    ST->>DB: Đọc cache SQLite
    DB-->>ST: Dữ liệu local
    ST-->>MVM: Trả nhanh từ cache
    MVM-->>MP: Render danh sách ban đầu

    par Đồng bộ nền
        ST->>API: GET /api/geo/stalls?deviceId=...
        API-->>ST: Dữ liệu mới
        ST->>DB: Upsert local DB
        ST-->>MVM: Trả dữ liệu đã làm mới
        MVM-->>MP: Refresh UI
    and Timer nền mỗi 3 phút
        SYNC->>ST: Trigger SyncAsync()
        ST->>API: Đồng bộ chênh lệch
        API-->>ST: Delta data
        ST->>DB: Upsert delta
    end
```

---

### SD-M06: Tính đường đi & chỉ đường (OSRM)

```mermaid
sequenceDiagram
    actor KTV as Khách tham quan
    participant SP as StallPopup/DetailPage
    participant MVM as MapViewModel
    participant GPS as Geolocation
    participant OSRM as OSRM Service
    participant MAP as MapPage

    KTV->>SP: Nhấn "Chỉ đường"
    SP->>GPS: Lấy vị trí hiện tại
    GPS-->>SP: Tọa độ hiện tại
    SP->>OSRM: GET route(origin, destination)
    OSRM-->>SP: Polyline + khoảng cách + thời gian
    alt Route tìm được
        SP->>MVM: Vẽ polyline trên map
        MVM->>MAP: Render tuyến đường
        MAP-->>KTV: Hiển thị hành trình + thông tin
    else Không tìm được route
        SP-->>KTV: Hiển thị bản đồ cơ bản + hướng đi đơn giản
    end
```