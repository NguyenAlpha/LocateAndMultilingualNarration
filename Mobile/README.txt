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
- QR flow + lưu session quét 24h (ScanService).
- Audio play/pause/stop.
- Load ngôn ngữ động từ API.
- Upsert DevicePreference theo DeviceId.
- Kiến trúc MVVM + DI cho module chính.
- Chuẩn hóa gọi API bằng named HttpClient "ApiHttp" cho các service chính.

Đang hoàn thiện:
- Xử lý tập trung 401 (token hết hạn).
- Quản lý lifecycle audio khi app chuyển trạng thái.
- Dọn đồng bộ DTO cũ/mới để tránh lỗi compile khi merge nhánh.


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
1. Hoàn tất fix compile còn lại sau merge (DTO, Guid/string, interface signatures).
2. 401 Global Handler cho toàn bộ API calls.
3. Ổn định audio lifecycle (foreground/background/navigation).

P2 (ưu tiên trung bình):
4. Hoàn thiện UX khi thiếu quyền (Camera/Location + Settings shortcut).
5. Chính sách sync offline->online rõ ràng.
6. Chuẩn hóa BaseUrl/config theo môi trường (dev/staging/prod).

P3 (mở rộng):
7. Web Admin (Razor Pages/MVC) cho CRUD + Generate QR.
8. Tinh chỉnh zoom map theo DPI thiết bị thật.


11) KNOWN ISSUES
----------------------------------------------------------------------
- Một số file sau merge có thể còn khác biệt naming giữa DTO cũ và DTO mới
  (ví dụ: LanguageCode/Voice vs LanguageId/VoiceId).
- Nếu API không chạy profile HTTP (5299), mobile emulator sẽ không gọi được dữ liệu.
- Trong môi trường dev, cần ưu tiên endpoint HTTP cho Android Emulator
  (http://10.0.2.2:<port>) để tránh lỗi chứng chỉ HTTPS cục bộ.


12) LƯU Ý QUAN TRỌNG
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




13) VERSION
----------------------------------------------------------------------
- Phiên bản: v1.0
- Trạng thái: Core features completed, continuing stabilization
- Ngày cập nhật README: .....................................


14) TỔNG HỢP CHỨC NĂNG (HIỆN TRẠNG THỰC TẾ)
----------------------------------------------------------------------
A. Nhóm chức năng Mobile (.NET MAUI):
- Xác thực và session người dùng (login + lưu trạng thái).
- Bản đồ gian hàng (MapPage + MapViewModel):
  + Tải danh sách gian hàng theo geodata từ API.
  + Hiển thị pin và chọn gian hàng để focus.
  + Polling GPS + kiểm tra geofence để trigger tự động.
- Quét QR (ScanPage + ScanService):
  + Đọc QR, parse context stall/token.
  + Chống navigate lặp và có session 24h.
- Ngôn ngữ và giọng đọc:
  + Load language active từ API.
  + Load voice active theo language.
  + Lưu cấu hình theo DevicePreference (DeviceId-based).
- Audio guide:
  + Play/Pause/Stop qua Plugin.Maui.Audio.
  + Tích hợp cache/local audio trong luồng sync.
- Offline/cache-first:
  + Stall data ưu tiên SQLite local.
  + Fallback local khi API lỗi hoặc mất mạng.

B. Nhóm chức năng API (ASP.NET Core Web API):
- Auth + token/refresh token.
- Geo API cho mobile map.
- Language API (admin CRUD + public active).
- TTS voice API (public active theo language).
- DevicePreference API (get/upsert theo DeviceId).
- Các module nghiệp vụ mở rộng:
  + Stall, StallLocation, StallGeoFence, StallMedia, StallNarrationContent.
  + VisitorProfile, VisitorPreference, VisitorLocationLog.


15) API CHÍNH MOBILE ĐANG DÙNG (TÓM TẮT NHANH)
----------------------------------------------------------------------
1. Map/Geo:
- GET api/geo/stalls?deviceId={deviceId}
  -> Trả về danh sách GeoStallDto cho MapPage.

