# CLAUDE.md – Project Context

Đây là file hướng dẫn cho Claude Code. Đọc file này trước khi làm bất kỳ task nào.

---

## Project Overview

**Tên:** Hệ thống Thuyết minh Tự động Đa ngôn ngữ – Phố Ẩm Thực  
**Mục đích:** App mobile tự động phát audio thuyết minh khi khách đến gần gian hàng (geofencing), kết hợp web admin cho doanh nghiệp quản lý nội dung đa ngôn ngữ.  
**Framework:** .NET 10.0 toàn bộ.  
**IDE:** Visual Studio Community 2026.  
**Git:** nhánh `master` (stable), `dev` (integration), feature branches từ `dev`.

---

## Solution Structure

```
LocateAndMultilingualNarration/
├── Api/        ASP.NET Core 10.0 Web API   – REST backend chính
├── Web/        ASP.NET Core 10.0 MVC       – Giao diện admin (Admin + BusinessOwner)
├── Mobile/     .NET MAUI 10.0              – App khách tham quan (anonymous)
├── Shared/     Class Library .NET 10.0     – DTOs dùng chung
└── TestAPI/    Testing project
```

---

## Architecture Tổng Quan

```
Mobile (MAUI)  ──HTTP──▶  API (ASP.NET Core)  ──EF Core──▶  SQL Server
Web (MVC)      ──HTTP──▶  API (ASP.NET Core)  ──Azure SDK──▶ Azure (Speech/Blob/Translator)
                                │
                           Shared (DTOs)
                      dùng chung bởi cả 3 tầng
```

**API** là trung tâm duy nhất xử lý business logic. Web và Mobile chỉ gọi API, không kết nối DB trực tiếp.

---

## Các Module Chính

### API – Controllers (17 controllers)

| Controller | Route | Auth |
|-----------|-------|------|
| AuthController | `/api/auth` | Anonymous |
| GeoController | `/api/geo/stalls`, `/api/geo/nearest-stall` | **AllowAnonymous** |
| DevicePreferenceController | `/api/device-preference` | **AllowAnonymous** |
| QrCodeController | `/api/qrcodes` | AdminOnly (trừ `POST /verify` là AllowAnonymous) |
| UserController | `/api/users` | [Authorize(Policy=AdminOnly)] |
| BusinessController | `/api/business` | [Authorize] |
| StallController | `/api/stall` | [Authorize] |
| StallLocationController | `/api/stall-location` | [Authorize] |
| StallGeoFenceController | `/api/stall-geofence` | [Authorize] |
| StallMediaController | `/api/stall-media` | [Authorize] |
| LanguageController | `/api/languages` | [Authorize(Roles="Admin")] |
| TtsVoiceProfileController | `/api/tts-voice-profiles` | [Authorize] |
| StallNarrationContentController | `/api/stall-narration-content` | [Authorize] |
| NarrationAudioController | `/api/narration-audio` | [Authorize] |
| SubscriptionOrderController | `/api/subscription-orders` | [Authorize] |
| VisitorPreferenceController | `/api/visitor-preference` | [Authorize] |
| VisitorLocationLogController | `/api/visitor-location-log` | [Authorize] |

```
VisitorLocationLogController và VisitorPreferenceController hiện tại không dùng vì user không cần đăng nhập
và sẽ có 1 bảng mới để lưu lịch sử di chuyển của user để phục vụ thống kê trên web.
```

**Application Services** (`Api/Application/Services/`):
- `JwtService` – sinh JWT, hash refresh token (SHA256)
- `GeoService` – Haversine, tìm stall gần nhất
- `NarrationAudioService` – Azure TTS + Blob upload
- `AzureTranslationService` – Azure Translator v3.0
- `TtsBackgroundService` – Hosted service xử lý TTS jobs bất đồng bộ (DB polling, job claiming, retry/reset logic)

### API – Entities (20 entities, `Api/Domain/Entities/`)

```
User, Role, UserRole, RefreshToken
Business, BusinessOwnerProfile, EmployeeProfile
Stall, StallLocation, StallGeoFence, StallMedia
Language, TtsVoiceProfile, StallNarrationContent, NarrationAudio
DevicePreference
SubscriptionOrder
QrCode                                                 ← vé vào app cho Mobile
VisitorProfile, VisitorPreference, VisitorLocationLog  ← dự định xóa
```

