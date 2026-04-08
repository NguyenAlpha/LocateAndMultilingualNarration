======================================================================
1. TÊN ĐỒ ÁN / PROJECT TITLE
======================================================================
Tên tiếng Việt:
Hệ thống Bản đồ và Thuyết minh Đa ngôn ngữ cho Du lịch

Tên tiếng Anh:
LocateAndMultilingualNarration

---------------------------------------------------------------------
2. MÔ TẢ DỰ ÁN
---------------------------------------------------------------------
Mục tiêu:
- Xây dựng ứng dụng hỗ trợ du khách định vị gian hàng/điểm tham quan trên bản đồ tương tác.
- Cung cấp thuyết minh đa ngôn ngữ (multilingual narration) cho từng gian hàng.
- Tối ưu trải nghiệm: quét QR để nghe nhanh, chọn ngôn ngữ cá nhân, lưu cấu hình thiết bị.

Đối tượng sử dụng:
- Du khách tham quan sự kiện du lịch, hội chợ, khu triển lãm.
- Đơn vị tổ chức cần hỗ trợ khách quốc tế bằng nhiều ngôn ngữ.

Tính năng chính:
- Bản đồ tương tác hiển thị pin các gian hàng.
- Quét QR bằng camera để điều hướng nhanh tới gian hàng.
- Phát audio thuyết minh theo ngôn ngữ/voice đã chọn.
- Lưu tùy chọn thiết bị vào DevicePreferences để cá nhân hóa lần dùng sau.
- Trang hồ sơ (Profile) cho phép chọn ngôn ngữ ưu tiên.

---------------------------------------------------------------------
3. CÔNG NGHỆ SỬ DỤNG
---------------------------------------------------------------------
Mobile (Frontend):
- .NET MAUI (.NET 8/.NET 10)
- Mapsui + OpenStreetMap (hiển thị bản đồ, pin, geofence)
- ZXing.Net.Maui (quét QR code)
- Plugin.Maui.Audio (phát audio streaming/local)
- CommunityToolkit.Maui (UI helpers)
- DI + MVVM (ViewModel, Service, Command)

Backend (API):
- ASP.NET Core Web API
- Entity Framework Core
- API theo module: Auth, Stalls, Geo, Languages, DevicePreference, Narration, VisitorProfile...

Database:
- SQL Server
- Bảng DevicePreferences (tiêu biểu):
  + Id, DeviceId (unique), LanguageId, Voice
  + SpeechRate, AutoPlay
  + Platform, DeviceModel, Manufacturer, OsVersion
  + FirstSeenAt, LastSeenAt

Other:
- SQLite local cache (mobile) cho stall/audio metadata
- Azure Text-to-Speech (định hướng/tích hợp cho sinh audio đa ngôn ngữ)

---------------------------------------------------------------------
4. CẤU TRÚC THƯ MỤC DỰ ÁN
---------------------------------------------------------------------
MOBILE (Mobile/):
- Pages/           : Màn hình UI (MapPage, ScanPage, LanguagePage, VoicePage, ProfilePage...)
- ViewModels/      : Logic MVVM (MapViewModel, MainViewModel, ProfileViewModel...)
- Services/        : Gọi API, audio, đồng bộ nền, thiết bị, session...
- Models/          : Model UI và DTO tầng mobile
- Helpers/         : Tiện ích điều hướng/ngôn ngữ/service locator...
- LocalDb/         : Cache cục bộ SQLite (stall, metadata)
- MauiProgram.cs   : Đăng ký DI, plugin, HttpClient
- AppShell.xaml    : Route/navigation tổng

API (Api/):
- Controllers/     : Endpoint theo domain (Auth, Language, DevicePreference, Stalls...)
- Domain/Entities/ : Entity nghiệp vụ + quan hệ dữ liệu
- Infrastructure/  : AppDbContext, EF Configurations
- Migrations/      : Lịch sử migration CSDL
- Program.cs       : Cấu hình middleware, DI backend

SHARED (Shared/):
- DTOs/            : ApiResult, Auth DTO, Language DTO, DevicePreference DTO, Geo DTO...

---------------------------------------------------------------------
5. HƯỚNG DẪN CÀI ĐẶT VÀ CHẠY DỰ ÁN
---------------------------------------------------------------------
5.1. Yêu cầu môi trường:
- Visual Studio 2022 (khuyến nghị bản mới nhất)
- .NET SDK 8.0 và workload MAUI (và SDK theo target .NET 10 nếu dùng)
- Android SDK + Android Emulator (hoặc thiết bị Android thật)
- SQL Server (LocalDB hoặc SQL Server instance)

5.2. Mở project:
- Mở solution tại thư mục gốc dự án.
- Restore NuGet packages cho toàn solution.

