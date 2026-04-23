# Use Cases – Web Admin

> **Actors:**
> - **Admin** – Quản trị viên Ban Tổ Chức, toàn quyền hệ thống.
> - **BusinessOwner** – Chủ doanh nghiệp, chỉ thao tác dữ liệu của business mình sở hữu.
> - **Anonymous** – Người chưa đăng nhập (chỉ xem bảng giá).

> **Cơ chế auth chung:** Token lưu trong ASP.NET Session server-side (không localStorage). `AuthTokenHandler` inject Bearer + `X-TimeZoneId` vào mọi HttpClient outbound. `TokenExpirationFilter` check token trước mỗi action non-public; hết hạn → `RefreshAsync` hoặc redirect Login.

---

## Bảng tổng hợp Use Cases Web Admin

| Mã UC | Tên Use Case | Actor |
|-------|-------------|-------|
| UC-W01 | Đăng nhập | Admin, BusinessOwner |
| UC-W02 | Đăng ký BusinessOwner | Anonymous |
| UC-W03 | Đăng xuất | Admin, BusinessOwner |
| UC-W04 | Xem Dashboard Admin | Admin |
| UC-W05 | CRUD Doanh nghiệp | Admin (all), BusinessOwner (self) |
| UC-W06 | CRUD Gian hàng | Admin, BusinessOwner |
| UC-W07 | Đặt vị trí Gian hàng trên bản đồ | Admin, BusinessOwner |
| UC-W08 | Quản lý GeoFence | Admin, BusinessOwner |
| UC-W09 | Quản lý Media Gian hàng | Admin, BusinessOwner |
| UC-W10 | Quản lý Nội dung Thuyết minh | Admin, BusinessOwner |
| UC-W11 | Theo dõi & Retry TTS | Admin, BusinessOwner |
| UC-W12 | Upload Audio giọng người | Admin, BusinessOwner |
| UC-W13 | Xem bảng giá | Anonymous, Admin, BusinessOwner |
| UC-W14 | Thanh toán đăng ký Plan | BusinessOwner, Admin |
| UC-W15 | Xem Lịch sử Đơn đăng ký | Admin |
| UC-W16 | Admin cập nhật Subscription Business | Admin |
| UC-W17 | Quản lý Mã QR | Admin |
| UC-W18 | Kiosk Tạo QR Tự động | Admin |
| UC-W19 | Quản lý User & Role | Admin |
| UC-W20 | Theo dõi Thiết bị Online real-time | Admin |
| UC-W21 | Reset thiết bị từ xa | Admin |
| UC-W22 | Xem Bản đồ nhiệt (Heatmap) | Admin |
| UC-W23 | Quản lý Tour | Admin |
| UC-W24 | Thiết kế Tour (TourDesigner) | Admin |

---

## Đặc tả chi tiết

### UC-W01 – Đăng nhập

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | Nhập email **hoặc** username + password để nhận JWT + RefreshToken. |
| **Tiền điều kiện** | Tài khoản đã tồn tại. |
| **Hậu điều kiện** | Token + userName + userRole + (nếu BusinessOwner) plan + planExpiresAt lưu Session; redirect Home. |

**Luồng chính:**
1. Truy cập `/Auth/Login`.
2. Nhập email (hoặc username) + password, submit.
3. Web `POST /api/auth/login`.
4. API so `NormalizedEmail` hoặc `NormalizedUserName`, verify BCrypt, check `IsActive`.
5. API phát JWT (30 phút) + RefreshToken (30 ngày SHA256 lưu DB).
6. Web lưu Session + (nếu BusinessOwner) gọi `GetBusinessesAsync` để lấy plan → `StoreUserPlan(plan, expiresAt)`.
7. Redirect `/Home/Index`.

**Luồng thay thế:**
- **4a.** Sai credentials → "Email hoặc mật khẩu không đúng".
- **4b.** `IsActive = false` → "Tài khoản đã bị vô hiệu hóa".

---

### UC-W02 – Đăng ký BusinessOwner