**StallNarrationContent** có thêm 2 fields TTS tracking:
- `TtsStatus` (string) – trạng thái job: `None` / `Pending` / `Processing` / `Completed` / `Failed`
- `TtsError` (string?) – thông báo lỗi nếu `TtsStatus = "Failed"`

**TtsJobStatus** (`Api/Domain/Entities/TtsJobStatus.cs`) – static class hằng số:
```csharp
TtsJobStatus.None       // không cần TTS (Free plan)
TtsJobStatus.Pending    // đã queue, chưa xử lý
TtsJobStatus.Processing // đang xử lý
TtsJobStatus.Completed  // hoàn thành
TtsJobStatus.Failed     // thất bại
```

**Business entity** có thêm 2 fields subscription:
- `Plan` (string, default `"Free"`) – gói hiện tại của business
- `PlanExpiresAt` (DateTimeOffset?) – thời điểm plan hết hạn; `null` nếu Free

### API – Subscription System (`Api/Domain/SubscriptionPlan.cs`)

Static class chứa hằng số và helper:

```csharp
SubscriptionPlan.Free    = "Free"   // 0đ, 1 stall, không TTS
SubscriptionPlan.Basic   = "Basic"  // 199,000đ/tháng, 3 stalls, có TTS
SubscriptionPlan.Pro     = "Pro"    // 499,000đ/tháng, không giới hạn, có TTS

GetMaxStalls(plan)  // Free=1, Basic=3, Pro=MaxValue
AllowsTts(plan)     // false chỉ với Free
GetPrice(plan)      // 0 / 199_000m / 499_000m
```

**Business rules áp dụng trong API:**
- `StallController.CreateStall`: kiểm tra số stall hiện tại < `GetMaxStalls(effectivePlan)` trước khi tạo (Admin bypass)
- `StallNarrationContentController`: chặn tạo nội dung TTS nếu Free plan; khi tạo/cập nhật thành công → set `TtsStatus = Pending` để background service xử lý (không gọi TTS trực tiếp nữa)
- `SubscriptionOrderController.CreateOrder`: chặn downgrade khi plan hiện tại còn hạn (`PlanRank(request.Plan) < PlanRank(business.Plan)`)

**Effective plan logic** (dùng nhất quán ở mọi nơi):
```csharp
var planIsExpired = business.PlanExpiresAt.HasValue && business.PlanExpiresAt.Value <= DateTimeOffset.UtcNow;
var effectivePlan = (planIsExpired && business.Plan != "Free") ? "Free" : business.Plan;
```

### API – UserController (`/api/users`) [AdminOnly]

- `GET /api/users/roles` – danh sách roles kèm `UserCount`
- `GET /api/users` – danh sách users có phân trang, filter theo role/status/search
- `POST /api/users` – Admin tạo user mới (body: `AdminCreateUserDto`)
- `PUT /api/users/{id}/toggle-active` – bật/tắt IsActive
- `PUT /api/users/{id}/role` – đổi role (body: `UserRoleUpdateDto`)
- `GET /api/users/{id}` – chi tiết user

### API – StallNarrationContentController – TTS endpoints (bổ sung)

- `GET /api/stall-narration-content/{id}/tts-status` – poll trạng thái TTS (trả `TtsStatusDto`)
- `POST /api/stall-narration-content/{id}/retry-tts` – retry job TTS thất bại

### API – SubscriptionOrderController (`/api/subscription-orders`)

- `POST /api/subscription-orders` [AdminOrBusinessOwner]: Mock payment — strip spaces, nếu đúng 16 chữ số → `Completed`, else → `Failed`. Khi thành công: cập nhật `business.Plan` và `business.PlanExpiresAt`. Nếu business đang có plan active, extend từ `PlanExpiresAt` hiện tại (không bắt đầu từ now).
- `GET /api/subscription-orders` [AdminOnly]: Danh sách có phân trang, filter theo `plan`, `status`, `businessId`.