5.3. Cấu hình Backend API:
- Kiểm tra connection string trong project Api.
- Chạy migration/update database (nếu cần).
- Run project Api trước (http://localhost:<port>).

5.4. Cấu hình Base URL cho Mobile:
- Hiện tại nhiều service đang dùng BaseUrl local kiểu:
  http://10.0.2.2:5299 (Android Emulator gọi localhost máy host)
- Nếu chạy thiết bị thật hoặc môi trường production:
  cập nhật BaseUrl theo IP/domain thực tế.

5.5. Chạy Mobile app:
- Chọn project Mobile làm startup (hoặc multi-start cùng Api).
- Chọn Android Emulator / thiết bị thật.
- Run app và cấp đầy đủ quyền khi được hỏi.

5.6. Permissions quan trọng:
- Location: để định vị và hiển thị vị trí trên bản đồ.
- Camera: để quét QR code.
- Network access: để gọi API và phát audio online.

---------------------------------------------------------------------
6. CÁCH SỬ DỤNG ỨNG DỤNG
---------------------------------------------------------------------
Luồng 1 - Bản đồ (Map):
1) Mở ứng dụng.
2) Vào MapPage để xem danh sách pin gian hàng.
3) Chạm pin để xem thông tin và phát thuyết minh.

Luồng 2 - Quét QR:
1) Vào ScanPage.
2) Quét QR tại gian hàng.
3) Ứng dụng điều hướng tới chọn ngôn ngữ/voice hoặc map tương ứng flow hiện tại.

Luồng 3 - Chọn ngôn ngữ/giọng đọc:
1) Chọn Language.
2) Chọn Voice.
3) App lưu cấu hình vào DevicePreferences qua API.

Luồng 4 - Profile:
1) Mở ProfilePage.
2) Chọn ngôn ngữ ưu tiên.
3) Nhấn Lưu để upsert DevicePreference cho thiết bị hiện tại.

---------------------------------------------------------------------
7. CÁC TÍNH NĂNG ĐÃ HOÀN THÀNH
---------------------------------------------------------------------
Theo trạng thái hiện tại:
- Hiển thị bản đồ full screen bằng Mapsui, có định vị người dùng.
- Tải dữ liệu gian hàng từ API và hiển thị pin trên bản đồ.
- QR flow: quét mã, lấy stall context, điều hướng nhanh.
- Audio narration: Play/Pause/Stop qua Plugin.Maui.Audio.
- Danh sách ngôn ngữ lấy động từ API.
- Lưu cấu hình thiết bị (ngôn ngữ/voice) vào DevicePreferences.
- Cấu trúc MVVM + DI được chuẩn hóa cho các module chính.

---------------------------------------------------------------------
8. CÔNG VIỆC CÒN LẠI / TODO LIST
---------------------------------------------------------------------
1) Quản lý vòng đời audio:
- Cần đảm bảo thoát trang/ẩn app thì audio dừng an toàn, tránh phát nền ngoài ý muốn.

2) Token hết hạn (401 Unauthorized):
- Cần interceptor/handler để phát hiện 401, cảnh báo người dùng và clear session.

3) UX khi thiếu permissions:
- Nếu user từ chối quyền nhiều lần, cần nút mở cài đặt hệ thống (ShowSettingsUI).

4) Chuẩn hóa BaseUrl:
- Tập trung cấu hình HttpClient.BaseAddress tại MauiProgram để tránh hard-code rải rác.

5) Web Admin module:
- Bổ sung giao diện quản trị (Razor Pages/MVC) cho CRUD + Generate QR.

6) Tinh chỉnh zoom bản đồ theo DPI thiết bị thật:
- Cần test trên nhiều model máy để có mức zoom phù hợp.

---------------------------------------------------------------------
9. LƯU Ý QUAN TRỌNG
---------------------------------------------------------------------
- Token xác thực có thể hết hạn trong quá trình dùng lâu.
- Audio background cần kiểm soát chặt khi lifecycle page/app thay đổi.
- BaseUrl local khác nhau giữa emulator và thiết bị thật.
- Trải nghiệm Mapsui có thể khác giữa emulator và máy thật (độ phân giải, density).
- Nên chạy API trước khi mở Mobile để tránh lỗi gọi dữ liệu ban đầu.

---------------------------------------------------------------------
10. TÁC GIẢ / THÔNG TIN LIÊN HỆ
---------------------------------------------------------------------
Sinh viên thực hiện:
- Họ và tên: ................................................
- MSSV: .....................................................
- Lớp: ......................................................

Giảng viên hướng dẫn:
- ...........................................................

Liên hệ:
- Email: ....................................................
- GitHub: ...................................................

---------------------------------------------------------------------
11. NGÀY HOÀN THÀNH / VERSION
---------------------------------------------------------------------
- Phiên bản: v1.0 (Capstone Draft)
- Ngày cập nhật README: .....................................
- Trạng thái: Core features completed, tiếp tục hoàn thiện TODO.

======================================================================
GHI CHÚ
======================================================================
README này được viết theo định dạng plain text (README.txt) để phục vụ
nộp đồ án tốt nghiệp/capstone và dễ đọc trên mọi môi trường.