2. Language:
- GET api/languages/active
  -> Trả về danh sách ngôn ngữ đang active (public).

3. Voice:
- GET api/tts-voice-profiles/active?languageId={guid}
  -> Trả về voice profile active theo language.

4. Device preference:
- GET api/device-preference/{deviceId}
  -> Lấy cấu hình hiện tại theo thiết bị.
- POST api/device-preference
  -> Upsert cấu hình language/voice/speechRate/autoPlay.
- POST api/device-preferences
  -> Endpoint lưu preference theo request mở rộng.

5. Stalls public:
- GET api/stalls
- GET api/stalls/{id}


16) THƯ MỤC VÀ VAI TRÒ CHI TIẾT HƠN
----------------------------------------------------------------------
Mobile/Pages
- UI thuần XAML + code-behind cho lifecycle/navigation.

Mobile/ViewModels
- Chứa toàn bộ state + command + xử lý nghiệp vụ phía UI theo MVVM.

Mobile/Services
- Gọi API qua named HttpClient "ApiHttp".
- Chứa logic cache-first, sync, session, language/voice/device/audio.

Mobile/LocalDb
- SQLite model + repository phục vụ offline.

Mobile/Models
- DTO/model phục vụ binding UI mobile.

Api/Controllers
- Expose REST endpoints cho mobile + web.

Api/Infrastructure + Domain
- EF Core DbContext, entity mapping/config, seed dữ liệu, settings.

Shared/DTOs
- Contract DTO dùng chung để giảm sai khác giữa tầng mobile và API.


17) CÔNG DỤNG CHỨC NĂNG (THEO NGHIỆP VỤ)
----------------------------------------------------------------------
- Giảm rào cản ngôn ngữ cho khách quốc tế.
- Tăng tốc điều hướng tham quan bằng QR + map.
- Tạo trải nghiệm cá nhân hóa theo thiết bị (voice/language/speech rate).
- Đảm bảo app vẫn có ích khi mạng yếu nhờ cache-first + offline fallback.
- Hỗ trợ vận hành sự kiện đông người nhờ geofence + audio tự động.


18) NHỮNG PHẦN CÒN THIẾU / CẦN HOÀN THIỆN
----------------------------------------------------------------------
Mức bắt buộc để ổn định production:
1. Global 401 handler cho toàn bộ service gọi API.
2. Chuẩn hóa triệt để DTO giữa Mobile/Shared (tránh drift tên field).
3. Ổn định audio lifecycle khi app background/foreground/navigate nhanh.
4. Hoàn thiện flow quyền Camera/Location khi người dùng từ chối vĩnh viễn.
5. Chuẩn hóa cấu hình môi trường (dev/staging/prod) cho BaseUrl + secrets.

Mức nâng cao:
6. Kịch bản sync offline -> online rõ chiến lược conflict.
7. Bổ sung telemetry tập trung (error rate, API latency, audio fail rate).
8. Bổ sung test tự động cho QR/session/device-preference/map offline.


19) CHECKLIST NỘP ĐỒ ÁN / DEMO
----------------------------------------------------------------------
- API chạy ổn định đúng port và mobile gọi được từ emulator.
- DB đã migrate + seed data cơ bản (language, voice, stalls).
- Demo đủ 4 flow: Map / QR / Language-Voice / Profile.
- Có kịch bản offline (có cache và không cache).
- Có kịch bản lỗi quyền (camera/location denied).
- Có kịch bản token hết hạn (401).


20) CẬP NHẬT PHIÊN BẢN README
----------------------------------------------------------------------
- Bản cập nhật: v1.1 (bổ sung tổng hợp chức năng + API + gap analysis)
- Ngày cập nhật: .....................................

======================================================================
GHI CHÚ
======================================================================
Tài liệu này dùng định dạng plain text (README.txt) để dễ đọc, dễ nộp
đồ án và thuận tiện review trên nhiều môi trường khác nhau.