### Shared – DTOs (`Shared/DTOs/`)

17 nhóm: Auth, Business, Stall, Language, TtsVoiceProfile, Narration, Geo, StallGeoFence, StallLocation, StallMedia, DevicePreference, **SubscriptionOrders**, **Users**, **QrCodes**, VisitorPreference, VisitorLocationLog, Common.

**QrCodes**:
- `QrCodeCreateDto` – ValidDays (int, số ngày hiệu lực), Note
- `QrCodeDetailDto` – Id, Code, CreatedAt, ValidDays, AccessExpiresAt (null nếu chưa quét, = UsedAt+ValidDays nếu đã quét), IsUsed, UsedAt, UsedByDeviceId, Note
- `QrCodeVerifyRequestDto` – Code, DeviceId

**SubscriptionOrders**:
- `SubscriptionOrderCreateDto` – BusinessId, Plan, CardNumber, CardExpiry, CardCvv, CardHolder
- `SubscriptionOrderDetailDto` – Id, BusinessId, BusinessName, Plan, Amount, Status, CardLastFour, PaidAt, PlanStartAt, PlanEndAt

**Users** (mới):
- `UserListItemDto` – Id, UserName, Email, Roles, IsActive, LastLoginAt, CreatedAt
- `AdminCreateUserDto` – UserName, Email, Password, PhoneNumber, RoleName
- `UserRoleUpdateDto` – RoleName
- `RoleListItemDto` – Id, Name, UserCount

**Narration** (cập nhật):
- `StallNarrationContentDetailDto` có thêm `TtsStatus` và `TtsError`
- `TtsStatusDto` – Id, TtsStatus, TtsError, Audios (list NarrationAudioDetailDto)

**Business** (cập nhật):
- `BusinessDetailDto` có thêm `Plan` và `PlanExpiresAt`
- `SubscriptionUpdateDto` (Admin dùng) – Plan, PlanExpiresAt

### Web – Controllers (`Web/Controllers/`)

`AuthController`, `HomeController`, `BusinessController`, `StallController`, `StallLocationController`, `StallGeoFenceController`, `StallMediaController`, `NarrationController`, `AdminController` (User & Role management tích hợp), `DocsController`, **`SubscriptionController`**

Web giao tiếp API qua `Web/Services/` – mỗi domain có `*ApiClient.cs` riêng. `AuthTokenHandler` (DelegatingHandler) tự inject JWT vào mọi request. `TokenExpirationFilter` kiểm tra token còn hạn.

### Web – Services (`Web/Services/`)

| Service | Mục đích |
|---------|---------|
| `ApiClient` | Base client, quản lý session (AuthToken, UserRole, UserName, **UserPlan**, **UserPlanExpiresAt**) |
| `BusinessApiClient` | CRUD business |
| `StallApiClient` | CRUD stall |
| `LanguageApiClient` | Lấy ngôn ngữ active |
| `StallNarrationContentApiClient` | CRUD narration content + `GetTtsStatusAsync`, `RetryTtsAsync` |
| `SubscriptionApiClient` | `UpdateSubscriptionAsync` – Admin cập nhật plan cho business |
| `SubscriptionOrderApiClient` | `CreateOrderAsync`, `GetOrdersAsync` – thanh toán mock + lịch sử |
| `UserApiClient` | `GetUsersAsync`, `GetRolesAsync`, `CreateUserAsync`, `ToggleActiveAsync`, `UpdateRoleAsync`, `GetUserDetailAsync` – Admin quản lý user/role |
| `QrCodeApiClient` | `GetQrCodesAsync`, `GetQrCodeAsync`, `CreateQrCodeAsync`, `DeleteQrCodeAsync`, `GetQrCodeImageAsync` – Admin quản lý mã QR |

**Session keys quan trọng** (hằng số trong `ApiClient`):
- `TokenSessionKey` = `"AuthToken"`
- `UserRoleSessionKey` = `"UserRole"`
- `UserNameSessionKey` = `"UserName"`
- `UserPlanSessionKey` = `"UserPlan"`
- `UserPlanExpiresAtSessionKey` = `"UserPlanExpiresAt"`

