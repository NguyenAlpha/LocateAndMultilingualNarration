// Thư viện hỗ trợ Dependency Injection của Microsoft Extensions
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Mobile.Services;

namespace Mobile
{
    /// <summary>
    /// Lớp gốc của ứng dụng MAUI, kế thừa từ <see cref="Application"/>.
    /// Chịu trách nhiệm khởi tạo tài nguyên toàn cục (defined trong App.xaml)
    /// và tạo cửa sổ chính khi ứng dụng được khởi động.
    /// </summary>
    public partial class App : Application
    {
        private readonly SessionService _sessionService;
        private readonly ISyncBackgroundService _syncBackgroundService;
        private readonly IDeviceService _deviceService;
        private readonly IDevicePreferenceApiService _devicePreferenceApiService;
        private readonly ILogger<App> _logger;

        /// <summary>
        /// Constructor khởi tạo ứng dụng.
        /// <see cref="InitializeComponent"/> được tạo tự động bởi XAML compiler,
        /// có nhiệm vụ nạp toàn bộ tài nguyên khai báo trong App.xaml
        /// (ResourceDictionary, styles, màu sắc, v.v.) vào bộ nhớ.
        /// </summary>
        public App(
            SessionService sessionService,
            ISyncBackgroundService syncBackgroundService,
            IDeviceService deviceService,
            IDevicePreferenceApiService devicePreferenceApiService,
            ILogger<App> logger)
        {
            InitializeComponent();
            _sessionService = sessionService;
            _syncBackgroundService = syncBackgroundService;
            _deviceService = deviceService;
            _devicePreferenceApiService = devicePreferenceApiService;
            _logger = logger;
        }

        /// <summary>
        /// Ghi đè phương thức tạo cửa sổ chính của ứng dụng.
        /// Được MAUI framework gọi tự động khi ứng dụng khởi động lần đầu
        /// hoặc khi cần khôi phục cửa sổ (ví dụ: sau khi app bị đưa xuống nền trên desktop).
        /// </summary>
        /// <param name="activationState">
        /// Trạng thái kích hoạt từ hệ điều hành (có thể null trên một số nền tảng).
        /// Chứa thông tin như deep link URI, launch options, v.v.
        /// </param>
        /// <returns>
        /// Một <see cref="Window"/> mới bao bọc <see cref="AppShell"/> làm trang gốc.
        /// AppShell định nghĩa cấu trúc điều hướng (navigation) của toàn bộ ứng dụng.
        /// </returns>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                // Khởi động background sync sau khi app sẵn sàng
                _syncBackgroundService.Start();
            }
            catch (Exception ex)
            {
                // Bảo vệ startup để tránh crash toàn app nếu có lỗi runtime ngoài dự kiến.
                _logger.LogError(ex, "CreateWindow: khởi động SyncBackgroundService thất bại");
            }

            // Tạo cửa sổ chính với AppShell là root page
            // AppShell quản lý toàn bộ luồng điều hướng (tab bar, flyout menu, routes...)
            var window = new Window(new AppShell());

            // OLD CODE (kept for reference): _ = TrySkipScanOnStartupAsync();

            return window;
        }

        /// <summary>
        /// Startup navigation chuẩn cho flow:
        /// - Chưa có stall -> ScanPage
        /// - Có stall nhưng chưa có device preference -> LanguagePage
        /// - Đủ cấu hình -> MainPage
        /// </summary>
        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                // OLD CODE (kept for reference): điều hướng startup phụ thuộc vào stallId local.
                // var hasStall = await LocalStorageService.HasStall();
                // if (!hasStall)
                // {
                //     Console.WriteLine("[DEBUG] Startup navigate: //ScanPage");
                //     await Shell.Current.GoToAsync("//ScanPage");
                //     return;
                // }

                // Luồng mới: dựa vào device_id + DevicePreferences để quyết định onboarding.
                var deviceId = Preferences.Get("device_id", null);
                Console.WriteLine($"[DEBUG] Startup DeviceId: {deviceId}");

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    Console.WriteLine("[DEBUG] Startup API response: device_id missing");
                    Console.WriteLine("[DEBUG] Startup navigate: //ScanPage");
                    await Shell.Current.GoToAsync("//ScanPage");
                    return;
                }

                var hasPreference = await CheckDevicePreference(deviceId);

                if (!hasPreference)
                {
                    // OLD CODE (kept for reference): điều hướng LanguagePage khi chưa có preference.
                    // var stallId = await LocalStorageService.GetStallId();
                    // Console.WriteLine($"[DEBUG] Startup navigate: //LanguagePage?stallId={stallId}");
                    // await Shell.Current.GoToAsync($"//LanguagePage?stallId={Uri.EscapeDataString(stallId)}");

                    Console.WriteLine("[DEBUG] Startup navigate: //ScanPage (DevicePreferences chưa có)");
                    await Shell.Current.GoToAsync("//ScanPage");
                    return;
                }

                Console.WriteLine("[DEBUG] Startup navigate: //MainPage");
                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OnStart: điều hướng startup thất bại, fallback về ScanPage");
                await Shell.Current.GoToAsync("//ScanPage");
            }
        }

        /// <summary>
        /// Kiểm tra thiết bị đã có preference trên API hay chưa.
        /// </summary>
        private async Task<bool> CheckDevicePreference(string deviceId)
        {
            try
            {
                var preference = await _devicePreferenceApiService.GetAsync(deviceId);
                var hasPreference = preference is not null;
                Console.WriteLine($"[DEBUG] Startup API response: hasPreference={hasPreference} for DeviceId={deviceId}");
                return hasPreference;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CheckDevicePreference thất bại");
                return false;
            }
        }

        /// <summary>
        /// Tự động điều hướng sang trang ngôn ngữ khi đã có stallId local.
        /// Giữ nguyên flow hiện tại, chỉ thêm bước skip scan cho lần mở app sau.
        /// </summary>
        private async Task TrySkipScanOnStartupAsync()
        {
            try
            {
                // OLD CODE (kept for reference): var stallId = LocalStorageService.GetStallId();
                var stallId = await LocalStorageService.GetStallId();
                if (string.IsNullOrWhiteSpace(stallId))
                    return;

                // Chờ nhẹ để đảm bảo Shell đã khởi tạo xong trước khi điều hướng.
                await Task.Delay(200);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Console.WriteLine($"[DEBUG] Startup navigate to LanguagePage with StallId: {stallId}");
                    await Shell.Current.GoToAsync($"//LanguagePage?stallId={Uri.EscapeDataString(stallId)}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tự động bỏ qua ScanPage khi khởi động");
            }
        }

        protected override void OnSleep()
        {
            // Dừng timer khi app vào background để tiết kiệm pin
            _syncBackgroundService.Stop();
        }

        protected override void OnResume()
        {
            // Khởi động lại khi app quay về foreground
            _syncBackgroundService.Start();
        }
    }
}