======================================================================
LOCATEANDMULTILINGUALNARRATION - README
Hệ thống Bản đồ và Thuyết minh Đa ngôn ngữ cho Du lịch
======================================================================

1) GIỚI THIỆU DỰ ÁN
----------------------------------------------------------------------
Tên dự án: LocateAndMultilingualNarration

Mục tiêu:
- Hỗ trợ du khách định vị gian hàng/điểm tham quan trên bản đồ tương tác.
- Cung cấp thuyết minh đa ngôn ngữ theo từng gian hàng.
- Tối ưu trải nghiệm bằng QR flow + cá nhân hóa cấu hình thiết bị.

Đối tượng sử dụng:
- Du khách tham quan sự kiện, hội chợ, triển lãm.
- Đơn vị tổ chức cần hỗ trợ đa ngôn ngữ cho khách quốc tế.


2) CHỨC NĂNG CHÍNH
----------------------------------------------------------------------
- Bản đồ tương tác hiển thị pin gian hàng (Mapsui + OSM).
- Quét QR bằng camera để điều hướng nhanh theo ngữ cảnh (stall/token).
- Phát audio thuyết minh theo ngôn ngữ/voice đã chọn.
- Lưu cấu hình thiết bị vào DevicePreferences:
  + LanguageId
  + VoiceId
  + SpeechRate
  + AutoPlay
- Trang Profile cho phép cập nhật cấu hình ưu tiên.


3) CÔNG NGHỆ SỬ DỤNG
----------------------------------------------------------------------
Mobile (Frontend):
- .NET MAUI (.NET 10)
- Mapsui + OpenStreetMap
- ZXing.Net.Maui
- Plugin.Maui.Audio
- CommunityToolkit.Maui
- MVVM + DI

Backend (API):
- ASP.NET Core Web API
- Entity Framework Core
- Module API: Auth, Stalls, Geo, Languages, DevicePreference, Narration...

Database & Storage:
- SQL Server (server-side)
- SQLite (cache cục bộ mobile)

External/Integration:
- Azure Text-to-Speech (định hướng tích hợp mở rộng)


4) KIẾN TRÚC TỔNG QUAN
----------------------------------------------------------------------
Luồng dữ liệu mức cao:
MAUI App <-> ASP.NET Core API <-> SQL Server
MAUI App <-> SQLite local cache (offline/cache-first)

Nguyên tắc triển khai:
- Cache-first cho dữ liệu stall khi có local data.
- Ưu tiên fallback an toàn khi offline hoặc API lỗi.
- DI + Service layer tách biệt UI (MVVM).


5) CẤU TRÚC THƯ MỤC
----------------------------------------------------------------------
Mobile/:
- Pages/      : UI pages
- ViewModels/ : ViewModel logic
- Services/   : API/audio/sync/session/device
- Models/     : DTO/model cho mobile
- LocalDb/    : SQLite cache
- Helpers/    : Utilities
- AppShell.*  : Routing/navigation
- MauiProgram.cs : DI, HttpClient, plugin registration
- thêm migration bắc buộc
Api/:
- Controllers/
- Domain/Entities/
- Infrastructure/
- Migrations/
- Program.cs

Shared/:
- DTOs dùng chung giữa Mobile và API


6) HƯỚNG DẪN CÀI ĐẶT VÀ CHẠY
----------------------------------------------------------------------
Yêu cầu:
- Visual Studio 2026 (hoặc phiên bản mới tương đương)
- .NET SDK theo target framework hiện tại
- MAUI workload
- Android SDK + Emulator hoặc thiết bị thật
- SQL Server

Các bước:
1. Mở solution tại thư mục gốc.
2. Restore NuGet packages.
3. Cấu hình connection string cho project Api.
4. Chạy migration/update database (nếu cần).
5. Run Api trước.
6. Run Mobile sau.

Lưu ý BaseUrl cho Mobile:
- Android Emulator: http://10.0.2.2:<api-port>
- Thiết bị thật: dùng IP/domain thực tế.


7) LUỒNG SỬ DỤNG CHÍNH
----------------------------------------------------------------------
Luồng A - Map:
1. Mở app.
2. Vào MapPage.
3. Chạm pin gian hàng để xem thông tin/phát thuyết minh.