`StoreUserPlan(plan, expiresAt)` – gọi sau login (nếu BusinessOwner) và sau payment thành công để cập nhật session.

### Web – SubscriptionController

| Action | Route | Ghi chú |
|--------|-------|---------|
| `Plans` | GET `/Subscription/Plans` | Public. Nhận query `?highlight=X&businessId=Y` (businessId để pre-select ở Checkout) |
| `Checkout` | GET `/Subscription/Checkout?plan=X[&businessId=Y]` | Yêu cầu login + BusinessOwner/Admin. Fetch tất cả business của user. Pre-select businessId nếu có. |
| `ProcessPayment` | POST `/Subscription/ProcessPayment` | Gọi API tạo order. Thành công → cập nhật session plan + redirect Success. |
| `Success` | GET `/Subscription/Success` | Trang xác nhận. |

### Web – Views

**Subscription views** (`Web/Views/Subscription/`):
- `Plans.cshtml` – 3 card: Free, Basic, Pro. Public. Không có logic "Đang dùng". Nút Đăng ký → Checkout (kèm businessId nếu có). Nếu chưa login → redirect Login với returnUrl.
- `Checkout.cshtml` – 2 cột: Order Summary | Card form. Nếu user có nhiều business → radio card selector với badge plan hiện tại + ngày hết hạn. Hiện alert đỏ + disable nút nếu business đang dùng plan cao hơn (chặn downgrade).
- `Success.cshtml` – Trang thành công với plan badge + order ID.

**Admin views** (`Web/Views/Admin/`):
- `Subscription.cshtml` – Admin quản lý plan cho từng business (table + edit modal).
- `SubscriptionOrders.cshtml` – Lịch sử đơn thanh toán: stats cards (doanh thu, đơn thành công/thất bại) + table filter theo plan/status.
- `UserRoleManagement.cshtml` – Quản lý user & role thực (live data từ API): search/filter, phân trang, đổi role, toggle active, tạo user mới. Dùng `UserRoleManagementViewModel` (`Web/Models/`).
- `QrCodes.cshtml` – Tạo/xem/xoá mã QR: stats cards, table (cột "Hiệu lực": badge "N ngày" nếu chưa quét, ngày cụ thể nếu đã quét), create modal (nhập ValidDays), view QR image modal. Dùng `AdminQrCodesViewModel`.
- `AutoQr.cshtml` – Kiosk tạo QR tự động: nhập ValidDays → bắt đầu → hiển thị QR → JS poll 2s → khi khách quét xong tự tạo mã mới liên tục.

**Narration views** (`Web/Views/Narration/`):
- `show.cshtml` – hiển thị TTS status badge, live banner trạng thái job, error alert, và auto-refresh qua JS polling.
- `StallNarrationContentManagement.cshtml` – bảng content có cột TTS status badge.

**Business views** (`Web/Views/Business/`):
- `BusinessManagement.cshtml` – Table có thêm cột **Plan** (badge màu + ngày hết hạn/Hết hạn) và nút **Đổi gói** → `/Subscription/Plans?businessId={id}`.

**Home** (`Web/Views/Home/Index.cshtml`):
- Khi **chưa login**: hiện bảng giá 3 card (Free/Basic/Pro) ngay trên trang home (marketing).
- Khi **đã login + BusinessOwner**: hiện subscription status banner (plan badge, ngày hết hạn, nút Nâng cấp/Xem bảng giá).

**StallLocation views** (`Web/Views/StallLocation/`):
- `StallLocationMap.cshtml` – toggle hiện/ẩn tên gian hàng; marker phân biệt: xanh=đang chọn, đỏ=user, xám=public stall khác.

**Layout** (`Web/Views/Shared/_Layout.cshtml`):
- Sidebar BusinessOwner: hiện badge plan dưới logo (bg-secondary=Free, bg-info=Basic, bg-success=Pro, bg-warning=Hết hạn).
- Admin sidebar dropdown: có link "Subscription" và "Đơn đăng ký".
- Management dropdown: có link "Bảng giá" (tất cả user).

### Mobile – Pages & ViewModels

**Flow thực tế:** `ScanPage` → `LanguagePage` → `MapPage`

