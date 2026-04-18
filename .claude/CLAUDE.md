# CLAUDE.md – Project Context

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

### API – Controllers (18 controllers + 1 base, `Api/Controllers/`)

| Controller | Route | Auth |
|-----------|-------|------|
| AuthController | `/api/auth` | Anonymous (register-business-owner / login / refresh / logout) |
| AppControllerBase | — | Abstract base (helpers: `TryGetUserId`, `IsAdmin`, `IsBusinessOwner`, `GetTimeZone`, `ConvertFromUtc`) |
| GeoController | `/api/geo` | Hỗn hợp: `GET stalls` = AllowAnonymous; `GET active-devices` = AdminOnly |
| DevicePreferenceController | `/api/device-preference` | **AllowAnonymous** |
| DeviceLocationLogController | `/api/device-location-log` | **AllowAnonymous** – POST `batch` tối đa 500 điểm GPS |
| QrCodeController | `/api/qrcodes` | `[Authorize(AdminOnly)]` (trừ `POST /verify` = AllowAnonymous) |
| UserController | `/api/user` | `[Authorize]` class-level; action-level tự check `IsAdmin()` |
| BusinessController | `/api/Business` | `[Authorize]`; `PUT {id}/subscription` = AdminOnly |
| StallController | `/api/Stall` | `[Authorize]` (Admin hoặc owner business) |
| StallLocationController | `/api/stall-location` | `[Authorize]` |
| StallGeoFenceController | `/api/stall-geo-fence` | `[Authorize]` (lưu ý route dùng `stall-geo-fence`, không phải `stall-geofence`) |
| StallMediaController | `/api/stall-media` | `[Authorize]` (có `POST upload` multipart) |
| LanguageController | `/api/languages` | AdminOnly; `GET active` = AllowAnonymous |
| TtsVoiceProfileController | `/api/tts-voice-profiles` | **Chỉ có 1 action** `GET active` = AllowAnonymous (chưa có CRUD admin) |
| StallNarrationContentController | `/api/stall-narration-content` | `[Authorize]` – có `GET {id}/tts-status`, `POST {id}/retry-tts` |
| NarrationAudioController | `/api/narration-audio` | `[Authorize]` – chỉ có `PUT {id}/upload` (upload audio người thật) |
| SubscriptionOrderController | `/api/subscription-orders` | `POST` = AdminOrBusinessOwner; `GET` = AdminOnly |

**Application Services** (`Api/Application/Services/`):
- `JwtService` – sinh JWT HS256, refresh token 64-byte random, hash SHA256
- `GeoService` – resolve DevicePreference, filter narration theo language + voice; có Haversine nhưng hiện không còn endpoint `nearest-stall`
- `NarrationAudioService` – Azure TTS + Blob upload (`CreateOrUpdateFromTtsAsync`)
- `AzureTranslationService` – wrapper Azure Translator v3.0 (chuẩn hoá code về 2 ký tự)
- `TtsBackgroundService` – **Hosted service** `PeriodicTimer(5s)`, claim batch 5 job Pending → Processing, retry sau `StaleThreshold=10min`

### API – Entities (22 entities, `Api/Domain/Entities/`)

```
User, Role, UserRole, RefreshToken,
Business, BusinessOwnerProfile, EmployeeProfile,
Stall, StallLocation, StallGeoFence, StallMedia,
Language, TtsVoiceProfile, StallNarrationContent, NarrationAudio,
DevicePreference, DeviceLocationLog, ScanLog,
SubscriptionOrder, QrCode, QrCodeConfiguration
```

Ngoài ra: `TtsJobStatus` là **static class hằng số** (`None/Pending/Processing/Completed/Failed`), không phải entity. `ScanLog` đã có migration nhưng chưa controller nào dùng.

