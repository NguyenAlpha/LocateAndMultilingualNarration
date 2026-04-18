# Developer Notes API

Tài liệu giải thích luồng hoạt động và các quyết định kỹ thuật của tầng API.

---

## Mục lục

- [SD-A01: Đăng nhập & Phát hành Token](#sd-a01-đăng-nhập--phát-hành-token)
- [SD-A02: Refresh Token](#sd-a02-refresh-token)
- [SD-A03: TTS Background Service](#sd-a03-tts-background-service)
- [SD-A04: Xác thực Mã QR (Mobile)](#sd-a04-xác-thực-mã-qr-mobile)
- [SD-A05: Thanh toán & Kích hoạt Plan](#sd-a05-thanh-toán--kích-hoạt-plan)
- [SD-A06: Lấy danh sách Gian hàng (Geo)](#sd-a06-lấy-danh-sách-gian-hàng-geo)
- [SD-A07: Heartbeat Thiết bị & Lấy Thiết bị Đang Hoạt Động](#sd-a07-heartbeat-thiết-bị--lấy-thiết-bị-đang-hoạt-động)

---

## SD-A01: Đăng nhập & Phát hành Token

Khi nhận request đăng nhập, **AuthController** tìm user theo email hoặc username — cả hai đều được chấp nhận vì API so sánh với cả `NormalizedEmail` lẫn `NormalizedUserName`. Sau đó xác minh mật khẩu bằng BCrypt, kiểm tra `IsActive`, rồi phát hành hai token:

**JWT (30 phút):** Được sinh bởi `JwtService.GenerateToken`, chứa `UserId`, `UserName`, `Roles` dưới dạng claims. Token này stateless — API không lưu trữ, chỉ xác minh chữ ký khi nhận request.

**Refresh Token (30 ngày):** `JwtService.GenerateRefreshToken` sinh ra một cặp — raw token (gửi cho client) và SHA256 hash (lưu vào database). Client giữ raw token, database chỉ lưu hash để không thể khôi phục token gốc nếu DB bị lộ. Cùng với token, API lưu thêm `DeviceId` và IP để phục vụ audit sau này.

---

## SD-A02: Refresh Token

Khi JWT hết hạn, client gửi raw refresh token lên `POST /api/auth/refresh`. API hash token đó bằng SHA256 rồi tra cứu trong database — nếu không tìm thấy hash tương ứng thì từ chối ngay. Tiếp theo kiểm tra token chưa bị thu hồi (`RevokedAtUtc` là null) và chưa hết hạn.

Nếu hợp lệ, API thu hồi token cũ (set `RevokedAtUtc = now`), tạo cặp JWT + refresh token mới, lưu token mới vào DB và trả về. Client nhận được cặp token hoàn toàn mới — refresh token cũ không dùng được nữa.

---

## SD-A03: TTS Background Service

Đây là luồng phức tạp nhất trong project. Lý do tồn tại: Azure TTS + dịch thuật + upload Blob có thể mất 10–100 giây mỗi lần — nếu gọi đồng bộ trong HTTP request thì client sẽ timeout. Giải pháp là tách thành hai bước:

**Bước 1 — Đặt job:** Khi người dùng tạo hoặc cập nhật nội dung thuyết minh, controller chỉ lưu record với `TtsStatus = "Pending"` rồi trả về ngay lập tức.

**Bước 2 — Xử lý nền:** `TtsBackgroundService` chạy liên tục trong vòng đời API, poll database mỗi 5 giây bằng `PeriodicTimer`. Mỗi tick làm hai việc:

- **Reset stale jobs:** Tìm các job đang ở trạng thái `"Processing"` mà `UpdatedAt` đã quá 10 phút — dấu hiệu API đã crash giữa chừng. Reset chúng về `"Pending"` để xử lý lại.

- **Claim và xử lý:** Lấy tối đa 5 job `"Pending"` cũ nhất, set ngay thành `"Processing"` và commit DB *trước* khi gọi Azure. Mục đích: nếu có nhiều instance API chạy song song, mỗi job chỉ được một instance nhận. Sau đó gọi `NarrationAudioService` tuần tự từng job — không song song để tránh bão request đến Azure. Service dịch văn bản, tổng hợp giọng nói, upload Blob, cập nhật `NarrationAudio`. Kết quả: `"Completed"` hoặc `"Failed"` kèm thông báo lỗi.

---

## SD-A04: Xác thực Mã QR (Mobile)

Endpoint `POST /api/qrcodes/verify` được đánh dấu `[AllowAnonymous]` — Mobile gọi trước khi có bất kỳ tài khoản nào.

API tìm mã theo `Code`, kiểm tra chưa bị dùng (`IsUsed = false`), rồi đánh dấu đã dùng ngay lập tức (`IsUsed = true`, `UsedAt = now`, `UsedByDeviceId`). Sau đó tính ngày hết hạn: `expiryAt = UsedAt + ValidDays`. Kết quả trả về `{ isValid: true, expiryAt }`.

Mỗi mã chỉ dùng được một lần — lần quét thứ hai trả về `isValid: false`. Mobile nhận `expiryAt` và lưu vào `Preferences`. Mỗi lần mở app, `LoadingPage` kiểm tra `expiryAt > now` để quyết định có cho vào không.

---

## SD-A05: Thanh toán & Kích hoạt Plan

**Kiểm tra downgrade:** Nếu business đang có plan active (chưa hết hạn) và rank của plan mới thấp hơn plan hiện tại thì bị chặn. Rank: Free=0, Basic=1, Pro=2. Không thể từ Pro xuống Basic khi Pro còn hạn.

**Mock payment:** Bỏ tất cả dấu cách và gạch nối khỏi số thẻ. Nếu còn đúng 16 chữ số thì `Completed`, ngược lại `Failed`. Không có gateway thật.

**Extend plan:** Nếu business đang có plan active thì `planStartAt = PlanExpiresAt hiện tại` (gia hạn từ ngày kết thúc cũ, không bị mất thời gian còn lại). Nếu không có plan active thì `planStartAt = now`. `planEndAt = planStartAt + 1 tháng`.

Chỉ khi thanh toán `Completed` thì `business.Plan` và `business.PlanExpiresAt` mới được cập nhật. Đơn `Failed` vẫn được lưu để Admin xem lịch sử.

---

## SD-A06: Lấy danh sách Gian hàng (Geo)

Endpoint `GET /api/geo/stalls` cũng `[AllowAnonymous]` — Mobile gọi không cần JWT.

**Resolve ngôn ngữ:** `GeoService` tra cứu `DevicePreference` theo `deviceId` để biết ngôn ngữ và giọng đọc thiết bị đã chọn. Nếu thiết bị chưa có preference hoặc không truyền `deviceId`, fallback về tiếng Việt (`code = "vi"`). Nếu không có tiếng Việt trong DB thì lấy ngôn ngữ active đầu tiên.

**Query với Filtered Include:** EF Core 5+ cho phép lọc ngay trong `.Include()` — chỉ kéo về `StallNarrationContents` đúng ngôn ngữ và đang active, không lấy toàn bộ rồi lọc phía ứng dụng.

**Chọn audio:** Với mỗi stall, `PickAudioUrl` chọn audio theo thứ tự ưu tiên: khớp `VoiceId` của thiết bị → audio do TTS sinh (`IsTts = true`) → bất kỳ audio có URL. Mỗi stall chỉ trả về một `AudioUrl` duy nhất phù hợp nhất với thiết bị đó.

Mobile nhận danh sách, upsert vào SQLite local để dùng offline, rồi hiển thị marker trên bản đồ.

---

## SD-A07: Heartbeat Thiết bị & Lấy Thiết bị Đang Hoạt Động

Tính năng này gồm hai phần ghép lại — một phần chạy ngầm mỗi khi Mobile gọi API (heartbeat), một phần là endpoint Admin dùng để thống kê.

**Heartbeat (Phần A):** `GeoController.GetAllStalls` được Mobile gọi định kỳ mỗi 3 phút qua `SyncBackgroundService`. Thay vì tạo endpoint ping riêng, heartbeat được tích hợp ngay vào luồng này: sau khi `GeoService` trả về kết quả, controller gọi `ExecuteUpdateAsync` để set `LastSeenAt = now` cho `DevicePreference` có `DeviceId` tương ứng. Đây là một câu `UPDATE` trực tiếp không load entity — nếu thiết bị chưa có bản ghi `DevicePreference` thì lệnh update không làm gì (không tạo mới). Overhead gần như không đáng kể so với query stalls đã có.

**Tại sao dùng `LastSeenAt` trong `DevicePreference` thay vì bảng riêng?** `DevicePreference` đã tồn tại với trường `LastSeenAt` có nghĩa là "lần cuối thiết bị này liên hệ hệ thống". Tái sử dụng trường này tránh tạo bảng mới chỉ để lưu timestamp — đơn giản, không tốn thêm storage, không cần migration.

**Lấy thiết bị active (Phần B):** Endpoint `GET /api/geo/active-devices?withinMinutes=N` chỉ dành cho Admin (`[Authorize(Policy = AdminOnly)]`). Controller tính `threshold = now − N phút` rồi query `DevicePreferences` theo `LastSeenAt >= threshold`. Kết quả trả về `ActiveDevicesSummaryDto` gồm `ActiveCount`, `WithinMinutes`, `AsOf` (thời điểm truy vấn), và mảng `Devices` với thông tin cơ bản của từng thiết bị. Tham số `withinMinutes` được clamp trong `[1, 60]` phía API.

**Cửa sổ thời gian nên chọn bao nhiêu?** Vì `SyncBackgroundService` chạy mỗi 3 phút, cửa sổ mặc định 5 phút đủ để bắt thiết bị đang online ngay cả khi một chu kỳ sync bị trễ nhẹ. Cửa sổ ngắn hơn (1–2 phút) chỉ thích hợp để xem thiết bị "vừa mới" hoạt động.