| Page | ViewModel | Ghi chú |
|------|-----------|---------|
| ScanPage | ScanViewModel | Quét QR để active app |
| LanguagePage | LanguageViewModel | Chọn ngôn ngữ + giọng đọc (search/filter), lưu DevicePreference, navigate thẳng MapPage |
| MainPage | MainViewModel | Shell điều hướng; quick action "Gian hàng" → StallListPage |
| MapPage | MapViewModel | Bản đồ + geofence + audio |
| StallListPage | StallListViewModel | Danh sách gian hàng có search + phân trang |
| StallPopup | – | Popup chi tiết gian hàng |

**Mobile Services** (`Mobile/Services/`) – 13 services:

```
DeviceService              – Tạo/lấy DeviceId (Preferences)
DevicePreferenceApiService – Lưu/tải preference theo DeviceId lên API
QrService                  – Verify QR qua API + lưu/kiểm tra quyền truy cập (Preferences: qr_verified + qr_expiry)
LanguageService            – Fetch active languages, memory cache (implements ILanguageService)
VoiceService               – Fetch voice profiles theo language
StallService               – Cache-first: SQLite → API sync, in-memory cache (implements IStallService)
AudioGuideService          – Play/Pause/Resume/Stop audio
AudioCacheService          – Download & cache audio files local
LocalStallRepository       – SQLite CRUD (upsert, query)
SyncService                – Orchestrate: API → SQLite → audio download
SyncBackgroundService      – Timer 3 phút + connectivity trigger
LocationLogService         – Thu thập GPS theo batch, gửi lên API thống kê di chuyển
AuthService                – ⚠️ KHÔNG SỬ DỤNG
```

**Interfaces** (`Mobile/Services/`):
- `IStallService` – GetAllStallsAsync, GetFeaturedStallsAsync, GetStallByIdAsync, RefreshAsync
- `ILanguageService` – GetActiveLanguagesAsync, RefreshAsync

**Mobile Local DB** (`Mobile/LocalDb/`): SQLite via `sqlite-net-pcl`. `LocalStall` schema, `LocalStallRepository` upsert batch.

**Cache-First strategy:**
1. Đọc SQLite → hiển thị ngay
2. Async: gọi `/api/geo/stalls?deviceId=X` → upsert SQLite → refresh UI
3. Offline: dùng SQLite data đã có

---

## Conventions & Patterns

### Mapping
- **Không dùng AutoMapper**. Mapping thủ công trong Service layer của API.
- Mobile dùng thẳng DTO từ Shared – không có Model layer riêng.

### Authentication
- JWT Bearer (30 phút) + Refresh Token (30 ngày, hash SHA256 lưu DB).
- Password: `BCrypt.Net-Next`.
- Roles: `Admin`, `BusinessOwner`. Mobile hoàn toàn anonymous.
- Mobile dùng `DeviceId` (SecureStorage) không dùng user account.
- **Login chấp nhận email hoặc username** – `AuthController` thử lookup theo username trước, nếu không có thì lookup theo email.

### Authorization

**Policies** (định nghĩa trong `Program.cs`, hằng số trong `Api/Authorization/AppPolicies.cs`):

| Policy | Hằng số | Roles được phép |
|--------|---------|----------------|
| `AdminOnly` | `AppPolicies.AdminOnly` | `Admin` |
| `AdminOrBusinessOwner` | `AppPolicies.AdminOrBusinessOwner` | `Admin`, `BusinessOwner` |

**Cách dùng trên action/controller:**
```csharp
[Authorize(Policy = AppPolicies.AdminOnly)]           // chỉ Admin
[Authorize(Policy = AppPolicies.AdminOrBusinessOwner)] // Admin hoặc BusinessOwner
[AllowAnonymous]                                       // không cần token (Mobile endpoints)
```

**Base class `AppControllerBase`** (`Api/Controllers/AppControllerBase.cs`) cung cấp helper methods cho tất cả controller kế thừa:
- `TryGetUserId(out Guid userId)` – lấy UserId từ JWT claim `NameIdentifier`
- `IsAdmin()` – kiểm tra role Admin
- `IsBusinessOwner()` – kiểm tra role BusinessOwner
- `GetTimeZone()` – đọc header `X-TimeZoneId`, fallback về `SE Asia Standard Time`
- `ConvertFromUtc(...)` – convert DateTimeOffset/DateTime từ UTC sang timezone client