| Trường | Nội dung |
|--------|---------|
| **Actor** | Anonymous |
| **Mô tả** | Tạo tài khoản BusinessOwner mới. |
| **Tiền điều kiện** | Email + UserName chưa tồn tại. |
| **Hậu điều kiện** | User mới tạo với role `BusinessOwner` + `BusinessOwnerProfile`; redirect về Login. |

**Luồng chính:**
1. `/Auth/Register` → nhập UserName, Email, Password, PhoneNumber.
2. Web `POST /api/auth/register/business-owner`.
3. API hash BCrypt, tạo User + UserRole(BusinessOwner) + BusinessOwnerProfile.
4. Redirect `/Auth/Login`.

**Luồng thay thế:**
- **3a.** Email hoặc username trùng → API trả lỗi → modal hiện lỗi.

---

### UC-W03 – Đăng xuất

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | Xóa token khỏi Session, redirect về Login. |

**Luồng chính:**
1. Nhấn Đăng xuất trên sidebar.
2. Web `POST /Auth/Logout` → `ApiClient.ClearToken()`.
3. Xóa tất cả session keys: AuthToken, RefreshToken, UserName, UserRole, UserPlan, UserPlanExpiresAt.
4. Redirect `/Auth/Login`.

**Ghi chú:** JWT stateless — server không thể "thu hồi" token đã cấp. Nếu ai có bản sao token, vẫn dùng được đến khi hết hạn 30 phút.

---

### UC-W04 – Xem Dashboard Admin

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Dashboard tổng hợp stats + danh sách gần đây. 9 API calls song song `Task.WhenAll`. |
| **Tiền điều kiện** | Đã đăng nhập Admin. |

**Luồng chính:**
1. `/Admin/Dashboard` → `AdminController.Dashboard`.
2. `Task.WhenAll` 9 calls:
   - `GET /api/business?page=1&pageSize=5`
   - `GET /api/stall?page=1&pageSize=5`
   - `GET /api/languages/active`
   - `GET /api/stall-narration-content?page=1&pageSize=1`
   - `GET /api/user?page=1&pageSize=1`
   - `GET /api/qrcodes?page=1&pageSize=1`
   - `GET /api/qrcodes?isUsed=true&page=1&pageSize=1`
   - `GET /api/subscription-orders?status=Completed&pageSize=100` (tính TotalRevenue)
   - `GET /api/subscription-orders?page=1&pageSize=5`
3. Tổng hợp `AdminDashboardViewModel` → render stats cards + recent tables.

**Luồng thay thế:**
- Một API lỗi → giá trị tương ứng = 0 / rỗng, không crash.

---

### UC-W05 – CRUD Doanh nghiệp

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin (toàn bộ), BusinessOwner (chỉ business của mình) |
| **Mô tả** | Xem list có phân trang + filter plan + search; Create / Update / ToggleActive. |

**Luồng chính:**
1. `/Business` → `GET /api/business?page=&pageSize=&search=&plan=&sortBy=&sortDir=`.
2. Tạo mới: modal form → `POST /api/business`.
3. Sửa: modal Edit → `PUT /api/business/{id}`.
4. Toggle: `PATCH /api/business/{id}/toggle-active`.

**Luồng thay thế:**
- BusinessOwner thấy danh sách tự động filter (API-side check `OwnerUserId == userId`).
- Validation lỗi → giữ modal mở với thông báo lỗi.

---

### UC-W06 – CRUD Gian hàng

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | CRUD Stall với filter business + search. API check giới hạn plan khi Create. |

**Luồng chính:**
1. `/Stall` → song song `GET /api/business` (dropdown) + `GET /api/stall?...`.
2. Tạo: chọn Business, điền tên + slug + liên hệ → `POST /api/stall`.
3. Sửa: `PUT /api/stall/{id}`.
4. Toggle: `PATCH /api/stall/{id}/toggle-active`.