⚠️ **Cảnh báo namespace:** `QrCode.cs` và `QrCodeConfiguration.cs` đang dùng namespace `LocateAndMultilingualNarration.Domain.Entities` trong khi mọi entity khác là `Api.Domain.Entities`. Không lỗi runtime nhưng inconsistency.

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
- `StallController.CreateStall`: kiểm tra số stall < `GetMaxStalls(effectivePlan)` (Admin bypass)
- `StallNarrationContentController`: chặn tạo content TTS nếu Free plan; tạo/cập nhật xong → set `TtsStatus = Pending` để `TtsBackgroundService` xử lý (không gọi TTS trực tiếp)
- `SubscriptionOrderController.CreateOrder`: chặn downgrade khi plan hiện tại còn hạn (`PlanRank(request.Plan) < PlanRank(business.Plan)`)

**Effective plan logic** (dùng nhất quán ở mọi nơi):
```csharp
var planIsExpired = business.PlanExpiresAt.HasValue && business.PlanExpiresAt.Value <= DateTimeOffset.UtcNow;
var effectivePlan = (planIsExpired && business.Plan != "Free") ? "Free" : business.Plan;
```

### API – UserController (`/api/user`) [Authorize]

- `GET /api/User/roles` – danh sách roles kèm `UserCount`
- `GET /api/User` – users có phân trang, filter theo role/status/search
- `POST /api/User` – Admin tạo user (body: `AdminCreateUserDto`)
- `PATCH /api/User/{id}/toggle-active` – bật/tắt IsActive
- `PUT /api/User/{id}/role` – đổi role (body: `UserRoleUpdateDto`)
- `GET /api/User/{id}` – chi tiết user (logic nội bộ: cho phép Admin hoặc chính chủ user)

### API – SubscriptionOrderController (`/api/subscription-orders`)

- `POST /api/subscription-orders` [AdminOrBusinessOwner]: Mock payment — strip spaces, nếu đúng 16 chữ số → `Completed`, else → `Failed`. Khi thành công: cập nhật `business.Plan` và `business.PlanExpiresAt`. Nếu business đang có plan active, extend từ `PlanExpiresAt` hiện tại.
- `GET /api/subscription-orders` [AdminOnly]: Phân trang, filter `plan` / `status` / `businessId`.

### Shared – DTOs (`Shared/DTOs/`)

Nhóm: Auth, Businesses, Common, DeviceLocationLogs, DevicePreferences, Geo, Languages, Narrations, QrCodes, StallGeoFences, StallLocations, StallMedia, Stalls, SubscriptionOrders, TtsVoiceProfiles, Users.

**QrCodes**:
- `QrCodeCreateDto` – `ValidDays` (số ngày hiệu lực), `Note`
- `QrCodeDetailDto` – Id, Code, CreatedAt, ValidDays, AccessExpiresAt (null nếu chưa quét, `=UsedAt+ValidDays` nếu đã quét), IsUsed, UsedAt, UsedByDeviceId, Note
- `QrCodeVerifyRequestDto` – Code, DeviceId

**SubscriptionOrders**:
- `SubscriptionOrderCreateDto` – BusinessId, Plan, CardNumber, CardExpiry, CardCvv, CardHolder
- `SubscriptionOrderDetailDto` – Id, BusinessId, BusinessName, Plan, Amount, Status, CardLastFour, PaidAt, PlanStartAt, PlanEndAt

**Users**:
- `UserListItemDto` – Id, UserName, Email, Roles, IsActive, LastLoginAt, CreatedAt
- `UserDetailDto` – Id, UserName, Email, PhoneNumber, Roles, IsActive, LastLoginAt, CreatedAt, DisplayName, Sex, DateOfBirth, LockoutEnd, BusinessOwnerProfile, EmployeeProfile
- `AdminCreateUserDto` – UserName, Email, Password, PhoneNumber, RoleName
- `UserRoleUpdateDto` – RoleName
- `RoleListItemDto` – Id, Name, UserCount

**Narrations**:
- `StallNarrationContentDetailDto` có `TtsStatus` và `TtsError`
- `TtsStatusDto` – Id, TtsStatus, TtsError, Audios (list `NarrationAudioDetailDto`)

**Businesses**:
- `BusinessDetailDto` có `Plan` và `PlanExpiresAt`
- `SubscriptionUpdateDto` (Admin dùng) – Plan, PlanExpiresAt

