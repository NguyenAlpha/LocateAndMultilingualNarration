# Use Cases – API Backend

> **Actors:**
> - **Web Client** – ASP.NET Core MVC app (Admin / BusinessOwner đã login).
> - **Mobile Client** – .NET MAUI app (anonymous, chỉ có DeviceId).
> - **Admin** – có JWT với role `Admin`.
> - **BusinessOwner** – có JWT với role `BusinessOwner`.
> - **TtsBackgroundService** – Hosted service chạy trong API, không phải actor bên ngoài.
> - **Azure Services** – Speech, Blob Storage, Translator v3.0.

> **Cơ chế auth chung:** JWT Bearer HS256 (30 phút) + Refresh Token SHA256 (30 ngày). Policies `AdminOnly`, `AdminOrBusinessOwner`, hoặc `[AllowAnonymous]`. Ownership check trong service cho BusinessOwner.

> **Response format:** Luôn wrap `ApiResult<T>`. List wrap `PagedResult<T>`. `AppControllerBase` cung cấp helper `TryGetUserId`, `IsAdmin`, `IsBusinessOwner`, `GetTimeZone`, `ConvertFromUtc`.

---

## Bảng tổng hợp Use Cases API

| Mã UC | Tên Use Case | Auth |
|-------|-------------|------|
| UC-A01 | Đăng ký BusinessOwner | Anonymous |
| UC-A02 | Đăng nhập & Phát hành Token | Anonymous |
| UC-A03 | Refresh Token | Anonymous |
| UC-A04 | Logout (Revoke Token) | [Authorize] |
| UC-A05 | Lấy Stall cho Mobile (GeoController) | Anonymous |
| UC-A06 | Lấy Active Devices (dashboard) | AdminOnly |
| UC-A07 | Heartbeat Thiết bị (piggyback) | Anonymous |
| UC-A08 | Device Preference Upsert | Anonymous |
| UC-A09 | Device Reset Flag Pull | Anonymous |
| UC-A10 | Admin Set Reset Flag | AdminOnly |
| UC-A11 | Device Offline Notification | Anonymous |
| UC-A12 | Upload Batch GPS Logs | Anonymous |
| UC-A13 | Heatmap Aggregation | AdminOnly |
| UC-A14 | Verify QR Code (Mobile) | Anonymous |
| UC-A15 | CRUD QR Code (Admin) | AdminOnly |
| UC-A16 | Subscription Order Create | AdminOrBusinessOwner |
| UC-A17 | Subscription Order List | AdminOnly |
| UC-A18 | Admin Cập nhật Plan Business | AdminOnly |
| UC-A19 | CRUD Narration Content | [Authorize] |
| UC-A20 | TTS Background Service | Hệ thống |
| UC-A21 | Theo dõi & Retry TTS Status | [Authorize] |
| UC-A22 | Upload Audio Giọng người | AdminOrBusinessOwner |
| UC-A23 | CRUD Tour | Mixed |
| UC-A24 | Reorder Tour Stops | AdminOnly |
| UC-A25 | CRUD Business / Stall / Location / GeoFence / Media | [Authorize] |
| UC-A26 | Language & Voice Profile | Mixed |
| UC-A27 | User & Role Management | [Authorize] |

---

## Đặc tả chi tiết

### UC-A01 – Đăng ký BusinessOwner

| Trường | Nội dung |
|--------|---------|
| **Actor** | Anonymous (Web) |
| **Endpoint** | `POST /api/auth/register/business-owner` |
| **Mô tả** | Tạo User mới với role `BusinessOwner` + `BusinessOwnerProfile`. |

**Luồng chính:**
1. Nhận `RegisterBusinessOwnerDto { userName, email, password, phoneNumber }`.
2. Validate email + username chưa tồn tại (`UserExtensions.EmailExistsAsync`, `UserNameExistsAsync`).
3. Hash password `BCrypt.HashPassword`.
4. Transaction: INSERT User + UserRole(BusinessOwner) + BusinessOwnerProfile.
5. Return `ApiResult<RegisterResponseDto>` với UserId + UserName + Roles.