**Luồng thay thế:**
- **2a.** Plan Free và đã có 1 stall → API trả 400 "Đã vượt giới hạn plan" (Admin bypass).
- **2b.** Slug trùng → API trả conflict.

---

### UC-W07 – Đặt vị trí Gian hàng trên bản đồ

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | Click / drag marker trên bản đồ Leaflet để đặt tọa độ + radius cho stall. |

**Luồng chính:**
1. Tạo: `GET /StallLocation/CreateMap?stallId=X` → load dropdown stalls + tất cả locations hiện có làm markers.
2. Click map → marker mới xuất hiện + lat/lng vào form.
3. Nhập address, radius → submit → `POST /api/stall-location`.
4. Sửa: `GET /StallLocation/EditMap?id=X` → load location hiện tại → kéo marker → `PUT /api/stall-location/{id}`.

**⚠️ Known issue:** View `StallLocationMap.cshtml` hiện gọi API trực tiếp từ JS và cố đọc JWT từ localStorage — không chạy được. Cần submit qua Controller Action.

---

### UC-W08 – Quản lý GeoFence

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | CRUD StallGeoFence với `RadiusMeters` + IsActive. |

**Luồng chính:**
1. `/StallGeoFence` → `GET /api/stall-geo-fence` (lưu ý route có dấu gạch).
2. Tạo / Sửa / Toggle / Delete.

---

### UC-W09 – Quản lý Media Gian hàng

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | Upload ảnh lên Azure Blob, quản lý grid. |

**Luồng chính:**
1. `/StallMedia` → song song dropdown stalls + `GET /api/stall-media?stallId=&isActive=` (grid).
2. Upload: multipart form → `POST /api/stall-media/upload`.
3. Cập nhật ảnh / caption / sortOrder: `PUT /api/stall-media/{id}/upload` (multipart).
4. Xóa: `DELETE /api/stall-media/{id}`.

---

### UC-W10 – Quản lý Nội dung Thuyết minh

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | CRUD StallNarrationContent. Tạo/Cập nhật → API tự set `TtsStatus = Pending` nếu plan cho phép TTS. |

**Luồng chính:**
1. `/Narration/StallNarrationContents` → 3 API song song (stalls, languages, contents).
2. Tạo: form chọn Stall + Language + Title + Description + ScriptText → `POST /api/stall-narration-content`.
3. Xem chi tiết: `/Narration/Show/{id}` → `GET /api/stall-narration-content/{id}` trả content + audios list.
4. Sửa: `PUT /api/stall-narration-content/{id}` → set TtsStatus=Pending lại để regenerate audio.
5. Toggle `IsActive`: `PATCH /api/stall-narration-content/{id}/status`.

**Luồng thay thế:**
- **2a.** Plan Free → API trả lỗi "Plan Free không hỗ trợ TTS" khi có ScriptText.

---

### UC-W11 – Theo dõi & Retry TTS

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | Trang show.cshtml polling trạng thái TTS, hiện Retry nếu Failed. |

**Luồng chính:**
1. Mở `Show/{id}` — hiển thị badge TtsStatus (Pending/Processing/Completed/Failed).
2. Nếu Pending/Processing → JS `setInterval` gọi `GET /Narration/TtsStatus/{id}` → `GET /api/stall-narration-content/{id}/tts-status`.
3. API trả `{TtsStatus, TtsError, Audios[]}` → cập nhật badge + audio player.
4. Khi Completed → badge xanh + audio player hiện, **dừng polling**.
5. Khi Failed → badge đỏ + hiện TtsError + nút **Thử lại TTS**, **dừng polling**.

**Retry:**
1. Nhấn "Thử lại TTS" → `POST /Narration/RetryTts/{id}` → `POST /api/stall-narration-content/{id}/retry-tts`.
2. API set `TtsStatus = Pending` + clear TtsError.
3. `TtsBackgroundService` sẽ nhận job ở tick kế tiếp.
4. Redirect Show/{id} → polling restart.

---

### UC-W12 – Upload Audio giọng người

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin, BusinessOwner |
| **Mô tả** | Upload file audio thật (mp3/wav) thay thế audio TTS cho một NarrationAudio entry. |

