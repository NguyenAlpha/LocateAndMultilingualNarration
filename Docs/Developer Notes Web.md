# Developer Notes

Tài liệu giải thích luồng hoạt động và các quyết định kỹ thuật trong project.

---

## Mục lục

- [SD-W01: Đăng nhập](#sd-w01-đăng-nhập)
- [SD-W02: Đăng ký BusinessOwner](#sd-w02-đăng-ký-businessowner)
- [SD-W03: Đăng xuất](#sd-w03-đăng-xuất)
- [SD-W04: Quản lý Doanh nghiệp](#sd-w04-quản-lý-doanh-nghiệp)
- [SD-W05: Quản lý Gian hàng](#sd-w05-quản-lý-gian-hàng)
- [SD-W06: Đặt vị trí Gian hàng trên bản đồ](#sd-w06-đặt-vị-trí-gian-hàng-trên-bản-đồ)
- [SD-W07: Quản lý Media Gian hàng](#sd-w07-quản-lý-media-gian-hàng)
- [SD-W08: Quản lý Nội dung Thuyết minh](#sd-w08-quản-lý-nội-dung-thuyết-minh)
- [SD-W09: Theo dõi & Retry TTS](#sd-w09-theo-dõi--retry-tts)
- [SD-W10: Upload Audio giọng người](#sd-w10-upload-audio-giọng-người)
- [SD-W11: Xem bảng giá](#sd-w11-xem-bảng-giá)
- [SD-W12: Thanh toán đăng ký gói](#sd-w12-thanh-toán-đăng-ký-gói)
- [SD-W13: Dashboard Admin](#sd-w13-dashboard-admin)
- [SD-W14: Quản lý User & Role](#sd-w14-quản-lý-user--role)
- [SD-W15: Quản lý Mã QR](#sd-w15-quản-lý-mã-qr)
- [SD-W16: Kiosk Tạo QR Tự Động](#sd-w16-kiosk-tạo-qr-tự-động)
- [SD-W17: Admin cập nhật Subscription Business](#sd-w17-admin-cập-nhật-subscription-business)
- [SD-W18: Lịch sử Đơn đăng ký](#sd-w18-lịch-sử-đơn-đăng-ký)
- [SD-W19: Theo dõi Thiết bị Online](#sd-w19-theo-dõi-thiết-bị-online)

---

## SD-W01: Đăng nhập

Người dùng nhập thông tin đăng nhập và submit, **AuthController** nhận request đó và gọi `LoginAsync` của **ApiClient** để gửi request đến API. API tiến hành xác thực thông tin với database. Nếu thất bại, API trả về lỗi và controller hiển thị thông báo lỗi trên View. Nếu thành công, API trả về JWT token cùng thông tin người dùng, controller lưu vào **Session** rồi redirect người dùng về trang chủ.

---

## SD-W02: Đăng ký BusinessOwner

Người dùng nhập thông tin đăng ký gồm username, email, mật khẩu và số điện thoại rồi submit, **AuthController** nhận request và gọi `RegisterBusinessOwnerAsync` của **ApiClient** để gửi request đến API. API tiến hành kiểm tra thông tin với database — nếu email hoặc username đã tồn tại thì trả về lỗi, controller hiển thị thông báo lỗi trên View. Nếu thành công, API tạo tài khoản mới với role **BusinessOwner** và controller redirect người dùng về trang đăng nhập.

---

## SD-W03: Đăng xuất

Người dùng nhấn nút Đăng xuất trên sidebar, **AuthController** nhận request và gọi `ClearToken()` của **ApiClient**. Hàm này xóa toàn bộ thông tin đang lưu trong Session — bao gồm JWT token, role, tên người dùng, và thông tin plan. Sau đó controller redirect người dùng về trang đăng nhập.

Lưu ý: vì JWT là stateless nên server không thể "thu hồi" token đã cấp. Đăng xuất ở đây chỉ xóa token khỏi Session phía server — nếu ai đó giữ bản sao của token thì vẫn dùng được cho đến khi hết hạn 30 phút. Đây là đặc điểm chung của JWT, không phải lỗi của hệ thống.

---

## SD-W04: Quản lý Doanh nghiệp

Trang quản lý doanh nghiệp có bốn luồng chính dùng chung một View:

**Xem danh sách:** Khi truy cập `/Business`, **BusinessController** gọi `GetBusinessesAsync` của **BusinessApiClient** để lấy danh sách có phân trang từ API. Admin thấy toàn bộ doanh nghiệp, BusinessOwner chỉ thấy doanh nghiệp của mình — logic lọc này nằm ở phía API, không phải Web.

**Tạo mới:** Người dùng điền form trong modal rồi submit. Controller gọi `CreateBusinessAsync` với thông tin tên, mã số thuế, email và số điện thoại liên hệ. Nếu thành công, redirect về danh sách kèm thông báo. Nếu thất bại, render lại trang với modal vẫn mở và hiển thị lỗi.

**Cập nhật:** Tương tự luồng tạo mới — người dùng mở modal sửa, submit, controller gọi `UpdateBusinessAsync`. Nếu thất bại, modal sửa mở lại với lỗi tương ứng.

**Kích hoạt / Vô hiệu hóa:** Người dùng nhấn nút toggle, controller gọi `ToggleActiveAsync` để đổi trạng thái. Nếu thành công, hiển thị thông báo "đã kích hoạt" hoặc "đã vô hiệu hóa" tùy theo trạng thái mới.

---

## SD-W05: Quản lý Gian hàng

Trang quản lý gian hàng có cấu trúc tương tự SD-W04 nhưng phức tạp hơn ở một số điểm:

**Xem danh sách:** Khi tải trang, **StallController** gọi song song hai API — `GetBusinessesAsync` của **BusinessApiClient** để lấy danh sách doanh nghiệp cho dropdown lọc, và `GetStallsAsync` của **StallApiClient** để lấy danh sách gian hàng có phân trang.

**Tạo mới:** Người dùng chọn doanh nghiệp, điền tên, slug và thông tin liên hệ rồi submit. API kiểm tra hai điều trước khi tạo: slug có bị trùng không, và số gian hàng hiện tại có vượt giới hạn của plan không (Free: 1, Basic: 3, Pro: không giới hạn). Admin được bỏ qua kiểm tra giới hạn plan. Nếu thất bại vì bất kỳ lý do nào, modal tạo mở lại với lỗi tương ứng.

**Cập nhật:** Người dùng mở modal sửa và submit. Controller gọi `UpdateStallAsync` với toàn bộ thông tin gian hàng. Nếu thất bại, modal sửa mở lại với lỗi tương ứng.

**Kích hoạt / Vô hiệu hóa:** Người dùng nhấn nút toggle, controller gọi `ToggleActiveAsync` để đổi trạng thái. Nếu thành công, hiển thị thông báo "đã kích hoạt" hoặc "đã vô hiệu hóa" tùy theo trạng thái mới.

---

## SD-W06: Đặt vị trí Gian hàng trên bản đồ

Luồng này khác các luồng CRUD thông thường — thay vì modal, người dùng tương tác trực tiếp trên bản đồ OpenStreetMap để chọn tọa độ.

**Tạo vị trí:** Khi mở trang, **StallLocationController** gọi `GetStallsAsync` của **StallApiClient** để lấy danh sách gian hàng cho dropdown, và `GetLocationsAsync` của **StallLocationApiClient** (qua `BuildAllLocationsJsonAsync`) để lấy tất cả vị trí hiện có nhằm vẽ marker lên bản đồ. Người dùng click lên bản đồ để chọn tọa độ — View cập nhật marker và điền lat/lng vào form. Sau khi submit, controller gọi `CreateLocationAsync` của **StallLocationApiClient**. Nếu thành công, redirect về danh sách kèm thông báo; nếu thất bại, render lại bản đồ với lỗi.

**Chỉnh sửa vị trí:** Khi mở trang, controller gọi `GetLocationAsync` của **StallLocationApiClient** để lấy thông tin vị trí hiện tại, sau đó gọi `GetStallsAsync` của **StallApiClient** và `GetLocationsAsync` (qua `BuildAllLocationsJsonAsync`) để vẽ lại bản đồ đầy đủ. Marker của vị trí đang sửa được đặt sẵn tại tọa độ cũ. Người dùng kéo marker hoặc nhập tọa độ mới rồi submit, controller gọi `UpdateLocationAsync` của **StallLocationApiClient** — luồng xử lý kết quả tương tự tạo mới.

---

## SD-W07: Quản lý Media Gian hàng

Trang quản lý ảnh/media của gian hàng. Ảnh được lưu trên Azure Blob Storage, không lưu trực tiếp trong database.

**Xem danh sách:** **StallMediaController** gọi song song `GetStallsAsync` của **StallApiClient** (cho dropdown lọc) và `GetListAsync` của **StallMediaApiClient** để lấy danh sách media. Kết quả hiển thị dạng grid ảnh.

**Upload ảnh mới:** Người dùng chọn file ảnh, điền caption và submit. Request gửi dưới dạng `multipart/form-data`. Controller gọi `UploadCreateAsync` của **StallMediaApiClient**, API nhận file, upload lên Azure Blob rồi tạo record trong database. Nếu thất bại, modal tạo mở lại với lỗi.

**Cập nhật ảnh:** Tương tự upload mới — người dùng có thể thay ảnh hoặc chỉ sửa thông tin (caption, thứ tự, trạng thái). Controller gọi `UploadUpdateAsync`.

**Xóa:** Người dùng xác nhận xóa, controller gọi `DeleteAsync`. API xóa cả record lẫn file trên Azure Blob.

---

## SD-W08: Quản lý Nội dung Thuyết minh

Đây là module trung tâm của hệ thống — quản lý văn bản thuyết minh theo từng ngôn ngữ, gắn với gian hàng cụ thể, và kích hoạt quy trình tổng hợp giọng nói (TTS).

**Xem danh sách:** **NarrationController** gọi song song ba API — `GetStallsAsync` (dropdown lọc theo gian hàng), `GetActiveLanguagesAsync` của **LanguageApiClient** (dropdown lọc theo ngôn ngữ), và `GetContentsAsync` của **StallNarrationContentApiClient** (danh sách nội dung kèm trạng thái TTS).

**Xem chi tiết:** Controller gọi `GetContentAsync` để lấy nội dung cùng danh sách audio đã tổng hợp. Song song đó gọi thêm `GetStallAsync` và `GetActiveLanguagesAsync` để render đầy đủ thông tin trên trang `show.cshtml`.

**Tạo mới:** Sau khi submit, API tự động set `TtsStatus = Pending` nếu plan của business cho phép TTS (Basic hoặc Pro). Nếu là Free plan, API từ chối tạo. Background service sẽ nhận job này và xử lý bất đồng bộ.

**Cập nhật:** Tương tự tạo mới — API reset `TtsStatus = Pending` để tổng hợp lại audio với nội dung mới. Nếu thất bại, trang `show.cshtml` render lại với lỗi.

**Kích hoạt / Vô hiệu hóa:** Controller gọi `ToggleStatusAsync` để đổi trạng thái hiển thị của nội dung.

---

## SD-W09: Theo dõi & Retry TTS

Trang `show.cshtml` có cơ chế tự động cập nhật trạng thái TTS mà không cần người dùng tải lại trang.

**Polling:** Khi trang phát hiện `TtsStatus` là `Pending` hoặc `Processing`, JavaScript khởi động `setInterval` gọi `GET /Narration/TtsStatus/{id}` mỗi vài giây. Controller gọi `GetTtsStatusAsync` của **StallNarrationContentApiClient**, nhận về trạng thái hiện tại cùng danh sách audio. JavaScript cập nhật badge trên giao diện theo kết quả. Khi trạng thái chuyển sang `Completed` hoặc `Failed`, polling dừng lại.

**Retry:** Nếu TTS thất bại (`Failed`), người dùng nhấn nút "Thử lại TTS". Controller gọi `RetryTtsAsync`, API reset `TtsStatus = Pending` và xóa thông báo lỗi cũ. Sau khi redirect lại trang `show.cshtml`, polling tự động khởi động lại.

---

## SD-W10: Upload Audio giọng người

Ngoài TTS tự động, người dùng có thể tự upload file audio do người thật thu âm để thay thế cho giọng máy.

Người dùng chọn file `.mp3` hoặc `.wav` rồi submit. Controller gọi `UploadHumanAudioAsync` của **NarrationAudioApiClient**, API upload file lên Azure Blob và cập nhật record với `AudioUrl` mới và `AudioType = Human`. Nếu thành công, redirect về trang `show.cshtml` kèm thông báo; nếu thất bại, redirect với thông báo lỗi.

---

## SD-W11: Xem bảng giá

Trang bảng giá là trang public — không cần đăng nhập để xem.

**SubscriptionController** nhận request và kiểm tra session. Nếu người dùng đã đăng nhập, controller gọi `GetBusinessesAsync` của **BusinessApiClient** để lấy danh sách business kèm thông tin plan hiện tại — thông tin này dùng để pre-select business ở trang Checkout. Nếu chưa đăng nhập, bỏ qua bước này.

Khi người dùng nhấn nút đăng ký một gói: nếu chưa đăng nhập thì redirect về trang Login kèm `returnUrl` trỏ về Checkout; nếu đã đăng nhập thì redirect thẳng sang Checkout kèm tham số `plan` và `businessId`.

---

## SD-W12: Thanh toán đăng ký gói

**Mở trang Checkout:** Controller kiểm tra token và role trong Session. Nếu chưa đăng nhập hoặc không phải BusinessOwner/Admin thì redirect. Sau đó gọi `GetBusinessesAsync` để lấy danh sách business — nếu user chưa có business nào thì redirect về trang Plans kèm thông báo. View hiển thị alert đỏ và disable nút Submit nếu business đang chọn đang dùng plan cao hơn plan muốn đăng ký (chặn downgrade phía client).

**Xử lý thanh toán:** Người dùng nhập thông tin thẻ và submit. Controller gọi `CreateOrderAsync` của **SubscriptionOrderApiClient**. API kiểm tra số thẻ sau khi bỏ khoảng trắng — đúng 16 chữ số thì `Completed`, sai thì `Failed`. Đây là mock payment, không có gateway thật.

Nếu thanh toán thành công, controller gọi `StoreUserPlan` để cập nhật thông tin plan vào Session ngay lập tức — badge plan trên sidebar cập nhật mà không cần đăng nhập lại. Sau đó redirect sang trang Success.

---

## SD-W13: Dashboard Admin

Trang dashboard chỉ dành cho Admin. Điểm đặc biệt là **AdminController** gọi đồng thời 9 API trong một lần bằng `Task.WhenAll` — bao gồm thống kê business, stall, ngôn ngữ, user, mã QR, doanh thu và đơn đăng ký gần đây. Toàn bộ 9 request chạy song song nên thời gian tải trang bằng request chậm nhất, không phải tổng cộng. Sau khi có kết quả, controller tổng hợp các con số rồi render View.

---

## SD-W14: Quản lý User & Role

Trang chỉ Admin truy cập được. Có bốn luồng chính:

**Xem danh sách:** Controller gọi song song `GetUsersAsync` (danh sách user có phân trang, filter theo role/trạng thái/tìm kiếm) và `GetRolesAsync` (danh sách role để hiển thị tab và dropdown).

**Tạo user mới:** Admin điền form trong modal — username, email, mật khẩu, role. Controller gọi `AdminCreateUserAsync` của **UserApiClient**. Nếu thất bại (username/email trùng), redirect kèm thông báo lỗi.

**Đổi role:** Admin chọn role mới từ dropdown rồi submit. Controller gọi `UpdateUserRoleAsync`. Lưu ý: Admin không thể tự đổi role của chính mình.

**Kích hoạt / Vô hiệu hóa:** Controller gọi `ToggleUserActiveAsync`. Admin không thể tự toggle bản thân — API kiểm tra và trả về lỗi nếu cố tình.

---

## SD-W15: Quản lý Mã QR

Mã QR là vé vào app cho khách tham quan. Admin tạo mã với số ngày hiệu lực (`ValidDays`), khách quét mã một lần để kích hoạt quyền truy cập.

**Xem danh sách:** Controller gọi `GetQrCodesAsync` của **QrCodeApiClient**. Bảng hiển thị trạng thái mã: chưa quét (badge "N ngày") hoặc đã quét (hiện ngày hết hạn cụ thể).

**Tạo mã:** Admin nhập số ngày hiệu lực trong modal, controller gọi `CreateQrCodeAsync`. Mã được tạo ở trạng thái chưa dùng.

**Xem ảnh QR:** Admin nhấn nút xem, controller gọi `GetQrCodeImageAsync` — API trả về file PNG bytes, controller trả về `File(bytes, "image/png")` để hiển thị trực tiếp trong modal trình duyệt.

**Xóa:** Controller gọi `DeleteQrCodeAsync`. Chỉ xóa được mã chưa được sử dụng.

---

## SD-W16: Kiosk Tạo QR Tự Động

Luồng kiosk cho phép Admin dựng màn hình tự phục vụ — mã QR mới tự xuất hiện liên tục sau mỗi lần khách quét, không cần thao tác thủ công.

Admin nhập số ngày hiệu lực rồi nhấn Bắt đầu. JavaScript gọi `POST /Admin/StartAutoQr` để tạo mã đầu tiên, nhận về ID và lấy ảnh PNG hiển thị lên màn hình.

Sau đó JavaScript poll mỗi 2 giây bằng `GET /Admin/PollAutoQr`. Controller gọi `GetQrCodeAsync` của **QrCodeApiClient** để kiểm tra `IsUsed`. Nếu chưa bị quét thì giữ nguyên; nếu đã quét thì controller tự tạo mã mới ngay trong cùng response rồi trả về ID và ảnh mới — JavaScript thay thế QR trên màn hình mà không cần reload trang.

---

## SD-W17: Admin cập nhật Subscription Business

Luồng này cho phép Admin trực tiếp set plan và ngày hết hạn cho bất kỳ business nào — không qua quy trình thanh toán.

**Xem danh sách:** Controller gọi `GetBusinessesAsync` của **BusinessApiClient** với filter plan để hiển thị bảng business kèm badge plan và ngày hết hạn.

**Cập nhật plan:** Admin mở modal sửa, chọn plan mới và ngày hết hạn rồi submit. Controller gọi `UpdateSubscriptionAsync` của **SubscriptionApiClient**. Khác với thanh toán thông thường, luồng này không kiểm tra downgrade — Admin có toàn quyền set bất kỳ giá trị nào. Nếu thành công, redirect kèm thông báo; nếu thất bại, modal sửa mở lại với lỗi.

---

## SD-W18: Lịch sử Đơn đăng ký

Trang chỉ Admin xem được. Controller gọi `GetOrdersAsync` của **SubscriptionOrderApiClient** với các tham số lọc theo plan và trạng thái. API trả về danh sách đơn có phân trang. Controller tính thêm các chỉ số thống kê từ dữ liệu trang hiện tại — tổng doanh thu (chỉ tính đơn `Completed`), số đơn thành công, và số đơn thất bại — rồi render View với bảng lịch sử và stats cards.

---

## SD-W19: Theo dõi Thiết bị Online

Trang real-time cho Admin xem có bao nhiêu thiết bị Mobile đang sử dụng app tại thời điểm hiện tại.

**Tải trang lần đầu:** **AdminController** gọi `GetActiveDevicesAsync` của **DeviceApiClient** với tham số `withinMinutes` (mặc định 5). Service gọi `GET /api/geo/active-devices?withinMinutes=5`, API trả về `ActiveDevicesSummaryDto` gồm tổng số thiết bị active, danh sách chi tiết từng thiết bị (DeviceId, Platform, Model, LastSeenAt), và thời điểm truy vấn. Controller render View với model ban đầu này.

**Tự động làm mới (mỗi 20 giây):** JavaScript khởi động đồng thời một countdown đếm ngược và một progress bar lấp đầy dần. Sau mỗi 20 giây, JS gọi `GET /Admin/ActiveDevicesData?withinMinutes={current}` — đây là action riêng biệt trong **AdminController** trả về JSON thay vì render View. JS nhận JSON, cập nhật trực tiếp trên DOM (số đếm, bảng thiết bị, timestamp "Cập nhật lúc"), rồi reset countdown và progress bar về 0, bắt đầu chu kỳ mới.

**Thay đổi cửa sổ thời gian:** Admin có thể chọn dropdown để thay đổi cửa sổ thời gian (1, 2, 5, 10, 15, 30 phút). Khi thay đổi, JS cập nhật biến `withinMinutes` nội bộ rồi gọi `ActiveDevicesData` ngay lập tức để lấy dữ liệu mới — không cần reload trang, không cần submit form.

**Hai action trong AdminController:**
- `GET /Admin/ActiveDevices` — render View với model ban đầu (dùng khi mở trang)
- `GET /Admin/ActiveDevicesData` — trả về JSON (dùng bởi JS polling)
