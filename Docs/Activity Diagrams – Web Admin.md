## 20. Activity Diagrams – Web Admin

> Dùng Mermaid `flowchart TD`. Ký hiệu: hình thoi `{}` = decision, hình chữ nhật bo góc `([])` = start/end, hình chữ nhật `[]` = activity, hình thoi kép `{{}}` = fork/join.

| Mã | Tên |
|----|-----|
| [AD-W01](#ad-w01-đăng-nhập) | Đăng nhập |
| [AD-W02](#ad-w02-đăng-ký-businessowner) | Đăng ký BusinessOwner |
| [AD-W03](#ad-w03-tạo--cập-nhật-business) | Tạo / Cập nhật Business |
| [AD-W04](#ad-w04-vô-hiệu-hóa-business) | Vô hiệu hóa Business |
| [AD-W05](#ad-w05-tạo--cập-nhật--vô-hiệu-hóa-stall) | Tạo / Cập nhật / Vô hiệu hóa Stall |
| [AD-W06](#ad-w06-đặt-vị-trí-stall-trên-bản-đồ) | Đặt vị trí Stall trên bản đồ |
| [AD-W07](#ad-w07-upload--cập-nhật--xóa-media-stall) | Upload / Cập nhật / Xóa Media Stall |
| [AD-W08](#ad-w08-tạo--cập-nhật-geofence) | Tạo / Cập nhật GeoFence |
| [AD-W09](#ad-w09-tạo--cập-nhật--toggle-narration-content) | Tạo / Cập nhật / Toggle Narration Content |
| [AD-W10](#ad-w10-xem-chi-tiết-narration-content--cập-nhật-script) | Xem chi tiết Narration Content + Cập nhật script |

---

### AD-W01: Đăng nhập

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Người dùng truy cập /Auth/Login]
    B --> C[Hiển thị form đăng nhập]
    C --> D[Nhập email + password]
    D --> E[Nhấn Đăng nhập\nPOST /Auth/Login]
    E --> F{ModelState\nhợp lệ?}
    F -- Không --> G[Hiển thị lỗi validation trên form]
    G --> D
    F -- Có --> H[Gọi POST /api/auth/login]
    H --> I{API trả về\nsuccess?}
    I -- Không --> J[Hiển thị lỗi:\nSai email hoặc mật khẩu]
    J --> D
    I -- Có --> K[Nhận accessToken + refreshToken]
    K --> L[Lưu token vào session\nStoreToken]
    L --> M[Redirect /Home/Index]
    M --> N([Kết thúc])
```

---

### AD-W02: Đăng ký BusinessOwner

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Người dùng truy cập /Auth/Register]
    B --> C[Hiển thị form đăng ký]
    C --> D[Nhập username, email, password, phone]
    D --> E[Nhấn Đăng ký\nPOST /Auth/Register]
    E --> F{ModelState\nhợp lệ?}
    F -- Không --> G[Hiển thị lỗi validation trên form]
    G --> D
    F -- Có --> H[Gọi POST /api/auth/register-business-owner]
    H --> I{API trả về\nsuccess?}
    I -- Không --> J{Loại lỗi?}
    J -- Email đã tồn tại --> K[Hiển thị:\nEmail đã được đăng ký]
    J -- Lỗi khác --> L[Hiển thị thông báo lỗi chung]
    K --> D
    L --> D
    I -- Có --> M[Tài khoản tạo thành công\nRole = BusinessOwner]
    M --> N[Redirect /Auth/Login]
    N --> O([Kết thúc])
```

---

### AD-W03: Tạo / Cập nhật Business

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào /Business/Index]
    B --> C[Gọi GET /api/business\nvới page, pageSize, search]
    C --> D[Hiển thị danh sách Business\n+ phân trang]
    D --> E{Admin chọn\nhành động?}

    E -- Tìm kiếm --> F[Nhập từ khóa search]
    F --> C

    E -- Tạo mới --> G[Mở modal Create]
    G --> H[Điền Name, TaxCode,\nContactEmail, ContactPhone]
    H --> I[Nhấn Lưu\nPOST /Business/Create]
    I --> J{ModelState\nhợp lệ?}
    J -- Không --> K[Giữ modal mở\nHiển thị lỗi từng trường]
    K --> H
    J -- Có --> L[Gọi POST /api/business]
    L --> M{API success?}
    M -- Không --> N[Giữ modal mở\nHiển thị lỗi API]
    N --> H
    M -- Có --> O[TempData: Tạo business thành công]
    O --> P[Redirect /Business/Index]
    P --> C

    E -- Cập nhật --> Q[Mở modal Edit\nvới dữ liệu hiện tại]
    Q --> R[Chỉnh sửa thông tin]
    R --> S[Nhấn Lưu\nPOST /Business/Update]
    S --> T{ModelState\nhợp lệ?}
    T -- Không --> U[Giữ modal Edit mở\nHiển thị lỗi]
    U --> R
    T -- Có --> V[Gọi PUT /api/business/id]
    V --> W{API success?}
    W -- Không --> X[Giữ modal Edit mở\nHiển thị lỗi API]
    X --> R
    W -- Có --> Y[TempData: Cập nhật thành công]
    Y --> P

    E -- Thoát --> Z([Kết thúc])
```

---

### AD-W04: Vô hiệu hóa Business

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin nhấn Vô hiệu hóa\ntrên dòng Business]
    B --> C[POST /Business/Deactivate?id=guid]
    C --> D[Gọi GET /api/business/id\nlấy thông tin hiện tại]
    D --> E{Tìm thấy\nBusiness?}
    E -- Không --> F[TempData: Không lấy được thông tin]
    F --> G[Redirect /Business/Index]
    G --> H([Kết thúc])
    E -- Có --> I[Giữ nguyên Name, TaxCode,\nContactEmail, ContactPhone\nĐổi IsActive = false]
    I --> J[Gọi PUT /api/business/id\nvới IsActive = false]
    J --> K{API success?}
    K -- Không --> L[TempData: Không thể vô hiệu hóa]
    L --> G
    K -- Có --> M[TempData: Business đã được vô hiệu hóa]
    M --> G
```

---

### AD-W05: Tạo / Cập nhật / Vô hiệu hóa Stall

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Người dùng vào /Stall/Index]

    B --> C1[Gọi GET /api/business\npageSize=100]
    B --> C2[Gọi GET /api/stall\npage, pageSize, search, businessId]
    C1 --> D[Chờ cả hai hoàn thành]
    C2 --> D
    D --> E[Hiển thị bảng Stall\n+ dropdown Business]

    E --> F{Chọn hành động?}

    F -- Lọc/Tìm kiếm --> G[Chọn Business hoặc nhập search]
    G --> C2

    F -- Tạo mới --> H[Mở modal Create\ncó dropdown Business]
    H --> I[Điền BusinessId, Name,\nDescription, Slug, Contact]
    I --> J[POST /Stall/Create]
    J --> K{ModelState OK?}
    K -- Không --> L[Giữ modal mở + lỗi]
    L --> I
    K -- Có --> M[POST /api/stall]
    M --> N{API success?}
    N -- Không --> O[Giữ modal mở + lỗi API]
    O --> I
    N -- Có --> P[Redirect + thông báo thành công]
    P --> B

    F -- Cập nhật --> Q[Mở modal Edit]
    Q --> R[Chỉnh sửa thông tin]
    R --> S[POST /Stall/Update]
    S --> T{ModelState OK?}
    T -- Không --> U[Giữ modal Edit + lỗi]
    U --> R
    T -- Có --> V[PUT /api/stall/id]
    V --> W{API success?}
    W -- Không --> X[Giữ modal Edit + lỗi API]
    X --> R
    W -- Có --> P

    F -- Vô hiệu hóa --> Y[POST /Stall/Deactivate]
    Y --> Z[GET /api/stall/id]
    Z --> AA{Tìm thấy?}
    AA -- Không --> AB[TempData error → Redirect]
    AB --> B
    AA -- Có --> AC[PUT /api/stall/id\nIsActive = false]
    AC --> AD{API success?}
    AD -- Không --> AE[TempData error → Redirect]
    AE --> B
    AD -- Có --> AF[TempData success → Redirect]
    AF --> B

    F -- Thoát --> AG([Kết thúc])
```

---

### AD-W06: Đặt vị trí Stall trên bản đồ

```mermaid
flowchart TD
    A([Bắt đầu]) --> B{Tạo mới hay\nchỉnh sửa?}

    B -- Tạo mới --> C[GET /StallLocation/CreateMap]
    B -- Chỉnh sửa --> D[GET /StallLocation/EditMap/id]

    C --> E1[Gọi GET /api/stall\npageSize=500]
    C --> E2[Gọi GET /api/stall-location\npageSize=500 để load markers]
    D --> F[Gọi GET /api/stall-location/id\nlấy dữ liệu vị trí hiện tại]
    F --> E1
    F --> E2

    E1 --> G[Chờ cả hai hoàn thành]
    E2 --> G
    G --> H[Hiển thị bản đồ OpenStreetMap\nvới tất cả markers hiện có]

    H --> I{Đang ở\nmode nào?}
    I -- Create --> J[Admin chọn Stall từ dropdown]
    I -- Edit --> K[Hiển thị marker tại tọa độ cũ]

    J --> L[Click vị trí trên bản đồ\nlấy lat/lng]
    K --> L

    L --> M[Nhập địa chỉ + bán kính]
    M --> N[Nhấn Lưu]
    N --> O{ModelState OK?}
    O -- Không --> P[Hiển thị lỗi trên trang map]
    P --> L

    O -- Có --> Q{Tạo mới hay\nCập nhật?}
    Q -- Tạo --> R[POST /api/stall-location]
    Q -- Cập nhật --> S[PUT /api/stall-location/id]

    R --> T{API success?}
    S --> T

    T -- Không --> U[Hiển thị lỗi API trên trang map]
    U --> L
    T -- Có --> V[TempData success]
    V --> W[Redirect /StallLocation/Index]
    W --> X([Kết thúc])
```

---

### AD-W07: Upload / Cập nhật / Xóa Media Stall

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Người dùng vào /StallMedia/Index]
    B --> C1[Gọi GET /api/stall\npageSize=500]
    B --> C2[Gọi GET /api/stall-media\npage, pageSize, stallId, isActive]
    C1 --> D[Chờ cả hai hoàn thành]
    C2 --> D
    D --> E[Hiển thị gallery ảnh\n+ dropdown Stall]

    E --> F{Chọn hành động?}

    F -- Upload mới --> G[Mở modal Create]
    G --> H[Chọn Stall, chọn file ảnh,\nnhập caption, sort order]
    H --> I{File ảnh\nđã chọn?}
    I -- Không --> J[Lỗi: Vui lòng chọn ảnh\nGiữ modal mở]
    J --> H
    I -- Có --> K{ModelState OK?}
    K -- Không --> L[Giữ modal mở + lỗi]
    L --> H
    K -- Có --> M[POST /api/stall-media/upload\nmultipart/form-data]
    M --> N[API upload file\nlên Azure Blob]
    N --> O{Upload\nthành công?}
    O -- Không --> P[Giữ modal mở + lỗi upload]
    P --> H
    O -- Có --> Q[Lưu URL vào DB\nTạo StallMedia record]
    Q --> R[Redirect + thông báo thành công]
    R --> B

    F -- Cập nhật ảnh --> S[Mở modal Edit\nvới caption/order hiện tại]
    S --> T[Chọn file ảnh mới]
    T --> U{File mới\nđã chọn?}
    U -- Không --> V[Lỗi: Vui lòng chọn ảnh mới]
    V --> T
    U -- Có --> W[PUT /api/stall-media/id/upload]
    W --> X{API success?}
    X -- Không --> Y[Giữ modal Edit + lỗi]
    Y --> T
    X -- Có --> R

    F -- Xóa --> Z[POST /StallMedia/Delete?id=guid]
    Z --> AA[DELETE /api/stall-media/id]
    AA --> AB{API success?}
    AB -- Không --> AC[TempData error → Redirect]
    AC --> B
    AB -- Có --> AD[TempData success → Redirect]
    AD --> B

    F -- Lọc --> AE[Chọn Stall hoặc trạng thái]
    AE --> C2

    F -- Thoát --> AF([Kết thúc])
```

---

### AD-W08: Tạo / Cập nhật GeoFence

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào /StallGeoFence/Index]
    B --> C1[Gọi GET /api/stall\npageSize=500]
    B --> C2[Gọi GET /api/stall-geofence\npage, pageSize, stallId]
    C1 --> D[Chờ cả hai hoàn thành]
    C2 --> D
    D --> E[Hiển thị danh sách GeoFence\n+ dropdown Stall]

    E --> F{Chọn hành động?}

    F -- Lọc theo Stall --> G[Chọn Stall từ dropdown]
    G --> C2

    F -- Tạo mới --> H[Mở form tạo GeoFence]
    H --> I[Chọn Stall,\nnhập tọa độ / bán kính]
    I --> J[POST /StallGeoFence/Create]
    J --> K{ModelState OK?}
    K -- Không --> L[TempData error\nRedirect /StallGeoFence/Index]
    L --> B
    K -- Có --> M[POST /api/stall-geofence]
    M --> N{API success?}
    N -- Không --> O[TempData error\nRedirect /StallGeoFence/Index]
    O --> B
    N -- Có --> P[TempData: Tạo geofence thành công\nRedirect /StallGeoFence/Index]
    P --> B

    F -- Cập nhật --> Q[Mở form Edit\nvới dữ liệu hiện tại]
    Q --> R[Chỉnh sửa tọa độ / bán kính]
    R --> S[POST /StallGeoFence/Update/id]
    S --> T{ModelState OK?}
    T -- Không --> U[TempData error\nRedirect]
    U --> B
    T -- Có --> V[PUT /api/stall-geofence/id]
    V --> W{API success?}
    W -- Không --> X[TempData error\nRedirect]
    X --> B
    W -- Có --> Y[TempData: Cập nhật thành công\nRedirect]
    Y --> B

    F -- Thoát --> Z([Kết thúc])
```

---

### AD-W09: Tạo / Cập nhật / Toggle Narration Content

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Vào /Narration/StallNarrationContents]
    B --> C1[Gọi GET /api/stall\npageSize=200]
    B --> C2[Gọi GET /api/languages\nisActive=true]
    B --> C3[Gọi GET /api/stall-narration-content\nvới bộ lọc hiện tại]
    C1 --> D[Chờ cả ba hoàn thành]
    C2 --> D
    C3 --> D
    D --> E[Hiển thị danh sách Content\n+ dropdown lọc Stall/Language/Status]

    E --> F{Chọn hành động?}

    F -- Lọc --> G[Chọn stall / language / isActive]
    G --> C3

    F -- Tạo mới --> H[Mở form tạo]
    H --> I[Chọn Stall, Language\nNhập Title, Description, ScriptText]
    I --> J[POST /Narration/Create]
    J --> K{ModelState OK?}
    K -- Không --> L[TempData error\nRedirect về danh sách]
    L --> B
    K -- Có --> M[POST /api/stall-narration-content]
    M --> N{API success?}
    N -- Không --> O[TempData error\nRedirect về danh sách]
    O --> B
    N -- Có --> P[TempData: Tạo thành công\nRedirect về danh sách]
    P --> B

    F -- Toggle trạng thái --> Q[POST /Narration/ToggleStatus\n?id=guid&isActive=bool]
    Q --> R[Gọi API toggle status]
    R --> S{API success?}
    S -- Không --> T[TempData error\nRedirect về danh sách]
    T --> B
    S -- Có --> U[TempData: Đổi trạng thái thành công\nRedirect về danh sách]
    U --> B

    F -- Xem chi tiết --> V[Chuyển sang AD-W10]

    F -- Thoát --> W([Kết thúc])
```

---

### AD-W10: Xem chi tiết Narration Content + Cập nhật script

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Vào /Narration/Show/id]
    B --> C[Gọi GET /api/stall-narration-content/id\nlấy content + danh sách audios]
    C --> D{Tìm thấy\ncontent?}
    D -- Không --> E[Hiển thị trang lỗi:\nKhông tìm thấy nội dung]
    E --> F([Kết thúc])

    D -- Có --> G1[Gọi GET /api/stall/stallId\nlấy tên Stall]
    D -- Có --> G2[Gọi GET /api/languages\nlấy tên Language]
    G1 --> H[Chờ cả hai hoàn thành]
    G2 --> H
    H --> I[Hiển thị trang detail:\nScript text, tên Stall/Language\nDanh sách NarrationAudio với nút phát thử]

    I --> J{Người dùng\nchọn hành động?}

    J -- Chỉnh sửa script --> K[Sửa Title, Description,\nScriptText, IsActive trên form]
    K --> L[POST /Narration/Update/id]
    L --> M{ModelState OK?}
    M -- Không --> N[Hiển thị lại trang detail\nvới dữ liệu đã nhập + lỗi validation]
    N --> K
    M -- Có --> O[PUT /api/stall-narration-content/id]
    O --> P{API success?}
    P -- Không --> Q[Hiển thị lại trang detail\nvới thông báo lỗi API]
    Q --> K
    P -- Có --> R[TempData: Cập nhật thành công]
    R --> S[Redirect /Narration/Show/id]
    S --> B

    J -- Quay lại danh sách --> T[Redirect /Narration/StallNarrationContents]
    T --> U([Kết thúc])
```