**Luồng chính:**
1. Từ Show/{id}, chọn file audio, submit.
2. Web `POST /Narration/UploadAudio` (multipart: audioId + narrationContentId + IFormFile).
3. Web `PUT /api/narration-audio/{audioId}/upload` (multipart).
4. API upload Azure Blob → set `AudioUrl`, `BlobId`, `IsTts=false`, `Provider="Human"`, clear `Voice`.
5. Redirect Show + thông báo thành công.

---

### UC-W13 – Xem bảng giá

| Trường | Nội dung |
|--------|---------|
| **Actor** | Anonymous, Admin, BusinessOwner |
| **Mô tả** | Trang public hiển thị 3 plan Free / Basic / Pro. Nếu login → pre-select business cho Checkout. |

**Luồng chính:**
1. `/Subscription/Plans?highlight=X&businessId=Y`.
2. Nếu login → `GET /api/business?page=1&pageSize=100` để lấy danh sách + plan hiện tại.
3. Render 3 card:
   - Free: 0đ, 1 stall, không TTS.
   - Basic: 199k/tháng, 3 stalls, có TTS.
   - Pro: 499k/tháng, không giới hạn, có TTS.
4. Nhấn "Đăng ký":
   - Chưa login → redirect Login?returnUrl=Checkout.
   - Đã login → redirect Checkout?plan=X&businessId=Y.

---

### UC-W14 – Thanh toán đăng ký Plan

| Trường | Nội dung |
|--------|---------|
| **Actor** | BusinessOwner, Admin |
| **Mô tả** | Mock payment với 16-digit card validation. Thành công → extend plan + cập nhật session. |

**Luồng chính:**
1. `/Subscription/Checkout?plan=Basic[&businessId=Y]`.
2. Web check Session: chưa login → redirect Login; không phải BusinessOwner/Admin → redirect Plans.
3. `GetBusinessesAsync` → nếu 0 business → redirect Plans + "Cần tạo business trước".
4. Render form thẻ + Order Summary. Nếu business đang dùng plan cao hơn → alert đỏ + disable submit.
5. Submit → `POST /Subscription/ProcessPayment`.
6. Web `POST /api/subscription-orders` với `{businessId, plan, cardNumber, cardExpiry, cardCvv, cardHolder}`.
7. API:
   - Check downgrade: nếu `hasActivePlan AND PlanRank(request) < PlanRank(business.Plan)` → 400.
   - Strip spaces, nếu 16 chữ số → `Completed`; khác → `Failed`.
   - Lưu `SubscriptionOrder`.
   - Nếu `Completed` → extend `business.Plan` + `business.PlanExpiresAt`.
8. Response:
   - API lỗi → Checkout + "Thanh toán thất bại".
   - Failed → Checkout + "Thẻ không hợp lệ".
   - Completed → `StoreUserPlan(plan, endAt)` → badge sidebar cập nhật ngay → redirect Success?plan=X.

**Ghi chú:**
- **Downgrade bị chặn** ở API khi plan hiện tại còn hạn.
- **Extend logic:** Business đang active → `planStartAt = PlanExpiresAt hiện tại` (gia hạn từ ngày kết thúc cũ). Ngược lại → `planStartAt = now`.

---

### UC-W15 – Xem Lịch sử Đơn đăng ký

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Bảng lịch sử đơn với filter plan/status, stats cards tổng doanh thu + completed + failed. |

**Luồng chính:**
1. `/Admin/SubscriptionOrders?page=&plan=&status=`.
2. Web `GET /api/subscription-orders?...`.
3. Render bảng + stats (tính trên trang hiện tại: TotalRevenue chỉ Completed, TotalCompleted, TotalFailed).

---

### UC-W16 – Admin cập nhật Subscription Business

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Set plan + PlanExpiresAt tuỳ ý cho bất kỳ business. Không qua flow payment, không kiểm downgrade. |

