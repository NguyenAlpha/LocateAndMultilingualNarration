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