**Geo** (thêm mới):
- `ActiveDeviceItemDto`, `ActiveDevicesSummaryDto` – cho dashboard Active Devices

**DeviceLocationLogs**:
- `DeviceLocationLogBatchDto` – DeviceId, list `LocationPointDto` (Lat/Lng/At)

### Web – Controllers (`Web/Controllers/`) – 11 controllers

`AuthController`, `HomeController`, `AdminController` (tích hợp: Dashboard, UserRoleManagement, QrCodes, AutoQr, ActiveDevices, Subscription, SubscriptionOrders), `BusinessController`, `StallController`, `StallLocationController`, `StallGeoFenceController`, `StallMediaController`, `NarrationController`, `SubscriptionController`, `DocsController`.

Web giao tiếp API qua `Web/Services/` – mỗi domain có `*ApiClient.cs` riêng. `AuthTokenHandler` (DelegatingHandler) tự inject JWT + header `X-TimeZoneId: SE Asia Standard Time` vào mọi request. `TokenExpirationFilter` (global filter) kiểm token còn hạn với path không public, nếu hết hạn thì gọi `RefreshAsync`.

### Web – Services (`Web/Services/`) – 15 files

| Service | Mục đích |
|---------|---------|
| `ApiClient` | Base client, quản session + login/register/refresh |
| `AuthTokenHandler` | DelegatingHandler inject Bearer + timezone header |
| `BusinessApiClient` | CRUD business + `plan/sortBy/sortDir` filters |
| `StallApiClient` | CRUD stall |
| `StallLocationApiClient` | CRUD stall location |
| `StallGeoFenceApiClient` | CRUD geofence (endpoint `api/stall-geo-fence`) |
| `StallMediaApiClient` | Upload/delete media |
| `LanguageApiClient` | `GetActiveLanguagesAsync` |
| `StallNarrationContentApiClient` | CRUD content + `GetTtsStatusAsync`, `RetryTtsAsync` |
| `NarrationAudioApiClient` | `UploadHumanAudioAsync` (multipart) |
| `SubscriptionApiClient` | `UpdateSubscriptionAsync` – Admin cập nhật plan |
| `SubscriptionOrderApiClient` | `CreateOrderAsync`, `GetOrdersAsync` |
| `UserApiClient` | Users & roles – Admin |
| `QrCodeApiClient` | QR CRUD + `GetQrCodeImageAsync` |
| `DeviceApiClient` | `GetActiveDevicesAsync` → `/api/geo/active-devices` |

**Session keys** (hằng số trong `ApiClient`):
- `TokenSessionKey` = `"AuthToken"`, `TokenExpiresAtSessionKey` = `"AuthTokenExpiresAt"`
- `RefreshTokenSessionKey` = `"RefreshToken"`, `RefreshTokenExpiresAtSessionKey` = `"RefreshTokenExpiresAt"`
- `UserNameSessionKey` = `"UserName"`, `UserRoleSessionKey` = `"UserRole"`
- `UserPlanSessionKey` = `"UserPlan"`, `UserPlanExpiresAtSessionKey` = `"UserPlanExpiresAt"`

`StoreUserPlan(plan, expiresAt)` – gọi sau login (nếu BusinessOwner) và sau payment thành công.
Session config: `IdleTimeout = 30 phút`, `HttpOnly`, `IsEssential`, `SecurePolicy = Always`, `SameSite = Strict`.

### Web – SubscriptionController

| Action | Route | Ghi chú |
|--------|-------|---------|
| `Plans` | GET `/Subscription/Plans` | Public. Query `?highlight=X&businessId=Y` |
| `Checkout` | GET `/Subscription/Checkout?plan=X[&businessId=Y]` | Yêu cầu login + BusinessOwner/Admin |
| `ProcessPayment` | POST `/Subscription/ProcessPayment` | Gọi API → thành công cập nhật session plan |
| `Success` | GET `/Subscription/Success` | Trang xác nhận (hiện không yêu cầu login – nên siết lại) |