**Quy tắc phân quyền theo tầng dữ liệu (Business rule):**
- `Admin` thấy và thao tác được tất cả dữ liệu.
- `BusinessOwner` chỉ thao tác được dữ liệu thuộc business của mình — check bằng `business.OwnerUserId == userId` sau khi lấy entity kèm `.Include(s => s.Business)`.
- `DevicePreferenceController` và `GeoController` phải giữ `[AllowAnonymous]` — Mobile gọi không có token.

### Response format (API)
```json
{ "success": true,  "data": {...},  "error": null }
{ "success": false, "data": null,   "error": { "code": "...", "message": "...", "field": null } }
```
Luôn wrap trong `ApiResult<T>`. List dùng `PagedResult<T>`.

### Mobile MVVM
- ViewModels kế thừa `ObservableObject` (CommunityToolkit.Maui).
- Commands dùng `[RelayCommand]` attribute.
- DI qua `MauiProgram.cs`: Singleton cho services, Transient cho ViewModels/Pages.
- HttpClient timeout: **10 giây**.

### Database
- EF Core 10.0 Fluent API, cấu hình trong `Api/Infrastructure/Persistence/Configurations/`.
- Nếu `ConnectionString` rỗng → dùng InMemory (dev/test).
- **Không dùng SQL Server GEOGRAPHY** – tính khoảng cách bằng Haversine C#.

### API Query Extensions (`Api/Infrastructure/Persistence/Extensions/`)

Thay vì viết query dài lặp đi lặp lại, dùng extension methods trên `IQueryable<TEntity>`.  
**Quy tắc:** Khi cần query lặp lại trên một entity → thêm method vào file extension tương ứng, không inline trong controller/service.

| File | Entity | Methods |
|------|--------|---------|
| `LanguageQueryExtensions.cs` | `Language` | `GetCodeByIdAsync`, `GetCodeDictionaryAsync`, `GetActiveByIdAsync`, `CodeExistsAsync` |
| `StallQueryExtensions.cs` | `Stall` | `GetByIdAsync`, `GetByIdReadOnlyAsync`, `SlugExistsAsync` |
| `TtsVoiceProfileQueryExtensions.cs` | `TtsVoiceProfile` | `IsActiveByIdAsync` |
| `DevicePreferenceQueryExtensions.cs` | `DevicePreference` | `GetByDeviceIdAsync`, `GetByDeviceIdReadOnlyAsync` |
| `UserQueryExtensions.cs` | `User` | `EmailExistsAsync`, `UserNameExistsAsync` |

**Convention đặt tên:**
- `GetByIdAsync` – có tracking (dùng trước update/delete)
- `GetByIdReadOnlyAsync` – `AsNoTracking` (chỉ đọc)
- `IsActiveByIdAsync` – trả `bool`, check tồn tại + `IsActive = true`
- `*ExistsAsync` – trả `bool`, kiểm tra unique field

**Lưu ý quan trọng:** Extension không chain được `.Include()`. Nếu query cần `.Include()` để load navigation property thì **giữ nguyên query inline**, không ép dùng extension.

### API Pagination
- Query params: `page` (default 1), `pageSize` (default 10, max 100).
- Wrap trong `PagedResult<T>`.

---

## External Services

| Service | Provider | Config key |
|---------|---------|-----------|
| Text-to-Speech | Azure Cognitive Services Speech | `AzureSpeech` |
| File Storage | Azure Blob Storage | `BlobStorage` (container: `narration-audio`) |
| Auto Translation | Azure Translator v3.0 | `AzureTranslation` |
| Map (Mobile) | Mapsui / OpenStreetMap | Không cần API key |

---

## Key NuGet Packages

**API:** `BCrypt.Net-Next 4.1.0`, `Azure.Storage.Blobs 12.25.0`, `Microsoft.CognitiveServices.Speech 1.48.2`, `Microsoft.EntityFrameworkCore 10.0.5`, `Swashbuckle.AspNetCore 10.1.5`, `QRCoder 1.6.0`