**Luồng chính:**
1. `/Admin/Subscription?page=&search=&plan=` → bảng businesses.
2. Mở modal Sửa → chọn Plan mới + PlanExpiresAt → submit.
3. Web `POST /Admin/UpdateSubscription` → `PUT /api/business/{id}/subscription` (AdminOnly).
4. API update trực tiếp không validate — Admin có toàn quyền.

---

### UC-W17 – Quản lý Mã QR

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | CRUD mã QR. Mã là vé vào app cho khách, one-time use với `ValidDays`. |

**Luồng chính:**
1. `/Admin/QrCodes?page=&pageSize=` → `GET /api/qrcodes?page=...`.
2. Tạo: modal nhập ValidDays + Note → `POST /api/qrcodes`.
3. Xem ảnh QR: `GET /Admin/GetQrImage?id=X` → `GET /api/qrcodes/{id}/image` trả PNG bytes → hiển thị modal.
4. Xóa: `POST /Admin/DeleteQrCode?id=X` → `DELETE /api/qrcodes/{id}`.

**Ghi chú:**
- Mã một lần: khách scan → `IsUsed = true`.
- Chưa quét → badge "N ngày" (hiển thị ValidDays).
- Đã quét → hiện ngày hết hạn cụ thể (`UsedAt + ValidDays`).

---

### UC-W18 – Kiosk Tạo QR Tự động

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin (đặt màn hình kiosk tại cổng) |
| **Mô tả** | Màn hình tự phát mã QR mới liên tục, không cần thao tác thủ công. |

**Luồng chính:**
1. `/Admin/AutoQr` → nhập ValidDays + nhấn Bắt đầu.
2. JS `POST /Admin/StartAutoQr` với `[ValidateAntiForgeryToken]` → tạo mã đầu + trả {id, code, imageUrl}.
3. JS `GET /Admin/GetQrImage?id=X` → hiện PNG lên màn hình.
4. Poll mỗi 2 giây: `GET /Admin/PollAutoQr?id=X&validDays=N`.
5. Controller gọi `GET /api/qrcodes/{id}` check `IsUsed`:
   - `false` → trả `{used: false}` → JS giữ nguyên QR.
   - `true` → controller tự tạo mã mới ngay → trả `{used: true, id, code, imageUrl}` → JS thay ảnh không reload.

---

### UC-W19 – Quản lý User & Role

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Xem users có phân trang/filter, tạo user mới, đổi role, toggle active. |

**Luồng chính:**
1. `/Admin/UserRoleManagement` → song song `GET /api/user?...` + `GET /api/user/roles`.
2. Tạo: modal UserName + Email + Password + RoleName → `POST /api/user`.
3. Đổi role: `PUT /api/user/{id}/role` với `{RoleName}`.
4. Toggle: `PATCH /api/user/{id}/toggle-active`.

**Ngoại lệ:**
- Admin không thể tự toggle hoặc đổi role chính mình (API check).

---

### UC-W20 – Theo dõi Thiết bị Online real-time

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Trang real-time với 5 option cửa sổ thời gian, polling mỗi 5 giây. |

**Luồng chính:**
1. `/Admin/ActiveDevices?withinSeconds=30` — default 30, clamp [10, 300].
2. Server render model đầu tiên + `ViewBag.WithinSeconds`.
3. JS start countdown (REFRESH_INTERVAL = 5) + progress bar.
4. Mỗi 5 giây: `GET /Admin/ActiveDevicesData?withinSeconds=X` → JSON → cập nhật DOM + reset countdown.
5. Dropdown thay đổi (30/60/120/180/300s) → trigger refresh ngay.

---

### UC-W21 – Reset thiết bị từ xa

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Set cờ NeedsReset để Mobile pull và tự clear Preferences khi sync. |