### Web – Views (`Web/Views/`)

- `Admin/`: `Dashboard.cshtml`, `UserRoleManagement.cshtml`, `Subscription.cshtml`, `SubscriptionOrders.cshtml`, `QrCodes.cshtml`, `AutoQr.cshtml`, `ActiveDevices.cshtml`
- `Auth/`: `Login.cshtml`, `Register.cshtml`
- `Business/`: `BusinessManagement.cshtml`
- `Home/`: `Index.cshtml`, `Privacy.cshtml`, `Error.cshtml`
- `Narration/`: `StallNarrationContentManagement.cshtml`, `show.cshtml`
- `Stall/`: `StallManagement.cshtml`
- `StallGeoFence/`: `StallGeoFenceIndex.cshtml`
- `StallLocation/`: `StallLocationIndex.cshtml`, `StallLocationMap.cshtml`
- `StallMedia/`: `StallMediaManagement.cshtml`
- `Subscription/`: `Plans.cshtml`, `Checkout.cshtml`, `Success.cshtml`
- `Shared/`: `_Layout.cshtml`, `Error.cshtml`, `_ValidationScriptsPartial.cshtml`

**Home** (`Home/Index.cshtml`):
- Chưa login: bảng giá 3 card (Free/Basic/Pro)
- Đã login + BusinessOwner: banner plan (badge, ngày hết hạn, nút Nâng cấp)

**ViewModels** (`Web/Models/`): `LoginViewModel`, `RegisterViewModel`, `HomeViewModel`, `AdminDashboardViewModel`, `BusinessManagementViewModel`, `BusinessFormViewModel`, `StallManagementViewModel`, `StallFormViewModel`, `StallLocationManagementViewModel`, `StallLocationFormViewModel`, `StallGeoFenceManagementViewModel`, `StallGeoFenceFormViewModel`, `StallMediaManagementViewModel`, `StallMediaFormViewModel`, `StallNarrationContentManagementViewModel`, `StallNarrationContentShowViewModel`, `SubscriptionManagementViewModel` / `SubscriptionFormViewModel`, `SubscriptionPlanViewModel` (gồm `PlansViewModel`, `BusinessSelectItem`, `CheckoutViewModel`, `SubscriptionOrdersViewModel`), `UserRoleManagementViewModel`, `AdminQrCodesViewModel`, `ErrorViewModel`.

### Mobile – Pages & ViewModels

**Flow thực tế:** `LoadingPage` (splash) → kiểm QR + language → `ScanPage` (nếu chưa có/hết hạn QR) → `LanguagePage` (nếu chưa có preference) → `//MainPage` hoặc `//MapPage`.

| Page | ViewModel | Ghi chú |
|------|-----------|---------|
| LoadingPage | – (code-behind) | Quyết định điều hướng splash |
| ScanPage | ScanViewModel | Quét QR để active app |
| LanguagePage | LanguageViewModel | Chọn ngôn ngữ + voice trong cùng trang, lưu DevicePreference + LocalPreference |
| MainPage | MainViewModel | Shell home; quick action → StallListPage |
| MapPage | MapViewModel | Bản đồ + geofence + audio queue |
| StallListPage | StallListViewModel | Search + phân trang (⚠️ ViewModel chưa được đăng ký DI, resolve qua `ServiceHelper` sẽ lỗi) |
| ProfilePage | ProfileViewModel | Đổi ngôn ngữ/voice/speechRate |
| StallPopup | dùng chung MapViewModel | Popup chi tiết gian hàng |

**Mobile Services** (`Mobile/Services/`) – 14 services:

```
DeviceService              – GetOrCreateDeviceId (Preferences key "device_id") + GetDeviceInfo
DevicePreferenceApiService – GET/POST /api/device-preference
LocalPreferenceService     – Lưu DevicePreference vào Preferences (8 key "pref_*"), offline-first
QrService                  – Verify QR qua API + lưu "qr_verified"/"qr_expiry" (Preferences)
LanguageService            – GET /api/languages/active, cache 15 phút
VoiceService               – GET /api/tts-voice-profiles/active?languageId=...
StallService               – Cache-first: memory → SQLite → API /api/geo/stalls, cache 10 phút
AudioGuideService          – Wrap Plugin.Maui.Audio; event PlaybackCompleted
AudioCacheService          – Download MP3 về {AppDataDirectory}/audio/{lang}/{stallId}.mp3
SyncService                – Orchestrate API → SQLite → audio (semaphore 3 song song)
SyncBackgroundService      – PeriodicTimer: StallSyncInterval=3 phút, FlushInterval=1 phút + ConnectivityChanged
LocationLogService         – Buffer GPS in-memory, batch POST /api/device-location-log/batch, sample 5s, max 500 điểm
GpsPollingService          – Vòng lặp Geolocation delay 1s, event LocationUpdated (dùng trong MapViewModel)
LocalStallRepository       – SQLite stalls.db3 (LocalDb/), upsert batch có diff check
AuthService                – ⚠️ DEAD CODE — vẫn có file nhưng DI đã comment, không flow nào gọi
```

**Interfaces** (`Mobile/Services/`):
- `IStallService`, `ILanguageService`, `IVoiceService`, `IAudioGuideService`, `IAudioCacheService`, `ISyncService`, `ISyncBackgroundService`, `ILocationLogService`, `IGpsPollingService`, `IQrService`, `ILocalPreferenceService`, `IDeviceService`, `IDevicePreferenceApiService`, `ILocalStallRepository`.

**DI Registration** (`MauiProgram.cs`):
- **Singleton** cho tất cả services trên (ngoại trừ AuthService đã comment).
- **Transient ViewModel**: MainViewModel, MapViewModel, LanguageViewModel, ScanViewModel, ProfileViewModel, StallListViewModel.
- **Transient Page**: MapPage, LoadingPage, LanguagePage, StallPopup, ProfilePage. `ScanPage`, `MainPage`, `StallListPage` dùng `ServiceHelper.GetService<VM>()` trong ctor không tham số.

**Mobile Local DB** (`Mobile/LocalDb/`): SQLite via `sqlite-net-pcl`. `LocalStall` schema, `LocalStallRepository` upsert batch.

**Cache-First strategy:**
1. Đọc SQLite → hiển thị ngay
2. Async: gọi `/api/geo/stalls?deviceId=X` → upsert SQLite → refresh UI
3. Offline: dùng SQLite data đã có

**Phân biệt Preference storage:**
- `DeviceService` → lưu 1 key `device_id` (GUID) trong `Preferences`.
- `LocalPreferenceService` → 8 key `pref_language_id / pref_language_code / pref_language_name / pref_language_display_name / pref_language_flag_code / pref_voice_id / pref_speech_rate / pref_auto_play` – snapshot DevicePreferenceDetailDto.
- `QrService` → `qr_verified`, `qr_expiry`.
- `LanguageHelper` → `app_selected_language`, `app_selected_voice` (⚠️ trùng với `LocalPreferenceService`, nên hợp nhất).
- **Không dùng `SecureStorage`** ở đâu.

---

## Conventions & Patterns

### Mapping
- **Không dùng AutoMapper**. Mapping thủ công trong Service layer của API.
- Mobile dùng thẳng DTO từ Shared – không có Model layer riêng.

### Authentication
- JWT Bearer (`ClockSkew = Zero`) + Refresh Token 30 ngày (hash SHA256 lưu DB).
- Password: `BCrypt.Net-Next`.
- Roles: `Admin`, `BusinessOwner`. Mobile hoàn toàn anonymous.
- Mobile dùng `DeviceId` (Preferences, không phải SecureStorage) không dùng user account.
- **Login chấp nhận email hoặc username** – `AuthController` dùng `NormalizedEmail == X || NormalizedUserName == X`.

### Authorization

**Policies** (định nghĩa trong `Program.cs`, hằng số trong `Api/Authorization/AppPolicies.cs`):