**Ngoại lệ:**
- Email/Username trùng → 409 Conflict.

---

### UC-A02 – Đăng nhập & Phát hành Token

| Trường | Nội dung |
|--------|---------|
| **Actor** | Web / Mobile (chỉ Web dùng) |
| **Endpoint** | `POST /api/auth/login` |

**Luồng chính:**
1. Nhận `LoginRequestDto { email, password }`.
2. Query User với `NormalizedEmail == email.ToUpper() || NormalizedUserName == email.ToUpper()` (chấp nhận cả email và username).
3. Verify `BCrypt.Verify(password, user.PasswordHash)`.
4. Check `user.IsActive = true`.
5. `JwtService.GenerateToken(user, roles)` → JWT 30 phút với claims UserId, UserName, Roles.
6. `JwtService.GenerateRefreshToken()` → cặp raw (64 byte base64) + SHA256 hash.
7. INSERT `RefreshToken { TokenHash, ExpiresAtUtc = now + 30 ngày, DeviceId, IpAddress }`.
8. UPDATE `user.LastLoginAt = now`.
9. Return `ApiResult<LoginResponseDto> { token, expiresAt, refreshToken (raw), refreshTokenExpiresAt, userInfo, roles }`.

**Ngoại lệ:**
- Không tìm thấy / sai password → 401 "Email hoặc mật khẩu không đúng".
- `IsActive = false` → 401 "Tài khoản đã bị vô hiệu hóa".

---

### UC-A03 – Refresh Token

| Trường | Nội dung |
|--------|---------|
| **Actor** | Web (tự động khi JWT sắp hết hạn) |
| **Endpoint** | `POST /api/auth/refresh` |

**Luồng chính:**
1. Nhận `RefreshTokenRequestDto { refreshToken (raw) }`.
2. Hash `JwtService.HashRefreshToken(raw)` → SHA256.
3. Query `RefreshToken WHERE TokenHash = hash`.
4. Check `RevokedAtUtc = null AND ExpiresAtUtc > now`.
5. Load User với roles, check `IsActive`.
6. Generate JWT + RefreshToken mới.
7. UPDATE `oldRefreshToken.RevokedAtUtc = now`.
8. INSERT RefreshToken mới.
9. Return cặp token mới.

**Ngoại lệ:**
- Hash không match → 401.
- Token bị revoke hoặc hết hạn → 401.
- User inactive → 401.

---

### UC-A04 – Logout (Revoke Token)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Web |
| **Endpoint** | `POST /api/auth/logout` |

**Luồng chính:**
1. Nhận `LogoutRequestDto { refreshToken }`.
2. Hash → tìm token → set `RevokedAtUtc = now`.

**Ghi chú:** JWT không thể revoke — vẫn dùng được đến khi hết hạn 30 phút. Chỉ refresh token bị mark revoked.

---

### UC-A05 – Lấy Stall cho Mobile (GeoController.GetAllStalls)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile |
| **Endpoint** | `GET /api/geo/stalls?deviceId=X` |
| **Auth** | AllowAnonymous |

**Luồng chính:**
1. Nhận `deviceId` query param.
2. `GeoService.GetAllStallsAsync(deviceId)`:
   - Tìm `DevicePreference` theo deviceId → lấy `LanguageId`, `VoiceId`.
   - Fallback: nếu không có preference hoặc deviceId rỗng → `languageId = Language("vi")` → fallback `Language active đầu tiên`.
3. Query `StallLocations` với Filtered Include:
   - `.Include(s => s.Stall).ThenInclude(st => st.NarrationContents.Where(nc => nc.LanguageId == X && nc.IsActive))`.
   - `.ThenInclude(nc => nc.Audios)`.
   - `.Include(st => st.StallMedia.Where(m => m.IsActive))`.
4. Với mỗi location: `PickAudioUrl(audios, preferredVoice)`:
   - Ưu tiên 1: audio khớp `VoiceId` device.
   - Ưu tiên 2: audio TTS (`IsTts = true`).
   - Ưu tiên 3: audio bất kỳ có URL.