**Luồng chính:**
1. Trong bảng Active Devices, nhấn nút Reset trên dòng thiết bị.
2. JS confirm + build FormData (deviceId + __RequestVerificationToken).
3. `POST /Admin/ResetDevice` (`[ValidateAntiForgeryToken]`).
4. Web `POST /api/device-preference/{deviceId}/reset` (AdminOnly).
5. API `ExecuteUpdateAsync SET NeedsReset = true`.
6. Response: `{success: true, message: "Đã gửi lệnh reset"}` → nút đổi icon check xanh.
7. Mobile pull cờ trong tối đa 3 phút → clear Preferences + về LoadingPage (UC-M14).

---

### UC-W22 – Xem Bản đồ nhiệt (Heatmap)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Phân tích mật độ GPS với filter thời gian + device, overlay stall markers. |

**Luồng chính:**
1. `/Admin/Heatmap?from=&to=&deviceId=` — default `from = now - 7 ngày`.
2. `Task.WhenAll`:
   - `GET /api/device-location-log/heatmap?from=...&to=...&deviceId=...` (AdminOnly).
   - `GET /api/geo/stalls` cho overlay.
3. Server preload JSON (camelCase) vào `rawPoints` / `rawStalls`.
4. JS:
   - Leaflet map + OSM tile.
   - Normalize weight về [0, 1] → `L.heatLayer(heatData)` với gradient blue→cyan→lime→yellow→red.
   - `map.fitBounds(bounds)` zoom vừa khít.
   - Render stall markers (circleMarker + circle radiusMeters).
5. Toggle "Ẩn/Hiện gian hàng" / "Ẩn/Hiện tên" (client-side, không API).

**Luồng thay thế:**
- Filter form `method=get` submit → reload toàn trang (không AJAX).

**Giới hạn:** API reject nếu `(to - from) > 90 ngày`.

---

### UC-W23 – Quản lý Tour

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | List tour + toggle active + delete. |

**Luồng chính:**
1. `/Tour?page=&search=` → `GET /api/tours?...&isActive=` (Admin thấy cả inactive).
2. Toggle: `POST /Tour/ToggleActive/{id}` → `PATCH /api/tours/{id}/toggle-active`.
3. Xóa: `POST /Tour/Delete/{id}` → `DELETE /api/tours/{id}` (cascade xóa stops).

**Guard:** Mọi action gọi `EnsureAdmin()` check role trong Session; non-Admin → redirect Home với ErrorMessage.

---

### UC-W24 – Thiết kế Tour (TourDesigner)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Admin |
| **Mô tả** | Leaflet map với drag-drop stops để tạo/sửa tour. Click marker thêm/bỏ stop. |

**Luồng chính:**
1. `/Tour/Designer` (tạo mới) hoặc `/Tour/Designer?id=X` (sửa).
2. Server load song song:
   - `GET /api/geo/stalls` (available stalls).
   - Nếu edit: `GET /api/tours/{id}` (tour detail).
3. JS render:
   - Markers xám: stall chưa thuộc tour (click để thêm).
   - Markers xanh: stop trong tour (click để bỏ).
   - Polyline nối các stop theo order.
   - Sidebar danh sách stops drag-drop được (SortableJS).
4. Admin thao tác thuần client-side (không gọi server).
5. Nhấn Lưu → JS build `TourSavePayload` (Name, Description, EstimatedMinutes, IsActive, Stops).
6. `POST /Tour/Save?id={optional}` JSON body + `[ValidateAntiForgeryToken]`.
7. Controller:
   - Validate `Stops.Count > 0`.
   - Nếu id null → `POST /api/tours` (Create).
   - Nếu có id → `PUT /api/tours/{id}` (Update full replace).
8. API validate bốn bước: Stops rỗng → trùng StallId → StallId tồn tại → Name unique. Re-index Order 1..N.
9. Thành công → redirect `/Tour` + SuccessMessage; thất bại → 400 với message.

**Luồng thay thế:**
- Drag-drop đổi thứ tự → client cập nhật state trong memory, chỉ flush khi Lưu.
- Hiện chưa có endpoint `Reorder` riêng trong Web (API có sẵn `POST /api/tours/{id}/stops/reorder` nhưng Web gộp vào Update full replace).