| Policy | Hằng số | Roles được phép |
|--------|---------|----------------|
| `AdminOnly` | `AppPolicies.AdminOnly` | `Admin` |
| `AdminOrBusinessOwner` | `AppPolicies.AdminOrBusinessOwner` | `Admin`, `BusinessOwner` |

**Cách dùng trên action/controller:**
```csharp
[Authorize(Policy = AppPolicies.AdminOnly)]
[Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
[AllowAnonymous]
```

**Base class `AppControllerBase`** cung cấp helpers cho mọi controller kế thừa:
- `TryGetUserId(out Guid userId)` – lấy UserId từ JWT claim `NameIdentifier`
- `IsAdmin()`, `IsBusinessOwner()`
- `GetTimeZone()` – đọc header `X-TimeZoneId`, fallback `SE Asia Standard Time`
- `ConvertFromUtc(...)` – convert UTC → timezone client

**Quy tắc phân quyền theo tầng dữ liệu:**
- `Admin` thấy và thao tác tất cả dữ liệu.
- `BusinessOwner` chỉ thao tác dữ liệu business của mình — check `business.OwnerUserId == userId` sau khi `.Include(s => s.Business)`.
- `DevicePreferenceController`, `DeviceLocationLogController`, `GeoController.GetAllStalls`, `QrCodeController.VerifyQrCode` phải giữ `[AllowAnonymous]` — Mobile gọi không có token.

### Response format (API)
```json
{ "success": true,  "data": {...},  "error": null }
{ "success": false, "data": null,   "error": { "code": "...", "message": "...", "field": null } }
```
Luôn wrap trong `ApiResult<T>`. List dùng `PagedResult<T>`.

### Mobile MVVM
- ViewModels kế thừa `ObservableObject` (CommunityToolkit.Mvvm).
- Commands dùng `[RelayCommand]` attribute.
- DI qua `MauiProgram.cs`: **Singleton** cho services, **Transient** cho ViewModels/Pages.
- HttpClient timeout: **10 giây**.

### Database
- EF Core 10.0 Fluent API, cấu hình trong `Api/Infrastructure/Persistence/Configurations/`.
- `Program.cs` CHỈ đăng ký SQL Server nếu có connection string — **không tự fallback InMemory**. Nếu thiếu connection string → runtime lỗi.
- **Không dùng SQL Server GEOGRAPHY** – tính khoảng cách bằng Haversine C#.

### API Query Extensions (`Api/Infrastructure/Persistence/Extensions/`)

Khi cần query lặp lại trên một entity → thêm method vào file extension tương ứng, không inline trong controller/service.

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

**Lưu ý:** Extension không chain được `.Include()`. Query cần `.Include()` thì giữ nguyên inline.

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

**API** (`Api.csproj`, .NET 10.0):
- `BCrypt.Net-Next 4.1.0`, `Azure.Storage.Blobs 12.25.0`
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.2`, `Microsoft.AspNetCore.OpenApi 10.0.2`
- `Microsoft.CognitiveServices.Speech 1.48.2`
- `Microsoft.EntityFrameworkCore 10.0.5` (+ Design / InMemory / SqlServer)
- `QRCoder 1.6.0`, `Swashbuckle.AspNetCore.SwaggerUI 10.1.5`, `System.IdentityModel.Tokens.Jwt 8.15.0`

**Mobile** (`Mobile.csproj`):
- `CommunityToolkit.Maui 14.0.1`, `Microsoft.Maui.Controls 10.0.51`
- `Mapsui.Maui 5.0.2`, `SkiaSharp.Views.Maui.Controls 3.119.2`
- `Plugin.Maui.Audio 4.0.0`, `ZXing.Net.Maui 0.7.4` + `ZXing.Net.Maui.Controls 0.7.4`
- `sqlite-net-pcl 1.9.172`, `SQLitePCLRaw.bundle_green 2.1.10`
- `Microsoft.Extensions.Http 10.0.5`, `Microsoft.Extensions.Logging.Debug 10.0.5`

**Web** (`Web.csproj`):
- `Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2`
- `NuGet.Packaging 7.3.1`, `NuGet.Protocol 7.3.1`
- `IHttpClientFactory` lấy từ `Microsoft.NET.Sdk.Web` meta-package.

**Web Frontend Stack** (qua CDN trong `_Layout.cshtml`):
- **Tabler 1.0.0-beta20** – Admin UI kit dựa trên Bootstrap 5 (CDN jsdelivr)
- **Bootstrap 5** (bundled trong Tabler)
- **Tabler Icons 3.19.0** webfont (`ti ti-*`) – icon ưu tiên cho view mới
- **Bootstrap Icons 1.11.1** (`bi bi-*`) – icon cho view cũ
- **AOS 2.3.4** – Animate On Scroll
- **jQuery + Validation + Unobtrusive** – local qua LibMan (`~/lib/`)

**Lưu ý badge trong dark sidebar:** Luôn thêm `text-white` khi dùng badge trong sidebar (`data-bs-theme="dark"`) để tránh text bị ẩn.

---

## Môi trường & URLs

| | URL |
|-|-----|
| API (dev) | `http://localhost:5299` |
| Web (dev) | `https://localhost:7188` |
| API (prod mặc định trong `appsettings.json`) | `https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/` |
| Swagger | `http://localhost:5299/swagger` (Development) |
| Android emulator → API | `http://10.0.2.2:5299` |