5. Map → `List<GeoStallDto>`.
6. **Piggyback heartbeat:** `ExecuteUpdateAsync SET LastSeenAt = now WHERE DeviceId = X`.
7. Return `ApiResult<List<GeoStallDto>>`.

---

### UC-A06 – Lấy Active Devices (Dashboard)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin (Web) |
| **Endpoint** | `GET /api/geo/active-devices?withinSeconds=30` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. Clamp `withinSeconds` vào `[10, 300]` (default 30).
2. `threshold = now − withinSeconds giây`.
3. Query `DevicePreferences WHERE LastSeenAt >= threshold ORDER BY LastSeenAt DESC`.
4. Map mỗi device → `ActiveDeviceItemDto { DeviceId, Platform, DeviceModel, Manufacturer, OsVersion, LastSeenAt }`.
5. Return `ApiResult<ActiveDevicesSummaryDto> { ActiveCount, WithinSeconds, AsOf, Devices }`.

---

### UC-A07 – Heartbeat Thiết bị (piggyback)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile |
| **Mô tả** | Không phải endpoint riêng. `LastSeenAt` cập nhật qua 2 endpoint khác. |

**Nguồn heartbeat:**
1. **Chính (tần suất ~20s):** `POST /api/device-location-log/batch` (xem UC-A12). Piggyback `ExecuteUpdateAsync SET LastSeenAt = now`.
2. **Phụ (tần suất ~3 phút):** `GET /api/geo/stalls` (UC-A05). Piggyback tương tự.

**Fallback:** Nếu thiết bị chưa có `DevicePreference`, `ExecuteUpdateAsync` không match dòng nào → không lỗi, không tạo mới.

---

### UC-A08 – Device Preference Upsert

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile |
| **Endpoint** | `POST /api/device-preference` |
| **Auth** | AllowAnonymous |

**Luồng chính:**
1. Nhận `DevicePreferenceUpsertDto { deviceId, languageId, voiceId?, speechRate, autoPlay, platform, deviceModel, manufacturer, osVersion }`.
2. Validate: `Language` tồn tại + active (`GetActiveByIdAsync`).
3. Nếu có `voiceId` → validate `TtsVoiceProfile` tồn tại + active.
4. Upsert:
   - Nếu chưa có `DevicePreference` với deviceId → INSERT mới với `FirstSeenAt = LastSeenAt = now`.
   - Nếu đã có → UPDATE các trường + `LastSeenAt = now`.
5. Return `ApiResult<DevicePreferenceDetailDto>` với include `Language` + `VoiceProfile` (cho Mobile cache).

---

### UC-A09 – Device Reset Flag Pull

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile (SyncBackgroundService) |
| **Endpoint** | `GET /api/device-preference/reset-flag?deviceId=X` |
| **Auth** | AllowAnonymous |

**Luồng chính:**
1. Validate deviceId không rỗng.
2. Tìm `DevicePreference`. Nếu không có → return `200 false` (chưa đăng ký preference, không có gì reset).
3. `needsReset = preference.NeedsReset`.
4. Nếu `needsReset = true` → **atomic** `ExecuteUpdateAsync SET NeedsReset = false` trong cùng response.
5. Return `ApiResult<bool>(needsReset)`.

**Ghi chú:** Atomic clear ngay trong response đảm bảo cờ không được đọc lần 2.

---

### UC-A10 – Admin Set Reset Flag

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin (Web) |
| **Endpoint** | `POST /api/device-preference/{deviceId}/reset` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. `ExecuteUpdateAsync WHERE DeviceId = X SET NeedsReset = true`.
2. Nếu rows updated = 0 → 404 "Không tìm thấy thiết bị".
3. Return `ApiResult<bool>(true)`.

---

### UC-A11 – Device Offline Notification

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile (App.OnSleep / MapPage.OnDisappearing) |
| **Endpoint** | `POST /api/device-preference/{deviceId}/offline` |
| **Auth** | AllowAnonymous |

**Luồng chính:**
1. `ExecuteUpdateAsync WHERE DeviceId = X SET LastSeenAt = DateTimeOffset.MinValue`.
2. Sentinel `0001-01-01` đảm bảo thiết bị rớt ngay khỏi `active-devices` list.
3. Return `ApiResult<bool>(true)` (không check rows).

