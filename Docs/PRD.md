# Product Requirements Document (PRD)
# Hệ thống Thuyết minh Tự động Đa ngôn ngữ – Phố Ẩm Thực

> **Phiên bản:** 3.0 (Full Rewrite – Khớp hiện trạng dự án)
> **Ngày cập nhật:** 2026-04-23
> **Trạng thái:** Đang phát triển — các module cốt lõi đã hoạt động
> **Mục đích tài liệu:** Mô tả chính xác trạng thái hiện tại của sản phẩm — dùng làm baseline cho test, kiểm thử nghiệm thu, và lập kế hoạch giai đoạn tiếp theo.

---

## Mục lục

1. [Tổng quan sản phẩm](#1-tổng-quan-sản-phẩm)
2. [Vấn đề & Mục tiêu](#2-vấn-đề--mục-tiêu)
3. [Người dùng mục tiêu](#3-người-dùng-mục-tiêu)
4. [Phạm vi sản phẩm](#4-phạm-vi-sản-phẩm)
5. [Kiến trúc hệ thống](#5-kiến-trúc-hệ-thống)
6. [Mô hình dữ liệu](#6-mô-hình-dữ-liệu)
7. [Model Subscription & Business Rules](#7-model-subscription--business-rules)
8. [Yêu cầu chức năng – Mobile App](#8-yêu-cầu-chức-năng--mobile-app)
9. [Yêu cầu chức năng – Web Admin](#9-yêu-cầu-chức-năng--web-admin)
10. [Yêu cầu chức năng – API Backend](#10-yêu-cầu-chức-năng--api-backend)
11. [API Endpoints](#11-api-endpoints)
12. [Tích hợp dịch vụ ngoài](#12-tích-hợp-dịch-vụ-ngoài)
13. [Phân quyền & Bảo mật](#13-phân-quyền--bảo-mật)
14. [Yêu cầu phi chức năng](#14-yêu-cầu-phi-chức-năng)
15. [Quyết định kỹ thuật chính](#15-quyết-định-kỹ-thuật-chính)
16. [Vấn đề đã biết & Nợ kỹ thuật](#16-vấn-đề-đã-biết--nợ-kỹ-thuật)
17. [Backlog](#17-backlog)
- [Phụ lục A – Cấu hình môi trường](#phụ-lục-a--cấu-hình-môi-trường)
- [Phụ lục B – NuGet Packages](#phụ-lục-b--nuget-packages)

---

## 1. Tổng quan sản phẩm

### 1.1 Tên hệ thống

**Hệ thống Thuyết minh Tự động Đa ngôn ngữ cho Phố Ẩm Thực**
*(Locate & Multilingual Narration System)*

### 1.2 Mô tả ngắn

Hệ thống cung cấp trải nghiệm tham quan thông minh tại Phố Ẩm Thực thông qua ba thành phần:
- **Mobile App** tự động phát audio thuyết minh khi khách đến gần từng gian hàng (geofencing). Khách truy cập ẩn danh qua mã QR — không cần đăng ký tài khoản.
- **Web Admin** cho Ban Tổ Chức và Chủ doanh nghiệp quản lý gian hàng, nội dung thuyết minh đa ngôn ngữ, media, subscription plan, tour tham quan, và theo dõi thiết bị real-time.
- **API Backend** xử lý business logic, tích hợp Azure (TTS / Blob / Translator), và background job tự động tổng hợp audio.

### 1.3 Thành phần hệ thống

| Thành phần | Công nghệ | Người dùng |
|-----------|-----------|-----------|
| **Mobile App** | .NET MAUI 10.0 (Android / iOS / Windows / macOS) | Khách tham quan (anonymous) |
| **Web Admin** | ASP.NET Core 10.0 MVC + Tabler UI | Admin, BusinessOwner |
| **API Backend** | ASP.NET Core 10.0 Web API | Toàn hệ thống |
| **Database** | SQL Server (EF Core 10.0) | – |
| **Cloud Services** | Azure Speech, Azure Blob Storage, Azure Translator | – |

### 1.4 Stack công nghệ

```
Backend    : ASP.NET Core 10.0 Web API + EF Core 10.0 + SQL Server
Web Admin  : ASP.NET Core 10.0 MVC + Tabler 1.0.0-beta20 + Leaflet + SortableJS
Mobile     : .NET MAUI 10.0 + CommunityToolkit.Mvvm + Mapsui + SQLite + Plugin.Maui.Audio
Shared     : .NET 10.0 Class Library (DTOs dùng chung)
Auth       : JWT Bearer HS256 (30 phút) + Refresh Token SHA256 (30 ngày)
Map        : Mapsui / OpenStreetMap (Mobile), Leaflet (Web)
TTS        : Azure Cognitive Services Speech v1.48.2
Storage    : Azure Blob Storage (container: narration-audio, access: Blob public)
Translate  : Azure Translator v3.0
QR         : ZXing.Net.Maui (Mobile scan) + QRCoder (API generate)
```

### 1.5 Môi trường

| Môi trường | URL |
|-----------|-----|
| API Dev | `http://localhost:5299` |
| Web Dev | `https://localhost:7188` |
| API Prod | `https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/` |
| Swagger (Dev) | `http://localhost:5299/swagger` |
| Android Emulator → API | `http://10.0.2.2:5299` |

**Timezone mặc định:** SE Asia Standard Time (UTC+7).

---

## 2. Vấn đề & Mục tiêu

### 2.1 Vấn đề

- Khách tham quan Phố Ẩm Thực không có thông tin đầy đủ về từng gian hàng.
- Khách quốc tế gặp rào cản ngôn ngữ, không hiểu sản phẩm/dịch vụ được giới thiệu.
- Nhân viên giới thiệu thủ công tốn chi phí và không nhất quán.
- Doanh nghiệp khó cập nhật thông tin thuyết minh linh hoạt theo sự kiện/mùa.
- Ban Tổ Chức không có công cụ theo dõi lượng khách đang tham quan và phân bố di chuyển.

### 2.2 Mục tiêu

| # | Mục tiêu | Đo lường thành công |
|---|----------|-------------------|
| 1 | Tự động phát thuyết minh khi khách đến gần gian hàng | Geofence trigger chính xác trong bán kính cấu hình (thường 8–30m) |
| 2 | Hỗ trợ đa ngôn ngữ | Có ≥ 2 ngôn ngữ active; audio phát đúng ngôn ngữ và giọng đã chọn |
| 3 | Khách tham quan không cần đăng ký | App hoạt động hoàn toàn anonymous qua mã QR một lần |
| 4 | Doanh nghiệp tự quản lý nội dung | BusinessOwner cập nhật được script và audio không cần kỹ thuật viên |
| 5 | Hoạt động offline | App hiển thị dữ liệu và phát audio đã cache khi mất mạng |
| 6 | Mô hình doanh thu bền vững | 3 plan (Free / Basic / Pro) với giới hạn stall rõ ràng và TTS gate |
| 7 | Hỗ trợ tour tham quan có lộ trình | Admin tạo tour; khách chạy tour với thứ tự dẫn đường |
| 8 | Theo dõi lượng khách real-time | Admin thấy thiết bị online với cửa sổ 30–300 giây |

---

## 3. Người dùng mục tiêu

### 3.1 Khách tham quan (Visitor) – Anonymous

| Thuộc tính | Mô tả |
|-----------|-------|
| **Đặc điểm** | Khách nội địa hoặc quốc tế, mọi độ tuổi, đến tham quan Phố Ẩm Thực |
| **Mục tiêu** | Tìm hiểu về các gian hàng, nghe thuyết minh bằng ngôn ngữ phù hợp |
| **Thiết bị** | Smartphone Android / iOS |
| **Kỳ vọng** | Quét QR tại cổng → chọn ngôn ngữ → dùng app không cần đăng ký |
| **Pain point** | Không biết tiếng Việt, muốn thông tin chi tiết hơn biển hiệu, không muốn tạo tài khoản |

**Định danh ẩn danh:** App đọc/sinh `device_id` (GUID) lưu trong `Microsoft.Maui.Storage.Preferences`. Mã QR là vé vào app — mỗi mã dùng một lần với số ngày hiệu lực (`ValidDays`) do Admin cấu hình. Preference ngôn ngữ/giọng đọc lưu trên server theo `DeviceId` và đồng bộ xuống local.

### 3.2 Chủ doanh nghiệp (BusinessOwner)

| Thuộc tính | Mô tả |
|-----------|-------|
| **Đặc điểm** | Chủ hoặc quản lý gian hàng tại Phố Ẩm Thực |
| **Mục tiêu** | Quản lý thông tin gian hàng, nội dung thuyết minh, hình ảnh; nâng cấp plan khi cần thêm stall hoặc kích hoạt TTS |
| **Thiết bị** | PC / Laptop, trình duyệt web |
| **Quyền hạn** | Chỉ thao tác dữ liệu business của mình (không thấy dữ liệu business khác) |

### 3.3 Quản trị viên (Admin)

| Thuộc tính | Mô tả |
|-----------|-------|
| **Đặc điểm** | Ban Tổ Chức / Quản trị hệ thống |
| **Mục tiêu** | Quản lý toàn bộ hệ thống; tạo mã QR; tạo tour; theo dõi real-time; cập nhật plan cho business |
| **Quyền hạn** | Toàn bộ hệ thống; bypass giới hạn plan; được phép set plan tuỳ ý cho business |

---

## 4. Phạm vi sản phẩm

### 4.1 Trong phạm vi (đã triển khai)

#### Mobile App (12 luồng chính)
- Startup routing với kiểm tra QR access + preference
- Quét QR kích hoạt quyền truy cập app (one-time use, hạn hiệu lực ngày)
- Chọn ngôn ngữ + giọng đọc trong cùng màn hình
- MainPage với bottom nav 3 nút (Bản đồ / Tour / Ngôn ngữ) + featured stalls + Đăng xuất
- Bản đồ tương tác với geofence circles, tap pin → popup → phát audio
- Geofence auto-play khi khách đi vào vùng (queue nếu đang phát)
- Danh sách gian hàng có tìm kiếm + phân trang
- Cache 3 lớp: memory (10 phút) → SQLite → API; offline vẫn xem được
- Background sync stall (3 phút) + flush GPS (20 giây) + piggyback heartbeat
- Tour flow: chọn tour → chạy với lộ trình → tự hoàn tất → resume sau kill app
- Pull cờ reset từ Admin (3 phút poll) + notify offline khi thoát app
- Chế độ tour: chỉ auto-play stall trong tour set

#### Web Admin
- Đăng nhập / đăng ký BusinessOwner / đăng xuất
- Dashboard Admin (9 API calls song song) với stats cards + recent orders
- CRUD Business, Stall, StallLocation, StallGeoFence, StallMedia
- CRUD Narration Content + theo dõi TTS status + retry TTS
- Upload audio giọng người (thay thế TTS)
- Xem bảng giá + Checkout + Payment (mock 16-digit card)
- Quản lý QR Codes + Kiosk Auto QR tự động
- Admin cập nhật plan cho Business + xem Lịch sử Đơn đăng ký
- Quản lý User & Role (Admin only)
- Active Devices real-time với 5 option dropdown (30/60/120/180/300s) + Reset thiết bị
- Bản đồ nhiệt (Heatmap) vị trí người dùng với filter thời gian + device
- Quản lý Tour (TourManagement + TourDesigner với Leaflet drag-drop)

#### API Backend
- JWT authentication + refresh token
- 18 controllers + base controller với timezone helper
- TTS Background Service (PeriodicTimer 5 giây, claim batch 5 job)
- Geo service: trả stall + narration + audio theo ngôn ngữ + giọng của device
- Subscription system với 3 plan + downgrade protection + extend logic
- QR verify one-time use với `expiryAt = UsedAt + ValidDays`
- Tour CRUD với Stops + reorder + validation
- GPS batch log + heatmap aggregation
- Active devices query + device reset flag + offline notify
- Azure integration: TTS + Blob upload + Translator v3.0

### 4.2 Ngoài phạm vi

- Background GPS polling khi app inactive (GpsPollingService chỉ chạy khi MapPage active)
- Bookmark gian hàng yêu thích
- Xem menu / thực đơn chi tiết
- Dashboard thống kê lượt nghe audio (dữ liệu chưa thu thập)
- Audit log cho CRUD operations
- Social login
- Role Collaborator (cộng tác viên hỗ trợ BusinessOwner)
- Push notification
- Gateway thanh toán thật (hiện mock 16-digit card)
- Scheduler tự áp dụng plan hết hạn (hiện dùng effective plan logic runtime)
- CRUD admin cho TTS voice profiles (chỉ có `GET active`)
- CRUD admin cho Language (seed cứng qua migration)

---

## 5. Kiến trúc hệ thống

### 5.1 Sơ đồ tổng quan

```
┌──────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                             │
│                                                                  │
│  ┌──────────────────────────┐      ┌──────────────────────────┐ │
│  │   Mobile App (MAUI)      │      │   Web Admin (MVC)        │ │
│  │  Android / iOS / Win     │      │  BusinessOwner / Admin   │ │
│  │  • Mapsui (OSM)          │      │  • Tabler 1.0.0-beta20   │ │
│  │  • SQLite cache          │      │  • Leaflet + heat + OSRM │ │
│  │  • Plugin.Maui.Audio     │      │  • SortableJS drag-drop  │ │
│  │  • ZXing QR Scan         │      │  • AuthTokenHandler      │ │
│  │  • Preferences (pref_*)  │      │  • Session + CSRF        │ │
│  └────────────┬─────────────┘      └────────────┬─────────────┘ │
└───────────────┼──────────────────────────────────┼──────────────┘
                │ HTTP/JSON + deviceId              │ HTTP/JSON + JWT
                ▼                                  ▼
┌──────────────────────────────────────────────────────────────────┐
│               API LAYER (ASP.NET Core 10.0)                      │
│                                                                  │
│  18 Controllers (REST) + 1 AppControllerBase                     │
│  4 Application Services: JwtService, GeoService,                 │
│                          NarrationAudioService, AzureTranslation │
│  1 Hosted Service: TtsBackgroundService (PeriodicTimer 5s)       │
│                                                                  │
│  Auth: JWT HS256 + BCrypt + RefreshToken SHA256                  │
│  Policies: AdminOnly, AdminOrBusinessOwner, [AllowAnonymous]     │
└──────┬────────────────────────────────────────────┬─────────────┘
       │ EF Core 10.0                              │ Azure SDK
       ▼                                          ▼
┌──────────────────┐                ┌──────────────────────────────┐
│   SQL Server     │                │   Azure Cloud Services       │
│   24 Entities    │                │  • Speech (TTS)              │
│                  │                │  • Blob Storage (public URL) │
│                  │                │  • Translator v3.0           │
└──────────────────┘                └──────────────────────────────┘

                ┌────────────────────────────────┐
                │   Shared Library (.NET 10.0)   │
                │  DTOs dùng chung: 17 thư mục    │
                │  Api + Web + Mobile            │
                └────────────────────────────────┘
```

### 5.2 Cấu trúc Solution

```
LocateAndMultilingualNarration/
├── Api/                    ASP.NET Core 10.0 Web API
│   ├── Controllers/        18 controllers + AppControllerBase
│   ├── Application/
│   │   └── Services/       JwtService, GeoService, NarrationAudioService,
│   │                       AzureTranslationService, TtsBackgroundService
│   ├── Authorization/      AppPolicies (hằng số)
│   ├── Domain/
│   │   ├── Entities/       24 entities
│   │   ├── Settings/       JwtSettings, AzureSpeechSettings, BlobStorageSettings,
│   │   │                   AzureTranslationSettings, QrCodeConfiguration
│   │   └── SubscriptionPlan.cs  Hằng số + helper (GetMaxStalls, AllowsTts, GetPrice)
│   ├── Infrastructure/
│   │   └── Persistence/    AppDbContext + Configurations + QueryExtensions
│   └── Migrations/         EF Core migrations
├── Web/                    ASP.NET Core 10.0 MVC
│   ├── Controllers/        12 controllers (Auth, Home, Admin,
│   │                       Business, Stall, StallLocation, StallGeoFence,
│   │                       StallMedia, Narration, Subscription, Tour, Docs)
│   ├── Views/              Razor views (Tabler UI)
│   ├── Services/           16 ApiClient + AuthTokenHandler + ApiClient (base)
│   ├── Models/             ViewModels
│   └── Filters/            TokenExpirationFilter (global)
├── Mobile/                 .NET MAUI 10.0
│   ├── Pages/              9 pages (Loading, Scan, Language, Main,
│   │                       Map, StallList, TourList, TourDetail) + StallPopup
│   ├── ViewModels/         7 ViewModels + GeofenceEngine (class thuần)
│   ├── Services/           14 services + 15 interfaces
│   ├── LocalDb/            SQLite schema + LocalStallRepository
│   └── DevConfig.cs        API base URL cho emulator
├── Shared/                 Class Library .NET 10.0
│   └── DTOs/               17 nhóm: Auth, Businesses, Common, DeviceLocationLogs,
│                           DevicePreferences, Geo, Languages, Narrations, QrCodes,
│                           StallGeoFences, StallLocations, StallMedia, Stalls,
│                           SubscriptionOrders, Tours, TtsVoiceProfiles, Users
└── TestAPI/                Test project
```

### 5.3 Nguyên tắc kiến trúc

- **API là trung tâm duy nhất xử lý business logic.** Web và Mobile chỉ gọi API, không truy cập DB trực tiếp.
- **Mapping thủ công trong Service layer của API.** Không dùng AutoMapper.
- **Mobile dùng thẳng DTO từ Shared** — không có Model layer riêng.
- **Cache-first Mobile:** memory (10 phút) → SQLite → API. Đảm bảo offline và UX instant.
- **Background TTS:** Controller chỉ set `TtsStatus = Pending`, `TtsBackgroundService` xử lý async. Tránh HTTP request timeout khi TTS mất nhiều giây.
- **Piggyback heartbeat:** `LastSeenAt` của `DevicePreference` được cập nhật trong `GeoController.GetAllStalls` và `DeviceLocationLogController.BatchCreate`. Không có endpoint ping riêng.
- **Pull-based flag cho Admin → Mobile:** Admin không push được trực tiếp. Admin set cờ trong DB, Mobile poll định kỳ qua `SyncBackgroundService`.

---

## 6. Mô hình dữ liệu

### 6.1 Danh sách Entities (24)

#### Nhóm User & Authorization (4)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `User` | Tài khoản hệ thống (Admin / BusinessOwner) | Id, UserName, Email, NormalizedEmail, NormalizedUserName, PasswordHash, DisplayName, Sex, DateOfBirth, IsActive, LastLoginAt, CreatedAt, LockoutEnd |
| `Role` | Vai trò | Id, Name |
| `UserRole` | Nối User ↔ Role | UserId, RoleId |
| `RefreshToken` | JWT refresh token 30 ngày | Id, UserId, TokenHash (SHA256), ExpiresAtUtc, RevokedAtUtc, DeviceId, IpAddress, CreatedAtUtc |

#### Nhóm Business & Stalls (7)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `Business` | Doanh nghiệp | Id, OwnerUserId, Name, TaxCode, ContactEmail, ContactPhone, IsActive, Plan, PlanExpiresAt, CreatedAt, UpdatedAt |
| `BusinessOwnerProfile` | Profile chi tiết chủ DN | Id, UserId, PhoneNumber, Address |
| `EmployeeProfile` | Profile nhân viên | Id, UserId, BusinessId |
| `Stall` | Gian hàng | Id, BusinessId, Name, Slug, Description, ContactEmail, ContactPhone, IsActive, CreatedAt, UpdatedAt |
| `StallLocation` | Tọa độ GPS | Id, StallId, Latitude (decimal 9,6), Longitude (decimal 9,6), RadiusMeters, Address, IsActive |
| `StallGeoFence` | Vùng geofence | Id, StallId, Name, RadiusMeters, IsActive |
| `StallMedia` | Ảnh gian hàng | Id, StallId, MediaUrl (Azure Blob), BlobId, MediaType, Caption, SortOrder, IsActive |

#### Nhóm Narration & Languages (4)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `Language` | Ngôn ngữ hỗ trợ | Id, Code (vi, en, zh...), Name, DisplayName, FlagCode, IsActive |
| `TtsVoiceProfile` | Cấu hình giọng Azure | Id, LanguageId, Provider, VoiceName (Azure voice id), DisplayName, Gender, Description, IsActive |
| `StallNarrationContent` | Script thuyết minh | Id, StallId, LanguageId, Title, Description, ScriptText, IsActive, TtsStatus (None/Pending/Processing/Completed/Failed), TtsError, CreatedAt, UpdatedAt |
| `NarrationAudio` | File audio | Id, NarrationContentId, TtsVoiceProfileId?, AudioUrl (Azure Blob), BlobId, DurationSeconds?, IsTts, Voice, Provider (AzureTts / Human), UpdatedAt |

#### Nhóm Device & Visitor (3)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `DevicePreference` | Preference ẩn danh theo DeviceId | Id, DeviceId, LanguageId, VoiceId?, SpeechRate, AutoPlay, Platform, DeviceModel, Manufacturer, OsVersion, FirstSeenAt, LastSeenAt, NeedsReset |
| `DeviceLocationLog` | Nhật ký GPS của thiết bị | Id, DeviceId, Latitude (decimal 9,6), Longitude (decimal 9,6), AccuracyMeters?, CapturedAtUtc |
| `ScanLog` | Log quét QR (có migration, chưa controller dùng) | Id, QrCodeId, DeviceId, ScannedAt |

#### Nhóm Subscription & Payment (1)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `SubscriptionOrder` | Đơn đăng ký plan | Id, BusinessId, Plan, Amount, Status (Completed/Failed), CardLastFour, CardHolder, PaidAt, PlanStartAt, PlanEndAt |

#### Nhóm QR Code (2)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `QrCode` | Mã QR vé vào app | Id, Code (unique), ValidDays, IsUsed, UsedAt?, UsedByDeviceId?, Note, CreatedAt |
| `QrCodeConfiguration` | Cấu hình generate QR | Id, DefaultValidDays, BaseUrl |

#### Nhóm Tour (2)

| Entity | Mô tả | Trường chính |
|--------|-------|-------------|
| `Tour` | Tuyến tham quan | Id, Name (unique), Description, EstimatedMinutes, IsActive, CreatedAt, UpdatedAt, CreatedByUserId (FK User, Restrict) |
| `TourStop` | Điểm dừng trong tour | Id, TourId (FK Tour, Cascade), StallId (FK Stall, Restrict), Order, Note, CreatedAt. Unique `(TourId, StallId)`, Index `(TourId, Order)` |

**Ghi chú:** `TtsJobStatus` là **static class hằng số** (`None/Pending/Processing/Completed/Failed`), không phải entity.

### 6.2 Quan hệ chính

```
User (1) ─────────── (N) UserRole (N) ─────────── (1) Role
User (1) ─────────── (1?) Business.OwnerUserId
User (1) ─────────── (1?) BusinessOwnerProfile
User (1) ─────────── (1?) EmployeeProfile
User (1) ─────────── (N) Tour.CreatedByUserId

Business (1) ─────── (N) Stall
Business (1) ─────── (N) SubscriptionOrder

Stall (1) ─────────── (N) StallLocation
Stall (1) ─────────── (N) StallGeoFence
Stall (1) ─────────── (N) StallMedia
Stall (1) ─────────── (N) StallNarrationContent
Stall (1) ─────────── (N) TourStop

StallNarrationContent (N) ─── (1) Language
StallNarrationContent (1) ──── (N) NarrationAudio
NarrationAudio (N) ─────────── (0..1) TtsVoiceProfile
Language (1) ───────────────── (N) TtsVoiceProfile

DevicePreference (N) ────────── (1) Language
DevicePreference (N) ────────── (0..1) TtsVoiceProfile

Tour (1) ─────────────────── (N) TourStop
TourStop (N) ─────────────── (1) Stall

QrCode (0..1 UsedByDeviceId) — không FK cứng (DeviceId là string free-form)
```

---

## 7. Model Subscription & Business Rules

### 7.1 Các Plan

Định nghĩa trong `Api/Domain/SubscriptionPlan.cs` (static class):

| Plan | Giá | Số gian hàng | TTS |
|------|-----|-------------|-----|
| **Free** | 0đ | 1 | ❌ Không |
| **Basic** | 199,000đ/tháng | 3 | ✅ Có |
| **Pro** | 499,000đ/tháng | Không giới hạn | ✅ Có |

Helper methods:
- `GetMaxStalls(plan)` — trả Free=1, Basic=3, Pro=`int.MaxValue`
- `AllowsTts(plan)` — trả `false` chỉ với Free
- `GetPrice(plan)` — trả `decimal`

### 7.2 Effective Plan Logic

Nhiều nơi trong code dùng pattern:

```csharp
var planIsExpired = business.PlanExpiresAt.HasValue && business.PlanExpiresAt.Value <= DateTimeOffset.UtcNow;
var effectivePlan = (planIsExpired && business.Plan != "Free") ? "Free" : business.Plan;
```

Nghĩa là khi plan hết hạn, business tự động **rơi về Free** ở runtime (không có scheduler thay đổi DB). Nếu muốn tiếp tục, phải mua lại plan mới.

### 7.3 Business Rules

- **Tạo Stall:** `StallController.CreateStall` kiểm tra `CountByBusinessAsync(businessId) < GetMaxStalls(effectivePlan)`. Admin bypass.
- **Tạo/Cập nhật Narration có TTS:** Nếu `effectivePlan = "Free"` thì API từ chối tạo/cập nhật content có script (không cho TTS). Plan cho phép thì set `TtsStatus = Pending` để `TtsBackgroundService` xử lý.
- **Đăng ký plan (mua):** `SubscriptionOrderController.CreateOrder` chặn **downgrade** khi plan hiện tại còn hạn — `PlanRank(request.Plan) < PlanRank(business.Plan)` trả 400.
- **Extend plan:** Nếu business đang có plan active, `planStartAt = PlanExpiresAt hiện tại` (gia hạn từ ngày kết thúc cũ). Nếu không có plan active, `planStartAt = now`. `planEndAt = planStartAt + 1 tháng`.
- **Mock payment:** Strip spaces/dashes khỏi `cardNumber`. Đúng 16 chữ số → `Completed`. Sai → `Failed`. Chỉ khi `Completed` mới cập nhật `business.Plan` và `business.PlanExpiresAt`. Đơn `Failed` vẫn lưu để Admin xem lịch sử.
- **Admin cập nhật plan trực tiếp:** `PUT /api/business/{id}/subscription` (AdminOnly) không qua flow payment — Admin có toàn quyền set plan + PlanExpiresAt tuỳ ý (không kiểm downgrade).

---

## 8. Yêu cầu chức năng – Mobile App

> **Nguyên tắc:** Mobile hoàn toàn anonymous. Không có login. Định danh qua `DeviceId` (GUID) sinh tự động và lưu trong `Preferences` (không phải SecureStorage).

### 8.1 Khởi động & Routing (LoadingPage)

**FR-M-01: Routing theo 3 trạng thái**
- Đọc `Preferences.Get("device_id", null)` raw.
- Nếu `device_id = null` → `//ScanPage` (app cài lần đầu).
- Nếu có `device_id` → kiểm tra `QrService.IsAccessValid()`:
  - QR hết hạn / không hợp lệ → `//ScanPage`.
  - QR còn hạn → kiểm `LocalPreferenceService.Load()`:
    - Có local preference → `//MainPage` (không gọi API, tối ưu cold start).
    - Không có → gọi `GET /api/device-preference/{deviceId}`:
      - API có → save local → `//MainPage`.
      - API 404 → `LanguagePage` (relative route, không `//`).

### 8.2 Quét QR (ScanPage)

**FR-M-02: Quét QR qua camera hoặc chọn ảnh**
- `ScanResultCommand` (ZXing scan camera) hoặc `PickImageFromGalleryCommand` (MediaPicker + ZXing decode).
- Gọi `DeviceService.GetOrCreateDeviceId()` — đây là nơi sinh `device_id` lần đầu.
- Gọi `POST /api/qrcodes/verify` với timeout 5 giây (khắt khe hơn HttpClient 10 giây).

**FR-M-03: Xử lý kết quả verify**
- Timeout / lỗi mạng → "Không thể kết nối".
- API trả `isValid = false` → hiển thị message từ API (QR đã dùng / không tồn tại).
- Thành công → `QrService.SaveAccess(expiryAt)` lưu `qr_verified` + `qr_expiry` → navigate `LanguagePage`.

### 8.3 Chọn ngôn ngữ & giọng đọc (LanguagePage)

**FR-M-04: Load ngôn ngữ và giọng**
- `GET /api/languages/active` qua `LanguageService` (cache 15 phút memory).
- Khi chọn ngôn ngữ → `GET /api/tts-voice-profiles/active?languageId=...` qua `VoiceService`.

**FR-M-05: Lưu preference**
- `POST /api/device-preference` (Upsert) với full payload: `deviceId, languageId, voiceId?, speechRate, autoPlay, platform, deviceModel, manufacturer, osVersion`.
- **Local save tự động** bên trong overload `UpsertAsync(dto)` — ViewModel không phải gọi `LocalPreferenceService.Save` trực tiếp.
- Navigate `//MapPage` sau khi lưu.

### 8.4 MainPage

**FR-M-06: Bottom nav 3 nút + Đăng xuất**
- `MapCommand` → `//MapPage`.
- `ToursCommand` → `//TourListPage`.
- `LanguageCommand` → `LanguagePage`.
- `LogoutCommand` → `QrService.ClearAccess()` (chỉ xóa QR, giữ language/voice) → `//ScanPage`.

**FR-M-07: Featured stalls**
- PageSize = 3, paging client-side trên `_allStalls` lấy qua `StallService.GetAllStallsAsync()` (cache-first).

### 8.5 Bản đồ (MapPage)

**FR-M-08: State machine**
- Enum `MapState { Uninitialized, Syncing, Loading, Ready, Error }`.
- `EnsureReadyAsync(bool forceReload, CancellationToken ct)` guard bằng `SemaphoreSlim(1, 1)`.
- `forceReload = false` (mặc định) → gọi `SyncService.EnsureSyncedAsync()` (chỉ sync nếu cache hết hạn).
- `forceReload = true` (pull-to-refresh) → gọi `SyncService.SyncAsync()` force.

**FR-M-09: Cache-first 3 lớp**
- Memory (10 phút) → SQLite (`stalls.db3` via `sqlite-net-pcl`) → API (`/api/geo/stalls?deviceId=X`).
- `LocalStallRepository.UpsertBatchAsync` có diff check `HasChanged` trước khi ghi.

**FR-M-10: Subscribe 3 event Singleton**
- `AudioGuideService.PlaybackCompleted` → trigger queue geofence tiếp theo.
- `GpsPollingService.LocationUpdated` → gọi `GeofenceEngine.CheckAsync`.
- `SyncService.AudioDownloaded` → refresh UI khi audio mới cache xong.
- **Dispose** trong `MapPage.Unloaded` để unsubscribe → tránh memory leak (VM Transient, Service Singleton).

**FR-M-11: Geofence auto-play**
- `GeofenceEngine` là class thuần (không DI), owned bởi MapVM.
- Haversine khoảng cách (bán kính Trái Đất 6,371,000m) so với `radiusMeters` (đơn vị mét).
- State `_triggeredIds` tránh trigger lại cùng stall trong session.
- Event `AutoPlayRequested: Func<GeoStallDto, Task>` awaitable (không fire-and-forget).
- **Tour mode filter:** `SetActiveTour(IEnumerable<Guid>?)` — null = không filter; set = chỉ trigger stall trong tập này.
- Nếu đang phát → enqueue; `PlaybackCompleted` fire → dequeue tiếp.

**FR-M-12: Tap pin → Popup → Play**
- `OnPinClickedAsync` set `SelectedStall = stall`; geofence KHÔNG chạm `SelectedStall` (tách UI selection khỏi audio target — fix bug race).
- Popup nhấn "Phát" → `await _vm.PlayStallAsync(stall)` (PlayStallAsync là Task thuần, awaitable).
- `AudioCacheService.GetOrDownloadAsync(url, stallId, langCode)` tải MP3 về `{AppDataDirectory}/audio/{lang}/{stallId}.mp3` dùng HttpClient `"download"` timeout 30s.

### 8.6 Tour Flow

**FR-M-13: Tour list + detail**
- `TourListPage` → `TourService.GetToursAsync()` — cache memory only 10 phút (không SQLite).
- `TourDetailPage` qua `[QueryProperty("TourId", "tourId")]`.

**FR-M-14: Start tour**
- `LocalPreferenceService.SetActiveTourId(id)` → `//MapPage?tourId={id}`.
- `MapPage.OnAppearing` → `ResolveTourAsync()` đọc query hoặc fallback `GetActiveTourId()` — cho phép resume sau kill app.
- `MapViewModel.SetActiveTourAsync(tourId)` → fetch detail → `GeofenceEngine.SetActiveTour(stallIds)` → raise `TourRouteChanged` event → MapPage vẽ polyline theo sequence stops.

**FR-M-15: Progress & complete**
- Mỗi stall trong tour được trigger → `AddCompletedStop(stallId)` (khóa `pref_tour_completed_stops`).
- Khi đủ stop → `CompleteTourAsync()`: alert hoàn thành + `ClearTourProgress()` + `SetActiveTour(null)`.

### 8.7 Background Sync & Offline

**FR-M-16: SyncBackgroundService**
- 2 `PeriodicTimer` song song: `StallSyncInterval = 3 phút`, `FlushInterval = 20 giây`.
- Piggyback `CheckResetFlagAsync` (GET `/api/device-preference/reset-flag`) ngay sau mỗi stall sync tick.
- `ConnectivityChanged` → trigger sync ngay, capture local `_cts` trước khi dùng (tránh race với Stop).
- `Start()` gọi `CleanupInternal()` chứ không `Stop()` (tránh gửi offline notify nhầm lúc restart).

**FR-M-17: Offline notify**
- `Stop()` fire-and-forget `POST /api/device-preference/{deviceId}/offline` → API set `LastSeenAt = MinValue` để thiết bị rớt ngay khỏi active-devices list.
- Trigger từ: `App.OnSleep`, `MapPage.OnDisappearing`, hoặc khi reset flag detected.
- `App.OnResume` → `Start()` lại → tick đầu tiên khôi phục `LastSeenAt = now`.

**FR-M-18: Reset flag**
- Admin set cờ qua `POST /api/device-preference/{id}/reset`.
- Mobile poll `GET /api/device-preference/reset-flag?deviceId=X` mỗi 3 phút.
- API atomic: đọc cờ rồi set về false trong cùng response.
- Nếu `true` → `Preferences.Clear()` (10 khóa) + xóa folder `audio/` + `GoToAsync("//LoadingPage")` → khách phải quét QR mới.

### 8.8 GPS Batch Logging

**FR-M-19: LocationLogService**
- Buffer các điểm GPS in-memory.
- Flush mỗi 20 giây: `POST /api/device-location-log/batch` với tối đa 500 điểm mỗi batch.
- Mỗi point: `{lat, lng, accuracyMeters?, capturedAt}`.
- API piggyback cập nhật `LastSeenAt` → nguồn heartbeat chính (cao tần hơn stall sync).

### 8.9 Stall List Page

**FR-M-20: Search + phân trang**
- PageSize = 10.
- `GetAllStallsAsync(forceRefresh: true)` mỗi lần load (known issue: bỏ qua cache).
- `SearchText` setter trigger LoadStallsAsync mỗi lần gõ (known issue: chưa debounce).
- Filter client-side trên `_allStalls`.

### 8.10 Yêu cầu chung Mobile

- HttpClient factory: `Default` + `"ApiHttp"` (BaseAddress, timeout 10s) cho REST; `"download"` (không BaseAddress, timeout 30s) cho audio.
- DI: tất cả Service = **Singleton**, tất cả ViewModel/Page = **Transient**.
- AppShell routes: `LoadingPage`, `ScanPage`, `MainPage`, `MapPage`, `StallListPage`, `TourListPage` là absolute (ShellContent); `LanguagePage`, `TourDetailPage` là relative qua `Routing.RegisterRoute`.

---

## 9. Yêu cầu chức năng – Web Admin

### 9.1 Xác thực (AuthController)

**FR-W-01:** Đăng nhập với email **hoặc** username — API so với cả `NormalizedEmail` và `NormalizedUserName`.
**FR-W-02:** Đăng ký BusinessOwner (UserName, Email, Password, PhoneNumber).
**FR-W-03:** Đăng xuất — xóa token/session-data (không revoke refresh token server-side).

**Cơ chế session:**
- Token, refreshToken, userName, userRole, plan, planExpiresAt lưu trong `HttpContext.Session` (IdleTimeout 30 phút, HttpOnly, Secure, SameSite Strict).
- `AuthTokenHandler` (DelegatingHandler) inject `Authorization: Bearer {token}` + `X-TimeZoneId: SE Asia Standard Time` vào mọi HttpClient outbound.
- `TokenExpirationFilter` global check token còn hạn trước mỗi action non-public; hết hạn → gọi `RefreshAsync` hoặc redirect Login.

### 9.2 Dashboard (AdminController)

**FR-W-04:** `/Admin/Dashboard` — 9 API calls song song `Task.WhenAll`: business, stall, languages/active, narration content, users, qrcodes (total + used), subscription-orders (completed + recent).

### 9.3 Quản lý Business

**FR-W-05..07:** CRUD Business với filter plan/search, Toggle Active.
- Admin thấy tất cả; BusinessOwner chỉ thấy business của mình (API-side filter bằng `business.OwnerUserId == userId`).

### 9.4 Quản lý Stall

**FR-W-08..11:** CRUD Stall với filter business/search, Toggle Active. Tạo stall vượt giới hạn plan → API từ chối (trừ Admin).

### 9.5 Stall Location & GeoFence

**FR-W-12..14:** CRUD StallLocation qua bản đồ Leaflet (CreateMap / EditMap). Admin chọn tọa độ bằng click hoặc drag marker, điều chỉnh `RadiusMeters` và địa chỉ.

**FR-W-15..17:** CRUD StallGeoFence (endpoint dùng `api/stall-geo-fence` chứ không phải `api/stall-geofence`).

### 9.6 Stall Media

**FR-W-18..21:** Upload ảnh (multipart) qua `POST /api/stall-media/upload`, Update, Delete. Grid view với lọc stall + isActive.

### 9.7 Narration Content & Audio

**FR-W-22..26:** CRUD Narration Content với filter stall/language/isActive. Sau khi tạo/cập nhật → API tự set `TtsStatus = Pending` (nếu plan allow TTS).

**FR-W-27: Theo dõi TTS real-time**
- Trang `show.cshtml` polling `GET /Narration/TtsStatus/{id}` → API `GET /api/stall-narration-content/{id}/tts-status` khi `TtsStatus ∈ {Pending, Processing}`.
- Dừng polling khi `Completed` / `Failed`.
- Nếu `Failed` → hiện nút **Thử lại TTS** → `POST /api/stall-narration-content/{id}/retry-tts` reset về Pending.

**FR-W-28: Upload audio giọng người**
- `PUT /api/narration-audio/{id}/upload` (multipart) — thay audio TTS bằng audio thật.
- API cập nhật `AudioUrl`, `BlobId`, set `IsTts = false`, `Provider = "Human"`.

### 9.8 Subscription & Payment

**FR-W-29:** `/Subscription/Plans` public — hiện 3 card Free/Basic/Pro. Pre-select business nếu login.

**FR-W-30:** `/Subscription/Checkout?plan=X[&businessId=Y]` — yêu cầu login + BusinessOwner/Admin. Alert đỏ + disable submit nếu business đang dùng plan cao hơn.

**FR-W-31: Mock payment**
- POST `/Subscription/ProcessPayment` với `{businessId, plan, cardNumber, cardExpiry, cardCvv, cardHolder}`.
- API strip spaces khỏi cardNumber, đúng 16 chữ số → `Completed`, sai → `Failed`.
- Thành công → extend plan + cập nhật session `UserPlan` + `UserPlanExpiresAt` (badge sidebar cập nhật ngay).

**FR-W-32: Lịch sử đơn (Admin only)**
- `/Admin/SubscriptionOrders` với filter plan/status, phân trang.
- Stats cards: TotalRevenue (chỉ Completed), TotalCompleted, TotalFailed.

### 9.9 QR Code Management (Admin only)

**FR-W-33:** CRUD QR qua `/Admin/QrCodes` — tạo với `ValidDays` + note, xem ảnh PNG, xóa.

**FR-W-34: Kiosk Auto QR**
- `/Admin/AutoQr` — Admin nhập `ValidDays`, nhấn Bắt đầu.
- JS gọi `POST /Admin/StartAutoQr` tạo mã đầu.
- JS poll `GET /Admin/PollAutoQr?id=X&validDays=N` mỗi 2 giây.
- Khi `IsUsed = true` → controller tự tạo mã mới ngay trong cùng response → JS thay QR trên màn hình.

### 9.10 Admin Subscription Management

**FR-W-35:** `/Admin/Subscription` — Admin set plan + PlanExpiresAt cho bất kỳ business nào, không qua flow payment (không kiểm downgrade).

### 9.11 Active Devices Tracking (Admin only)

**FR-W-36:** `/Admin/ActiveDevices?withinSeconds=30` (default 30, clamp [10, 300]).
- Render Razor lần đầu với data preload.
- JS polling mỗi 5 giây `GET /Admin/ActiveDevicesData?withinSeconds=X`.
- Dropdown 5 option: 30s / 60s / 120s / 180s / 300s — đổi trigger refresh ngay.
- Nút Reset mỗi dòng thiết bị → `POST /Admin/ResetDevice` (AntiForgeryToken) → `POST /api/device-preference/{id}/reset`.

### 9.12 Heatmap (Admin only)

**FR-W-37:** `/Admin/Heatmap?from=&to=&deviceId=` — default `from = now - 7 days`, `to = now` (UTC).
- `Task.WhenAll` load song song `GET /api/device-location-log/heatmap` + `GET /api/geo/stalls`.
- Preload JSON (camelCase) vào biến `rawPoints` / `rawStalls` → render Leaflet + `leaflet.heat` plugin, gradient blue→cyan→lime→yellow→red.
- Normalize weight về [0, 1] tránh outlier.
- Stall overlay với circleMarker + vòng tròn `radiusMeters`.
- Filter form dùng `method=get` reload toàn trang (không AJAX).
- Server-side cap khoảng thời gian **tối đa 90 ngày**.

### 9.13 Tour Management (Admin only)

**FR-W-38:** `/Tour` — List tour với search/phân trang, Toggle Active, Delete.

**FR-W-39: TourDesigner**
- `/Tour/Designer` (tạo mới) hoặc `/Tour/Designer?id=X` (sửa).
- Leaflet map với stall markers: xám = chưa thuộc tour, xanh = stop trong tour.
- SortableJS drag-drop để đổi thứ tự stops trong sidebar.
- Click marker xám thêm stop / click marker xanh bỏ stop.
- `POST /Tour/Save?id={optional}` JSON body với `[ValidateAntiForgeryToken]`.

### 9.14 User & Role Management (Admin only)

**FR-W-40..43:** `/Admin/UserRoleManagement` — List users với phân trang/filter, Admin tạo user mới, đổi role, toggle active.
- Admin không thể tự toggle hoặc đổi role chính mình (API check).

---

## 10. Yêu cầu chức năng – API Backend

### 10.1 Authentication

**FR-A-01..04:**
- `POST /api/auth/register/business-owner` — hash BCrypt, tạo User + UserRole(BusinessOwner) + BusinessOwnerProfile.
- `POST /api/auth/login` — chấp nhận email hoặc username; JWT 30 phút + RefreshToken 30 ngày (raw → client, SHA256 hash → DB).
- `POST /api/auth/refresh` — hash raw token, validate, revoke token cũ, sinh cặp mới.
- `POST /api/auth/logout` — revoke refresh token.

### 10.2 Geo & Device (AllowAnonymous cho Mobile)

**FR-A-05: GET /api/geo/stalls?deviceId=X**
- Resolve ngôn ngữ qua `DevicePreference` → fallback `vi` → fallback ngôn ngữ active đầu tiên.
- Filtered Include: chỉ kéo `StallNarrationContents` đúng ngôn ngữ + active.
- `PickAudioUrl`: ưu tiên voice của device → audio TTS → bất kỳ audio có URL.
- Output: `List<GeoStallDto>` mỗi stall 1 AudioUrl phù hợp nhất.
- **Piggyback:** Cập nhật `LastSeenAt = now` cho DevicePreference tương ứng (ExecuteUpdateAsync không load entity).

**FR-A-06: Active Devices (AdminOnly)**
- `GET /api/geo/active-devices?withinSeconds=30` — clamp [10, 300].
- Trả `ActiveDevicesSummaryDto {activeCount, withinSeconds, asOf, devices[]}`.

**FR-A-07: Device Preference CRUD (AllowAnonymous)**
- `GET /{deviceId}` — detail với include Language + VoiceProfile.
- `POST /` — upsert preference (insert hoặc update).
- `GET /reset-flag?deviceId=X` — đọc `NeedsReset`, atomic clear nếu true.
- `POST /{deviceId}/offline` — set `LastSeenAt = MinValue`.
- `POST /{deviceId}/reset` — (AdminOnly) set `NeedsReset = true`.

**FR-A-08: Device Location Log**
- `POST /api/device-location-log/batch` (AllowAnonymous) — max 500 điểm mỗi batch; piggyback cập nhật `LastSeenAt`.
- `GET /api/device-location-log/heatmap?from=&to=&deviceId=` (AdminOnly) — group by `(Lat, Lng)` + count. Giới hạn 90 ngày. Default 7 ngày gần nhất.

### 10.3 QR Code

**FR-A-09:** `POST /api/qrcodes/verify` (AllowAnonymous) — one-time use. Set `IsUsed=true`, `UsedAt=now`, `UsedByDeviceId`. Trả `{isValid, expiryAt}` với `expiryAt = UsedAt + ValidDays`.

**FR-A-10:** CRUD QR (AdminOnly) — Create (auto generate code), List (filter used/unused), GetImage (PNG bytes via QRCoder), Delete.

### 10.4 Narration & Audio

**FR-A-11..13: CRUD Narration Content**
- Create/Update: validate plan allow TTS → set `TtsStatus = Pending` → `TtsBackgroundService` xử lý async.
- Toggle status (`IsActive`).
- `GET {id}/tts-status` trả `{TtsStatus, TtsError, Audios[]}` cho polling.
- `POST {id}/retry-tts` reset `Pending` + clear error.

**FR-A-14: TTS Background Service**
- `PeriodicTimer(5s)` poll DB.
- Reset stale jobs: `TtsStatus = Processing` AND `UpdatedAt < now - 10min` → về Pending.
- Claim batch 5 job Pending cũ nhất → set Processing (commit trước khi gọi Azure).
- Gọi `NarrationAudioService.CreateOrUpdateFromTtsAsync`: dịch (nếu cần) → Azure Speech → Azure Blob → upsert `NarrationAudio`.
- Kết quả: `Completed` hoặc `Failed` (max 500 ký tự TtsError).

**FR-A-15: Upload audio giọng người**
- `PUT /api/narration-audio/{id}/upload` (multipart) — validate content type audio; upload Azure Blob; set `IsTts = false`, `Provider = "Human"`.

### 10.5 Subscription Order

**FR-A-16:** `POST /api/subscription-orders` (AdminOrBusinessOwner) — validate plan Basic/Pro, check downgrade, mock payment 16-digit, extend plan logic, save order + update business.

**FR-A-17:** `GET /api/subscription-orders` (AdminOnly) — list với filter plan/status/businessId.

### 10.6 Tour

**FR-A-18: Public endpoints (AllowAnonymous)**
- `GET /api/tours` — non-Admin force `IsActive=true`.
- `GET /api/tours/{id}` — tour inactive trả 404 cho non-Admin.

**FR-A-19: Admin CRUD**
- `POST /api/tours` (AdminOnly) — validate Stops không rỗng, không trùng StallId, StallId tồn tại, Name unique. Re-index `Order` 1..N.
- `PUT /api/tours/{id}` — full replace stops (DELETE + re-insert).
- `DELETE /api/tours/{id}` — cascade xóa stops.
- `PATCH /api/tours/{id}/toggle-active`.
- `POST /api/tours/{id}/stops/reorder` — validate `Count` match + `SetEquals` StallIds + re-index 1..N.

### 10.7 Business, Stall, Media, Location, GeoFence

**FR-A-20..24:** CRUD chuẩn với phân quyền `[Authorize]` policy-based + check ownership cho BusinessOwner trong service layer.

### 10.8 Language, Voice, User (Admin)

**FR-A-25:** Language CRUD (AdminOnly) — seed cứng ở migration, chưa có CRUD admin UI.

**FR-A-26:** `GET /api/tts-voice-profiles/active?languageId=X` (AllowAnonymous) — hiện chỉ có 1 action này.

**FR-A-27:** User Management (AdminOnly / self) — list, detail, create, toggle-active, update role.

### 10.9 Response Format chuẩn

```json
// Thành công
{ "success": true, "data": { ... }, "error": null }

// Lỗi
{
  "success": false,
  "data": null,
  "error": { "code": "Validation", "message": "...", "field": "Name" }
}

// List có phân trang
{
  "success": true,
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 45
  }
}
```

---

## 11. API Endpoints

### 11.1 Authentication (`api/auth`)

| Method | Endpoint | Auth |
|--------|----------|------|
| POST | `register/business-owner` | Anonymous |
| POST | `login` | Anonymous |
| POST | `refresh` | Anonymous |
| POST | `logout` | [Authorize] |

### 11.2 Geo & Device (`api/geo`, `api/device-preference`, `api/device-location-log`)

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `api/geo/stalls?deviceId=` | Anonymous |
| GET | `api/geo/active-devices?withinSeconds=` | AdminOnly |
| GET | `api/device-preference/{deviceId}` | Anonymous |
| POST | `api/device-preference` | Anonymous |
| GET | `api/device-preference/reset-flag?deviceId=` | Anonymous |
| POST | `api/device-preference/{deviceId}/offline` | Anonymous |
| POST | `api/device-preference/{deviceId}/reset` | AdminOnly |
| POST | `api/device-location-log/batch` | Anonymous |
| GET | `api/device-location-log/heatmap?from=&to=&deviceId=` | AdminOnly |

### 11.3 QR Code (`api/qrcodes`)

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `api/qrcodes` | AdminOnly |
| POST | `api/qrcodes` | AdminOnly |
| GET | `api/qrcodes/{id}` | AdminOnly |
| GET | `api/qrcodes/{id}/image` | AdminOnly |
| DELETE | `api/qrcodes/{id}` | AdminOnly |
| POST | `api/qrcodes/verify` | Anonymous |

### 11.4 Business & Stall (`api/business`, `api/stall`, `api/stall-location`, `api/stall-geo-fence`, `api/stall-media`)

| Method | Endpoint | Auth |
|--------|----------|------|
| GET / POST / GET{id} / PUT{id} / PATCH{id}/toggle-active | `api/business` | [Authorize] |
| PUT | `api/business/{id}/subscription` | AdminOnly |
| GET / POST / GET{id} / PUT{id} / PATCH{id}/toggle-active | `api/stall` | [Authorize] |
| GET / POST / GET{id} / PUT{id} / PATCH{id}/toggle-active | `api/stall-location` | [Authorize] |
| GET / POST / GET{id} / PUT{id} | `api/stall-geo-fence` | [Authorize] |
| GET / POST upload / GET{id} / PUT{id} / PUT{id}/upload / DELETE{id} | `api/stall-media` | [Authorize] |

### 11.5 Narration & Audio

| Method | Endpoint | Auth |
|--------|----------|------|
| GET / POST / GET{id} / PUT{id} / PATCH{id}/status | `api/stall-narration-content` | [Authorize] |
| GET | `api/stall-narration-content/{id}/tts-status` | [Authorize] |
| POST | `api/stall-narration-content/{id}/retry-tts` | [Authorize] |
| PUT | `api/narration-audio/{id}/upload` | AdminOrBusinessOwner |

### 11.6 Subscription & Tours

| Method | Endpoint | Auth |
|--------|----------|------|
| POST | `api/subscription-orders` | AdminOrBusinessOwner |
| GET | `api/subscription-orders` | AdminOnly |
| GET | `api/tours` | Anonymous |
| GET | `api/tours/{id}` | Anonymous |
| POST / PUT{id} / DELETE{id} / PATCH{id}/toggle-active / POST{id}/stops/reorder | `api/tours` | AdminOnly |

### 11.7 Languages, Voice, Users

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `api/languages` | AdminOnly |
| GET | `api/languages/active` | Anonymous |
| POST / PUT{id} / DELETE{id} | `api/languages` | AdminOnly |
| GET | `api/tts-voice-profiles/active?languageId=` | Anonymous |
| GET / POST / GET{id} / PATCH{id}/toggle-active / PUT{id}/role | `api/user` | [Authorize] (check IsAdmin trong action) |
| GET | `api/user/roles` | [Authorize] |

---

## 12. Tích hợp dịch vụ ngoài

### 12.1 Azure Cognitive Services Speech (TTS)

| Thuộc tính | Giá trị |
|-----------|---------|
| Mục đích | Tự sinh audio từ script text |
| Endpoint | `https://eastasia.api.cognitive.microsoft.com/` |
| Giọng mặc định | `vi-VN-HoaiMyNeural` |
| SDK | `Microsoft.CognitiveServices.Speech 1.48.2` |
| Output | `.wav` → upload Azure Blob |
| Gọi từ | `TtsBackgroundService` (không gọi trong HTTP request) |

### 12.2 Azure Blob Storage

| Thuộc tính | Giá trị |
|-----------|---------|
| Mục đích | Lưu audio và ảnh gian hàng |
| Container audio | `narration-audio` |
| Access | **`PublicAccessType.Blob`** — URL public, ai có URL đều download được (⚠️ xem §16) |
| SDK | `Azure.Storage.Blobs 12.25.0` |

### 12.3 Azure Translator

| Thuộc tính | Giá trị |
|-----------|---------|
| Mục đích | Dịch nội dung thuyết minh |
| Endpoint | `https://api.cognitive.microsofttranslator.com` |
| Version | v3.0 |
| Logic | Bỏ qua nếu source = target language code |
| Gọi từ | `NarrationAudioService.CreateOrUpdateFromTtsAsync` trước khi tổng hợp giọng |

### 12.4 Mapsui + OpenStreetMap (Mobile)

| Thuộc tính | Giá trị |
|-----------|---------|
| NuGet | `Mapsui.Maui 5.0.2` |
| Tile | OpenStreetMap (không cần API key) |
| Vẽ geofence | SkiaSharp circles + NTS geometry |

### 12.5 Leaflet + leaflet.heat (Web)

- CDN: `cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/` + `cdn.jsdelivr.net/npm/leaflet.heat@0.2.0/`
- OSM tiles, dùng cho TourDesigner, StallLocationMap, Heatmap, ActiveDevices.

---

## 13. Phân quyền & Bảo mật

### 13.1 Policies

Định nghĩa trong `Program.cs`, hằng số trong `Api/Authorization/AppPolicies.cs`:

| Policy | Hằng số | Roles |
|--------|---------|-------|
| `AdminOnly` | `AppPolicies.AdminOnly` | `Admin` |
| `AdminOrBusinessOwner` | `AppPolicies.AdminOrBusinessOwner` | `Admin`, `BusinessOwner` |

### 13.2 AppControllerBase Helpers

- `TryGetUserId(out Guid)` — lấy `NameIdentifier` từ JWT.
- `IsAdmin()`, `IsBusinessOwner()`.
- `GetTimeZone()` — đọc header `X-TimeZoneId`, fallback `SE Asia Standard Time`.
- `ConvertFromUtc(...)` — convert UTC → timezone client.

### 13.3 Ownership check

BusinessOwner chỉ thao tác dữ liệu của business mình — service/controller `.Include(s => s.Business)` rồi check `business.OwnerUserId == userId`.

### 13.4 Anonymous endpoints (Mobile)

`GeoController.GetAllStalls`, `DevicePreferenceController` (hầu hết), `DeviceLocationLogController.BatchCreate`, `QrCodeController.VerifyQrCode`, `TourController.GetTours` / `GetTourDetail`, `TtsVoiceProfileController.GetActive`, `LanguageController.GetActive` — Mobile gọi không có JWT.

### 13.5 Security settings

- Password: `BCrypt.Net-Next 4.1.0` adaptive hashing.
- JWT: HS256, `ClockSkew = Zero`, key từ `appsettings.Jwt.Key`.
- Refresh Token: 64 byte random base64 → SHA256 hash → lưu DB. Raw gửi cho client.
- Web session: `HttpOnly`, `Secure`, `SameSite = Strict`, `IdleTimeout = 30 min`.
- CSRF: `[ValidateAntiForgeryToken]` trên mọi form POST (`ResetDevice`, `ToggleActive`, `Delete`, `StartAutoQr`, `Tour/Save`, ...).
- HTTPS bắt buộc trên production.
- Azure keys trong `appsettings.json` (không commit key vào Git — dùng User Secrets / Azure Key Vault cho production).

---

## 14. Yêu cầu phi chức năng

### 14.1 Hiệu năng

| Yêu cầu | Mục tiêu |
|---------|---------|
| API response (Geo endpoints) | < 500ms |
| API response (CRUD) | < 1s |
| Mobile cold start đến MapPage | < 3s (khi có local cache) |
| Audio playback start | < 2s (từ cache local); < 5s (stream Blob) |
| SQLite read | < 100ms |
| Pagination max | 100 items/trang |
| Heatmap max range | 90 ngày |
| GPS batch max | 500 điểm |

### 14.2 Độ tin cậy

- Mobile không crash khi mất mạng (cache SQLite + xử lý null API).
- API trả `ApiResult<T>` structured error, không expose stack trace.
- Refresh token 30 ngày giữ Web Admin không logout bất ngờ.
- TTS job stale (`Processing > 10 phút`) tự động reset về `Pending` khi API restart.
- `SyncService.IsSyncing` atomic qua `Interlocked.CompareExchange` — tránh race khi nhiều tick trùng nhau.

### 14.3 Khả năng mở rộng

- API stateless (JWT) → scale horizontal dễ.
- Audio/media ở Azure Blob → không phụ thuộc local disk.
- SQLite cache Mobile giảm tải API.
- Pagination trên tất cả list endpoints.
- TTS queue trong DB — thêm instance API là thêm worker (⚠️ race condition với multi-instance hiện chưa fix — xem §16).

### 14.4 Maintainability

- Không dùng AutoMapper — mapping rõ ràng trong Service layer.
- Shared DTOs dùng chung cho 3 project.
- EF Core Fluent API tách riêng trong `Configurations/`.
- Query Extensions cho các query lặp lại trong `Infrastructure/Persistence/Extensions/`.
- `ILogger<T>` structured logging.
- Swagger Development only: `http://localhost:5299/swagger`.

### 14.5 Nền tảng Mobile

| Nền tảng | Version |
|---------|---------|
| Android | API 21+ (Android 5.0+) |
| iOS | 15.0+ |
| Windows | 10.0.17763.0+ |
| macOS Catalyst | 15.0+ |

---

## 15. Quyết định kỹ thuật chính

| Quyết định | Lý do |
|-----------|-------|
| **Không AutoMapper** | Mapping thủ công trong Service, kiểm soát rõ ràng |
| **Mapsui + OSM** (không Google Maps) | Miễn phí, không cần API key |
| **Azure Stack** (Speech + Blob + Translator) | Một nhà cung cấp, nhất quán |
| **Không SQL GEOGRAPHY** | Haversine C# đủ chính xác cho scale khu phố |
| **.NET 10.0** | Phiên bản mới nhất |
| **Timezone SE Asia Standard Time** | UTC+7, client truyền qua header `X-TimeZoneId` |
| **Mobile anonymous** | Visitor không có tài khoản; QR + DeviceId thay user |
| **Preferences thay vì SecureStorage** (Mobile) | Đủ bảo mật cho deviceId anonymous; đơn giản hơn |
| **Cache-First + SQLite** (Mobile) | UX mượt + offline support |
| **Background TTS** | Tránh HTTP timeout; cho phép dùng nhiều Azure voice cùng lúc |
| **Pull-based reset flag** | Không push trực tiếp xuống Mobile (không kết nối liên tục) |
| **Piggyback heartbeat** | Không tạo endpoint ping riêng; tái sử dụng các endpoint đã có |
| **Effective plan runtime** (không scheduler) | Đơn giản hoá; không cần hosted service để đảo plan |
| **Mock payment** | POC stage; 16-digit validation đủ demo |
| **QR vé vào app** | Kiểm soát truy cập tại sự kiện thực tế; chống abuse |
| **Tour memory cache only** (Mobile, không SQLite) | Data nhỏ, cold start API nhanh |

---

## 16. Vấn đề đã biết & Nợ kỹ thuật

### 16.1 Bảo mật

- **Azure Blob container `narration-audio` đang `PublicAccessType.Blob`** — ai có URL đều download được. Cân nhắc SAS URL hoặc proxy stream qua API.
- **`AdminController` chưa có `[Authorize]` attribute** — chỉ dựa vào `TokenExpirationFilter`. Nên bổ sung policy-level defense-in-depth.
- **`/Subscription/Success`** không yêu cầu login — có thể xem URL thành công mà không cần thanh toán.

### 16.2 Race condition

- **`TtsBackgroundService` claim job không atomic** — `SELECT` rồi `UPDATE` riêng. Multi-instance API có thể xử lý trùng job, upload Blob trùng. Fix: `ExecuteUpdateAsync` với `WHERE Status='Pending'`.
- **`QrCodeController.VerifyQrCode` set `IsUsed = true` không atomic** — 2 thiết bị scan cùng lúc đều nhận `isValid = true`. Fix: `ExecuteUpdateAsync` với `WHERE IsUsed = 0`.
- **`SyncService.LastUpdated` luôn = `DateTimeOffset.UtcNow` cho stall mới** → `LocalStallRepository.HasChanged` luôn trả true → ghi lại toàn bộ SQLite mỗi 3 phút. Fix: dùng timestamp thật từ API.

### 16.3 UX / Flow

- **`Web/Views/StallLocation/StallLocationMap.cshtml`** gọi API trực tiếp từ JS và cố đọc JWT từ `localStorage` — mà Web lưu trong Session server-side. Flow create/update không chạy được. Fix: submit qua Controller Action hoặc proxy endpoint.

### 16.4 Mobile known issues

- **`StallListViewModel` dùng `forceRefresh: true` mỗi lần load** — bỏ qua cache, luôn gọi API.
- **`SearchText` setter trigger load mỗi lần gõ** — chưa debounce (dễ spam API).
- **`CurrentFilter = "All"` placeholder** — chưa implement filter category.

### 16.5 Dead / Duplicate code

- `Api/Domain/Entities/ScanLog.cs` — có migration, chưa controller nào dùng.
- `QrCode.cs` và `QrCodeConfiguration.cs` — namespace `LocateAndMultilingualNarration.Domain.Entities` trong khi entity khác là `Api.Domain.Entities` (inconsistency, không lỗi runtime).
- `ScanViewModel` và `LanguageViewModel` — không dùng `[RelayCommand]` / `[ObservableProperty]` (dùng `ICommand` + `INotifyPropertyChanged` thủ công). Nợ refactor.

---

## 17. Backlog

| # | Tính năng | Mô tả | Phụ thuộc |
|---|-----------|-------|-----------|
| B-01 | Background GPS polling | App inactive vẫn trigger geofence | Permission OS, battery optimization |
| B-02 | Bookmark gian hàng yêu thích | Khách lưu stall ưa thích | Entity `FavoriteStall` |
| B-03 | Xem menu / thực đơn | Danh sách món ăn | Entity `Menu`, `MenuItem` |
| B-04 | Push notification | Thông báo sự kiện đặc biệt | Firebase FCM |
| B-05 | Audit log | Lịch sử chỉnh sửa | Entity `AuditLog`, middleware |
| B-06 | Role Collaborator | Cộng tác viên hỗ trợ BusinessOwner | Thêm Role + policy |
| B-07 | Dashboard lượt nghe audio | Thống kê lượt phát, ngôn ngữ phổ biến | Cần entity log playback trên Mobile |
| B-08 | Scheduler đảo plan hết hạn | Hosted service tự set Plan=Free khi hết hạn | `IHostedService` |
| B-09 | CRUD admin TTS voice profiles | Giao diện thêm/sửa voice Azure | Hiện chỉ seed cứng |
| B-10 | CRUD admin Language | Giao diện thêm/sửa ngôn ngữ | Hiện seed qua migration |
| B-11 | Gateway thanh toán thật | VNPay / MoMo / Stripe | OAuth2 + callback URL |
| B-12 | Atomic TTS claim + QR verify | Fix race với `ExecuteUpdateAsync` | §16.2 |
| B-13 | SAS URL / Proxy stream audio | Bảo mật Blob không public | §16.1 |
| B-14 | Debounce search Mobile | Stall list page | §16.4 |
| B-15 | Multi-branch stall | 1 stall nhiều chi nhánh | `StallLocation` đã hỗ trợ 1:N |

---

## Phụ lục A – Cấu hình môi trường

### Api/appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  },
  "Jwt": {
    "Key": "...",
    "Issuer": "LocateAndMultilingualNarrationApi",
    "Audience": "LocateAndMultilingualNarrationClient",
    "ExpiryMinutes": 30
  },
  "AzureSpeech": {
    "Endpoint": "https://eastasia.api.cognitive.microsoft.com/",
    "Key": "",
    "DefaultVoice": "vi-VN-HoaiMyNeural",
    "Provider": "AzureTts"
  },
  "BlobStorage": {
    "ConnectionString": "",
    "ContainerName": "narration-audio"
  },
  "AzureTranslation": {
    "Endpoint": "https://api.cognitive.microsofttranslator.com",
    "Key": "",
    "Region": "",
    "Provider": "AzureTranslator"
  },
  "QrCode": {
    "DefaultValidDays": 1,
    "BaseUrl": "https://..."
  }
}
```

**Lưu ý:** `Program.cs` CHỈ đăng ký SQL Server nếu có connection string — không fallback InMemory. Thiếu connection string → runtime lỗi.

### Mobile/DevConfig.cs

```csharp
// Hiện tại: Android emulator
public const string ApiBaseUrl = "http://10.0.2.2:5299";
```

| Môi trường | Base URL |
|-----------|---------|
| Android Emulator | `http://10.0.2.2:5299/` |
| iOS Simulator | `http://localhost:5299/` |
| Thiết bị thật (cùng LAN) | `http://<laptop-ip>:5299/` |
| Production | `https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/` |

### Web/appsettings.json

```json
{
  "ApiBaseUrl": "http://localhost:5299/"
}
```

---

## Phụ lục B – NuGet Packages

### API (`Api.csproj`, .NET 10.0)

```
BCrypt.Net-Next 4.1.0
Azure.Storage.Blobs 12.25.0
Microsoft.AspNetCore.Authentication.JwtBearer 10.0.2
Microsoft.AspNetCore.OpenApi 10.0.2
Microsoft.CognitiveServices.Speech 1.48.2
Microsoft.EntityFrameworkCore 10.0.5
Microsoft.EntityFrameworkCore.Design 10.0.5
Microsoft.EntityFrameworkCore.InMemory 10.0.5
Microsoft.EntityFrameworkCore.SqlServer 10.0.5
QRCoder 1.6.0
Swashbuckle.AspNetCore.SwaggerUI 10.1.5
System.IdentityModel.Tokens.Jwt 8.15.0
```

### Mobile (`Mobile.csproj`)

```
CommunityToolkit.Maui 14.0.1
Microsoft.Maui.Controls 10.0.51
Mapsui.Maui 5.0.2
SkiaSharp.Views.Maui.Controls 3.119.2
Plugin.Maui.Audio 4.0.0
ZXing.Net.Maui 0.7.4
ZXing.Net.Maui.Controls 0.7.4
sqlite-net-pcl 1.9.172
SQLitePCLRaw.bundle_green 2.1.10
Microsoft.Extensions.Http 10.0.5
Microsoft.Extensions.Logging.Debug 10.0.5
```

### Web (`Web.csproj`)

```
Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2
NuGet.Packaging 7.3.1
NuGet.Protocol 7.3.1
(IHttpClientFactory từ Microsoft.NET.Sdk.Web meta-package)
```

**Web Frontend Stack (CDN):**
- Tabler 1.0.0-beta20 (admin UI)
- Bootstrap 5 (bundled trong Tabler)
- Tabler Icons 3.19.0 (`ti ti-*`)
- Bootstrap Icons 1.11.1 (`bi bi-*`)
- AOS 2.3.4 (Animate On Scroll)
- jQuery + Validation + Unobtrusive (LibMan)
- Leaflet 1.9.4 + leaflet.heat 0.2.0
- SortableJS