`Mobile/DevConfig.cs` hiện đặt `http://10.0.2.2:5299` (Android emulator). Khi deploy hoặc chạy trên thiết bị thật, đổi sang IP laptop hoặc URL production.

**Timezone mặc định:** SE Asia Standard Time (UTC+7).

---

## Những gì CHƯA làm (Backlog)

- Background GPS polling khi app inactive (GpsPollingService hiện chỉ chạy khi MapPage active)
- Bookmark gian hàng yêu thích
- Xem menu/thực đơn gian hàng
- Dashboard thống kê thực (DeviceLocationLog đã có, cần aggregate)
- Audit log
- Role Collaborator
- Scheduler tự động áp dụng plan khi plan cũ hết hạn (hiện dùng effective plan logic tại runtime)
- CRUD admin cho TTS voice profiles (mới chỉ có `GET active`)
- CRUD admin cho Language (seed fix-cứng)

---

## Vấn đề Kỹ thuật Đã Biết (Technical Debt / Known Issues)

### Bảo mật / nghiêm trọng
- **`StallsController` (`/api/stalls`) là endpoint `AllowAnonymous` trùng chức năng GeoController**, trả `StallMapDto` không wrap `ApiResult`, không kiểm QR. Nên xoá sau khi verify Mobile không gọi.
- **Azure Blob container được set `PublicAccessType.Blob`** trong `NarrationAudioService` và `NarrationAudioController` — file audio public với mọi người có URL. Cân nhắc chuyển sang `None` + SAS URL (cần refactor Mobile audio player để chấp nhận SAS token).

### Race condition
- **`TtsBackgroundService` claim job không nguyên tử** (SELECT rồi UPDATE riêng) — nếu chạy nhiều instance API sẽ xử lý job trùng, upload Blob trùng. Nên dùng `ExecuteUpdateAsync` với WHERE Status='Pending' để atomic.
- **`QrCodeController.VerifyQrCode` set `IsUsed=true` không atomic** — 2 thiết bị scan cùng lúc đều nhận `isValid=true`. Dùng `ExecuteUpdateAsync` với WHERE `IsUsed=0`.
- **Mobile `SyncService.IsSyncing` là bool non-volatile** và `FlushAsync` không có lock — 2 tick (ConnectivityChanged + Timer) có thể fire song song gây duplicate POST.
- **`MapViewModel` subscribe event Singleton nhưng không unsubscribe** → memory leak và audio duplicate khi điều hướng lại. Cần Implement `IDisposable` + gỡ event trong `OnDisappearing`.
- **`SyncService.LastUpdated` luôn set = `DateTimeOffset.UtcNow` cho stall mới** → `LocalStallRepository.HasChanged` luôn trả true → ghi toàn bộ SQLite mỗi 3 phút. Cần dùng timestamp thật từ API hoặc bỏ so sánh.