---

### UC-A12 – Upload Batch GPS Logs

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile (LocationLogService flush) |
| **Endpoint** | `POST /api/device-location-log/batch` |
| **Auth** | AllowAnonymous |

**Luồng chính:**
1. Nhận `DeviceLocationLogBatchDto { deviceId, points[{lat, lng, accuracy?, capturedAt}] }`.
2. Validate:
   - `deviceId` không rỗng.
   - `points` không rỗng.
   - `points.Count <= 500`.
3. Map points → `DeviceLocationLog` entities (Guid mới, `Latitude/Longitude decimal(9,6)`, `CapturedAtUtc`).
4. `AddRange` + `SaveChangesAsync`.
5. **Piggyback heartbeat:** `ExecuteUpdateAsync SET LastSeenAt = now WHERE DeviceId = X`.
6. Return `ApiResult<int>(logs.Count)`.

**Ngoại lệ:**
- 400 khi vi phạm validation.

---

### UC-A13 – Heatmap Aggregation

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin (Web) |
| **Endpoint** | `GET /api/device-location-log/heatmap?from=&to=&deviceId=` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. Default: `to = now`, `from = to - 7 ngày`.
2. Validate:
   - `from <= to`.
   - `(to - from) <= 90 ngày`.
3. Query `DeviceLocationLogs WHERE CapturedAtUtc BETWEEN from AND to [AND DeviceId = X]`.
4. `GROUP BY (Latitude, Longitude)` + `COUNT(*) AS Weight`.
5. Cột decimal(9,6) tự gom nhóm các điểm trùng tọa độ (~11cm precision), không cần round client-side.
6. Return `ApiResult<List<HeatmapPointDto>>`.

---

### UC-A14 – Verify QR Code (Mobile)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mobile (ScanPage) |
| **Endpoint** | `POST /api/qrcodes/verify` |
| **Auth** | AllowAnonymous |

**Luồng chính:**
1. Nhận `QrCodeVerifyRequestDto { code, deviceId }`.
2. Query `QrCode WHERE Code = code`.
3. Nếu không tìm thấy → `200 { isValid: false, message: "Mã QR không tồn tại" }`.
4. Nếu `IsUsed = true` → `200 { isValid: false, message: "Mã QR đã được sử dụng" }`.
5. `usedAt = now`, `expiryAt = usedAt + qrCode.ValidDays`.
6. UPDATE `IsUsed = true, UsedAt = usedAt, UsedByDeviceId = deviceId`.
7. Return `ApiResult<{ isValid: true, expiryAt }>`.

**⚠️ Known issue:** SELECT rồi UPDATE không atomic. 2 thiết bị scan cùng lúc đều có thể nhận `isValid = true`. Fix: dùng `ExecuteUpdateAsync WITH WHERE IsUsed = 0`.

---

### UC-A15 – CRUD QR Code (Admin)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Endpoint** | `GET/POST /api/qrcodes`, `GET/DELETE /api/qrcodes/{id}`, `GET /api/qrcodes/{id}/image` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. List: phân trang + filter `isUsed`.
2. Create: nhận `QrCodeCreateDto { validDays, note? }`. Generate `Code` unique. INSERT.
3. Get Image: dùng `QRCoder` render `Code` thành PNG bytes, return `FileContentResult("image/png")`.
4. Delete: chỉ xóa nếu chưa dùng (hoặc xóa tùy ý — tùy cấu hình).

---

### UC-A16 – Subscription Order Create

| Trường | Nội dung |
|--------|---------|
| **Actor** | BusinessOwner / Admin (Web) |
| **Endpoint** | `POST /api/subscription-orders` |
| **Auth** | AdminOrBusinessOwner |

**Luồng chính:**
1. Nhận `SubscriptionOrderCreateDto { businessId, plan, cardNumber, cardExpiry, cardCvv, cardHolder }`.
2. Validate plan ∈ {"Basic", "Pro"}. Free không đăng ký được.
3. Load `Business` theo businessId:
   - Not found → 404.
   - BusinessOwner nhưng không phải owner → 403.