Luồng B - QR:
1. Vào ScanPage.
2. Quét QR.
3. Điều hướng theo context hiện tại (language/voice/map).

Luồng C - Language/Voice:
1. Chọn ngôn ngữ.
2. Chọn giọng đọc.
3. Lưu DevicePreference qua API.

Luồng D - Profile:
1. Vào ProfilePage.
2. Cập nhật cấu hình.
3. Nhấn Lưu để upsert theo DeviceId.


8) TRẠNG THÁI CHỨC NĂNG
----------------------------------------------------------------------
Đã hoàn thành:
- Map hiển thị pin gian hàng.
- QR flow cơ bản.
- Audio play/pause/stop.
- Load ngôn ngữ động từ API.
- Upsert DevicePreference.
- Kiến trúc MVVM + DI cho module chính.

Đang hoàn thiện:
- Xử lý tập trung 401 (token hết hạn).
- Quản lý lifecycle audio khi app chuyển trạng thái.
- Chuẩn hóa cấu hình BaseUrl toàn app.


9) TEST CASES KHUYẾN NGHỊ
----------------------------------------------------------------------
Nhóm test quan trọng:
- QR flow:
  + QR hợp lệ (stallId/token) điều hướng đúng.
  + QR sai format không crash.
  + Chống quét lặp gây navigate trùng.

- DevicePreference + session:
  + Upsert đúng theo DeviceId (không tạo bản ghi trùng).
  + Đọc lại preference sau khi mở lại app.
  + Session 24h: hết hạn thì clear session + điều hướng hợp lệ.

- Map + Offline:
  + Online: load pin đúng.
  + Offline có cache: fallback SQLite.
  + Offline không cache: empty state rõ ràng.

- Audio lifecycle:
  + Stop audio khi rời trang/đóng app.
  + Không phát chồng khi mở narration mới.

- Permission handling:
  + Camera/Location denied hoặc denied vĩnh viễn.
  + Có hướng dẫn mở cài đặt hệ thống.

- 401 handling:
  + API trả 401 tại các màn hình chính phải xử lý nhất quán.


10) TODO / ROADMAP
----------------------------------------------------------------------
P1 (ưu tiên cao):
1. 401 Global Handler cho toàn bộ API calls.
2. Ổn định audio lifecycle (foreground/background/navigation).
3. Chuẩn hóa BaseUrl từ cấu hình tập trung.

P2 (ưu tiên trung bình):
4. Hoàn thiện UX khi thiếu quyền (Camera/Location + Settings shortcut).
5. Chính sách sync offline->online rõ ràng.

P3 (mở rộng):
6. Web Admin (Razor Pages/MVC) cho CRUD + Generate QR.
7. Tinh chỉnh zoom map theo DPI thiết bị thật.


11) LƯU Ý QUAN TRỌNG
----------------------------------------------------------------------
- Cần chạy API trước khi mở Mobile để tránh lỗi dữ liệu ban đầu.
- BaseUrl khác nhau giữa emulator và thiết bị thật.
- Trải nghiệm Mapsui có thể khác nhau theo DPI và hiệu năng thiết bị.
- Token xác thực có thể hết hạn trong quá trình sử dụng dài.

Các tình huống có thể diễn ra trong vận hành thực tế:
- Người dùng quét QR thành công nhưng để quá 24h mới quay lại app: cần xử lý hết hạn phiên QR.
- Người dùng từ chối quyền Camera/Location: phải có luồng UX thay thế rõ ràng.
- Người dùng đang offline: app cần fallback cache hoặc hiển thị empty state thân thiện.
- Audio có thể lỗi do URL/file: cần fail-safe, không làm treo app.
- API có thể timeout/401 trong giờ cao điểm: cần handler đồng nhất toàn app.




12) VERSION
----------------------------------------------------------------------
- Phiên bản: v1.0
- Trạng thái: Core features completed, continuing stabilization
- Ngày cập nhật README: .....................................

======================================================================
GHI CHÚ
======================================================================
Tài liệu này dùng định dạng plain text (README.txt) để dễ đọc, dễ nộp
đồ án và thuận tiện review trên nhiều môi trường khác nhau.