### Flow / UX
- **`Web/Views/StallLocation/StallLocationMap.cshtml`** gọi API trực tiếp từ JS và cố đọc JWT từ `localStorage` — mà Web lưu JWT trong **Session server-side**. Flow create/update location trên bản đồ KHÔNG chạy được. Sửa: submit qua Controller Action hoặc proxy endpoint.
- **`AdminController`** chưa có `[Authorize]` attribute — chỉ dựa vào `TokenExpirationFilter`; nên bổ sung policy-level để defense-in-depth.
- **`LanguageHelper` (Mobile) và `LocalPreferenceService` trùng key** cho language/voice — nguy cơ out-of-sync.

### Dead / Duplicate code
- `Api/Controllers/StallsController.cs` – duplicate của `GeoController`
- `Api/Controllers/DevicePreferenceController.cs` có 2 action `Save` và `Upsert` trùng chức năng
- `Mobile/Services/AuthService.cs` – vẫn còn file, DI đã comment
- `Api/Domain/Entities/ScanLog.cs` – có migration, không controller nào dùng

### Đã fix (commit gần đây)
- ✅ `UserDetailDto` lộ `PasswordHash`/`SecurityStamp`/`ConcurrencyStamp` — xoá khỏi DTO và mapping trong `UserController`
- ✅ `Mobile/DevConfig.cs` hardcode URL production → đổi về `http://10.0.2.2:5299`
- ✅ `StallLocationController` fallback `ApiBaseUrl` trỏ sai → đổi về `http://localhost:5299/`
- ✅ `QrService.IsAccessValid()` dùng `DateTimeOffset.TryParse` + `RoundtripKind` thay cho `DateTime.TryParse`
- ✅ `StallListViewModel` đăng ký Transient trong `MauiProgram.cs`
- ✅ `GpsPollingService` xoá `.ContinueWith(_ => { })` nuốt cancellation
- ✅ `AudioGuideService.Pause/Resume/Stop` thêm `_playerLock` + tách `StopInternal` để tránh race với `PlayAsync`
- ✅ `AdminController.StartAutoQr` thêm `[ValidateAntiForgeryToken]` + view gửi header `RequestVerificationToken`
- ✅ `ZXing.Net.Maui` gỡ khỏi `Api.csproj` và `Web.csproj` (chỉ còn Mobile)

---

## Cảnh báo Quan Trọng

- `AuthService` (Mobile) **dead code** – khách không đăng nhập. Không thêm logic login vào Mobile trừ khi được yêu cầu rõ ràng.
- Khi thêm entity mới → phải thêm migration EF Core và cập nhật `AppDbContext`.
- Khi thêm DTO mới → đặt trong project `Shared`, không tạo DTO riêng trong từng project.
- `GeoController.GetAllStalls`, `DevicePreferenceController`, `DeviceLocationLogController`, `QrCodeController.VerifyQrCode` phải giữ `[AllowAnonymous]` – Mobile gọi không có token.
- **QR là vé vào app**: Admin tạo mã với `ValidDays`. Khách quét → API tính `AccessExpiresAt = UsedAt + ValidDays` → Mobile lưu qua `IQrService.SaveAccess(expiryAt)` → `LoadingPage` check `IsAccessValid()` mỗi lần mở app. QR là **one-time use** (`IsUsed=true` sau lần quét đầu).
- **AutoQr kiosk**: `GET /Admin/AutoQr` + `StartAutoQr` (POST) + `PollAutoQr` (GET). JS poll mỗi 2s, khi phát hiện QR đã dùng tự tạo mã mới.
- **Subscription downgrade bị chặn tại API**: không đăng ký được plan thấp hơn khi plan hiện tại còn hạn. Checkout view cũng disable nút submit client-side.
- **Mock payment**: số thẻ strip spaces = 16 chữ số → `Completed`, khác → `Failed`. Demo only.
- **Web không lưu JWT vào `localStorage`** — lưu trong ASP.NET Session server-side. JS trong view không cần (và không được) gửi `Authorization` header trực tiếp.