4. Check downgrade:
   - `hasActivePlan = business.Plan != "Free" AND business.PlanExpiresAt > now`.
   - Nếu `hasActivePlan AND PlanRank(request.Plan) < PlanRank(business.Plan)` → 400 "Không thể đăng ký gói thấp hơn khi plan hiện tại còn hạn".
5. Mock payment:
   - Strip spaces/dashes khỏi cardNumber.
   - Đúng 16 chữ số → `status = Completed`.
   - Khác → `status = Failed`.
6. Tính thời gian plan:
   - `hasActivePlan` → `planStartAt = business.PlanExpiresAt hiện tại` (extend).
   - Ngược lại → `planStartAt = now`.
   - `planEndAt = planStartAt + 1 tháng`.
7. INSERT `SubscriptionOrder { businessId, plan, amount, status, cardLastFour, cardHolder, paidAt, planStartAt, planEndAt }`.
8. Nếu `Completed` → UPDATE `business.Plan = request.Plan, business.PlanExpiresAt = planEndAt`.
9. Return `ApiResult<SubscriptionOrderDetailDto>`.

---

### UC-A17 – Subscription Order List

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Endpoint** | `GET /api/subscription-orders?page=&pageSize=&plan=&status=&businessId=` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. Query với filter plan / status / businessId.
2. Return `PagedResult<SubscriptionOrderDetailDto>`.

---

### UC-A18 – Admin Cập nhật Plan Business

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Endpoint** | `PUT /api/business/{id}/subscription` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. Nhận `SubscriptionUpdateDto { plan, planExpiresAt }`.
2. UPDATE trực tiếp `business.Plan` + `business.PlanExpiresAt` không validate downgrade.
3. Return `ApiResult<BusinessDetailDto>`.

**Ghi chú:** Endpoint này bypass flow payment và rules thông thường. Admin có toàn quyền.

---

### UC-A19 – CRUD Narration Content

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin / BusinessOwner |
| **Endpoint** | CRUD `api/stall-narration-content` |
| **Auth** | [Authorize] |

**Luồng chính:**

**List:** `GET` với filter `stallId, languageId, isActive`, phân trang.

**Detail:** `GET /{id}` trả `StallNarrationContentWithAudiosDto` (content + audios + TtsStatus + TtsError).

**Create:** `POST`:
1. Nhận `StallNarrationContentCreateDto { stallId, languageId, title, description, scriptText }`.
2. Validate: Stall + Language tồn tại; chưa có content cho cặp (stall, language).
3. Check plan của business: nếu `effectivePlan = "Free"` và có scriptText → 400 "Plan Free không hỗ trợ TTS".
4. INSERT với `TtsStatus = Pending` (để BG service xử lý).

**Update:** `PUT /{id}`:
1. Update title/description/scriptText/IsActive.
2. Nếu scriptText đổi → reset `TtsStatus = Pending` + clear `TtsError`.

**Toggle Status:** `PATCH /{id}/status` với `{isActive}`.

---

### UC-A20 – TTS Background Service

| Trường | Nội dung |
|--------|---------|
| **Actor** | Hosted Service (API internal) |
| **Mô tả** | Chu kỳ 5 giây claim batch 5 job Pending → gọi Azure TTS + Translator + Blob → cập nhật status. |

**Luồng chính:**
1. **Khởi động API:** Reset stale jobs — `WHERE TtsStatus = "Processing" AND UpdatedAt < now - 10 phút` → set `Pending`.
2. **Mỗi 5 giây (`PeriodicTimer`):**
   - Query 5 `StallNarrationContent WHERE TtsStatus = "Pending" ORDER BY CreatedAt ASC`.
   - Nếu không có → skip tick.
   - Nếu có → UPDATE tất cả 5 job → `TtsStatus = "Processing"` **commit trước khi xử lý**.