**Mobile:** `Mapsui.Maui 5.0.2`, `Plugin.Maui.Audio 4.0.0`, `ZXing.Net.Maui 0.7.4`, `sqlite-net-pcl 1.9.172`, `CommunityToolkit.Maui 14.0.1`, `SkiaSharp.Views.Maui.Controls 3.119.2`

**Web:** `Microsoft.Extensions.Http` (HttpClientFactory)

**Web Frontend Stack:**
- **Tabler** (CDN) – Admin UI kit dựa trên Bootstrap 5, dùng **default theme**
- **Bootstrap 5** (bundled trong Tabler)
- **Bootstrap Icons** (`bi-*`) – icon set cho các view cũ
- **Tabler Icons** (`ti-*`) – icon set ưu tiên cho các view mới
- **jQuery** – JavaScript utility, form validation
- **jQuery Validation + Unobtrusive** – client-side validation cho Razor forms
- Customize qua CSS variables trong `wwwroot/css/site.css` (nếu cần override màu)

**Lưu ý badge trong dark sidebar:** Luôn thêm `text-white` khi dùng badge trong sidebar (`data-bs-theme="dark"`) để tránh text bị ẩn.

---

## Môi trường & URLs

| | URL |
|-|-----|
| API (dev) | `http://localhost:5299` |
| Web (dev) | `https://localhost:7188` |
| Swagger | `http://localhost:5299/swagger` (Development only) |
| Android emulator → API | `http://10.0.2.2:5299` |

**Timezone mặc định:** SE Asia Standard Time (UTC+7).

---

## Những gì CHƯA làm (Backlog)

- Background GPS polling liên tục (geofence auto-trigger)
- Bookmark gian hàng yêu thích
- Xem menu/thực đơn gian hàng
- Dashboard thống kê (VisitorLocationLog đã có, cần aggregate)
- Audit log
- Role Collaborator
- Dashboard thống kê Web Admin (hiện chỉ có stats card tĩnh)
- Scheduler tự động áp dụng plan khi plan cũ hết hạn (hiện tại dùng effective plan logic đọc tại runtime)

---

## Cảnh báo Quan Trọng

- `AuthService` **có trong code nhưng KHÔNG thuộc flow chính**. Khách không cần đăng nhập. Không thêm logic login vào Mobile trừ khi được yêu cầu rõ ràng.
- Khi thêm entity mới → phải thêm migration EF Core và cập nhật `AppDbContext`.
- Khi thêm DTO mới → thêm vào project `Shared`, không tạo DTO riêng trong từng project.
- `GeoController` endpoints phải giữ `[AllowAnonymous]` – Mobile gọi không có token.
- `DevicePreferenceController` phải giữ `[AllowAnonymous]`.
- `QrCodeController.VerifyQrCode` (`POST /api/qrcodes/verify`) phải giữ `[AllowAnonymous]` — Mobile gọi trước khi vào app.
- **QR là vé vào app**: Admin tạo mã QR với `ValidDays` (số ngày hiệu lực). Khách quét → API tính `expiryAt = UsedAt + ValidDays` → Mobile lưu qua `IQrService.SaveAccess(expiryAt)` → `LoadingPage` check `IsAccessValid()` mỗi lần mở app. QR là one-time use (`IsUsed=true` sau lần quét đầu).
- **AutoQr kiosk**: `GET /Admin/AutoQr` + actions `StartAutoQr` (POST) và `PollAutoQr` (GET) trong AdminController. JS poll mỗi 2 giây, khi phát hiện QR đã dùng tự tạo mã mới.
- **Subscription downgrade bị chặn hoàn toàn tại API**: không thể đăng ký plan thấp hơn khi plan hiện tại còn hạn. Checkout view cũng disable nút submit phía client khi phát hiện tình huống này.
- **Mock payment**: Số thẻ sau khi strip spaces phải đúng 16 chữ số → `Completed`. Ít hơn/nhiều hơn → `Failed`. Đây là demo, không có gateway thật.