3. **Xử lý từng job tuần tự** (không song song để tránh bão Azure request):
   - `NarrationAudioService.CreateOrUpdateFromTtsAsync(contentId, scriptText, languageId)`:
     - Query `TtsVoiceProfiles` active theo `languageId`.
     - Nếu cần (source != target): gọi Azure Translator v3.0 dịch.
     - Foreach voice profile:
       - Azure Speech SDK tổng hợp SSML → audio bytes (.wav).
       - Upload `Azure Blob Storage` container `narration-audio`.
       - Upsert `NarrationAudio { AudioUrl, BlobId, IsTts = true, TtsVoiceProfileId }`.
   - Thành công → UPDATE content `TtsStatus = "Completed"`, `TtsError = null`, `UpdatedAt = now`.
   - Thất bại (Azure lỗi, network, ...) → UPDATE `TtsStatus = "Failed"`, `TtsError = ex.Message` (max 500 ký tự).

**⚠️ Known issue:** Claim không atomic (SELECT → UPDATE riêng). Multi-instance có thể xử lý trùng. Fix: `ExecuteUpdateAsync` với `WHERE TtsStatus = "Pending"`.

---

### UC-A21 – Theo dõi & Retry TTS Status

| Trường | Nội dung |
|--------|---------|
| **Actor** | Web Client (trang show.cshtml) |
| **Endpoint** | `GET /api/stall-narration-content/{id}/tts-status`, `POST /api/stall-narration-content/{id}/retry-tts` |
| **Auth** | [Authorize] |

**Luồng chính:**

**TTS Status:** trả `TtsStatusDto { Id, TtsStatus, TtsError, Audios[] }` — Web poll khi Pending/Processing.

**Retry:**
1. UPDATE content `TtsStatus = "Pending"`, `TtsError = null`.
2. `TtsBackgroundService` sẽ nhận job ở tick kế.
3. Return `ApiResult<bool>(true)`.

---

### UC-A22 – Upload Audio Giọng người

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin / BusinessOwner |
| **Endpoint** | `PUT /api/narration-audio/{id}/upload` |
| **Auth** | AdminOrBusinessOwner |
| **Content-Type** | multipart/form-data |

**Luồng chính:**
1. Load `NarrationAudio` kèm Include `NarrationContent.Stall.Business`.
2. Nếu không phải Admin → check `business.OwnerUserId == userId`.
3. Validate `audioFile.ContentType` ∈ `{ audio/mpeg, audio/mp3, audio/wav, audio/ogg, audio/aac, audio/flac, audio/webm }`.
4. Upload Azure Blob:
   - `blobName = "narration-audio/{contentId}/{timestamp}{ext}"`.
   - Tạo container nếu chưa có (`PublicAccessType.Blob`).
   - `BlobClient.UploadAsync` với content-type header.
5. UPDATE audio:
   - `AudioUrl = blobClient.Uri.ToString()`.
   - `BlobId = blobName`.
   - `IsTts = false`, `Voice = null`, `Provider = "Human"`.
   - `DurationSeconds = null` (chưa compute).
6. Return `ApiResult<NarrationAudioDetailDto>`.

---

### UC-A23 – CRUD Tour

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mixed (List/Detail anonymous, CRUD AdminOnly) |
| **Endpoint** | `api/tours` |

**List (AllowAnonymous):**
1. Nhận `page, pageSize, search, isActive`.
2. Non-Admin → force `IsActive = true`. Admin có thể filter.
3. Projection với `StopCount = t.Stops.Count`.
4. Return `PagedResult<TourListItemDto>`.

**Detail (AllowAnonymous):**
1. Include `Stops.OrderBy(Order) → Stall → StallLocations + StallMedia`.
2. Nếu không tìm thấy hoặc tour inactive + caller không phải Admin → 404.
3. Map `TourDetailDto` với `MapStopDetail` chọn primary location (active trước) + thumbnail (image MediaType, SortOrder asc).

**Create (AdminOnly):**
1. Validate:
   - Stops không rỗng.
   - Không trùng `StallId` (compare `Distinct().Count` với `Stops.Count`).
   - All `StallId` tồn tại (`COUNT(Stalls WHERE Id IN)`).
   - Name unique (`NameExistsAsync`).
2. `CreatedByUserId = userId từ JWT`.
3. **Re-index Order 1..N** theo thứ tự client gửi (bỏ `Order` client).
4. INSERT Tour + TourStops.
5. Reload với Include → return detail.

**Update (AdminOnly):**
1. Cùng bộ validate như Create.
2. Check Name đổi: chỉ check unique nếu thực sự khác.
3. **Full replace Stops:** `RemoveRange(tour.Stops)` + `tour.Stops.Clear()` + add lại theo payload.
4. Re-index Order 1..N.
5. SaveChanges (một transaction).

**Delete (AdminOnly):**
- FK Cascade trên `TourStop.TourId` → EF tự xóa stops.

**Toggle Active (AdminOnly):**
- `tour.IsActive = !tour.IsActive`.

---

### UC-A24 – Reorder Tour Stops

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Endpoint** | `POST /api/tours/{id}/stops/reorder` |
| **Auth** | AdminOnly |

**Luồng chính:**
1. Nhận `List<TourStopReorderDto> { stallId, order }`.
2. Validate:
   - Request không rỗng.
   - Tour tồn tại.
   - `request.Count == tour.Stops.Count` (không thêm/bớt qua endpoint này).
   - `request.StallIds.SetEquals(tour.StallIds)` (chỉ đổi thứ tự).
3. Loop: set `stop.Order = i + 1` theo `request.OrderBy(r => r.Order)`.
4. SaveChanges.
5. Return reloaded detail.

**Ghi chú:** Web hiện gộp Reorder vào Update full replace, endpoint này có sẵn nhưng chưa bị gọi từ Web.

---

### UC-A25 – CRUD Business / Stall / Location / GeoFence / Media

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Auth** | [Authorize] (ownership check trong service) |

**Business (`/api/business`):** CRUD + ToggleActive. BusinessOwner chỉ thấy business của mình (filter `OwnerUserId == userId`).

**Stall (`/api/stall`):** CRUD + ToggleActive. Check ownership qua `.Include(s => s.Business)`. **Create** kiểm tra giới hạn plan: `CountByBusinessAsync(businessId) < GetMaxStalls(effectivePlan)` (Admin bypass).

**StallLocation (`/api/stall-location`):** CRUD + ToggleActive. Mỗi Stall có thể có nhiều location (1:N).

**StallGeoFence (`/api/stall-geo-fence`):** CRUD. Lưu ý route dùng dấu gạch giữa `stall-geo-fence`, không phải `stall-geofence`.

**StallMedia (`/api/stall-media`):**
- `POST /upload` (multipart) — upload ảnh Azure Blob.
- `PUT /{id}/upload` (multipart) — thay ảnh.
- CRUD metadata (caption, sortOrder, isActive).
- `DELETE /{id}`.

---

### UC-A26 – Language & Voice Profile

| Trường | Nội dung |
|--------|---------|
| **Actor** | Mixed |

**Language:**
- `GET /api/languages/active` (Anonymous) — Mobile gọi để render danh sách.
- `GET /api/languages` (AdminOnly) — list admin.
- `POST/PUT/DELETE /api/languages` (AdminOnly).

**TTS Voice Profile:**
- `GET /api/tts-voice-profiles/active?languageId=X` (AllowAnonymous) — hiện **chỉ có action này**, chưa có CRUD admin.

---

### UC-A27 – User & Role Management

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin (hầu hết action); self cho GetById |
| **Endpoint** | `api/user` |
| **Auth** | [Authorize] class-level; action-level check `IsAdmin()` |

**Endpoints:**
- `GET /roles` — danh sách roles kèm `UserCount`.
- `GET /` — list với phân trang/filter role/status/search (AdminOnly trong action).
- `POST /` — Admin tạo user (`AdminCreateUserDto`).
- `PATCH /{id}/toggle-active` — AdminOnly.
- `PUT /{id}/role` — AdminOnly.
- `GET /{id}` — Admin hoặc chính chủ (self-check).

**Ngoại lệ:**
- Admin không được tự toggle hoặc đổi role chính mình.
- `UserDetailDto` đã loại bỏ `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp` (đã fix).
