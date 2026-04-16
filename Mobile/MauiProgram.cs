using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mobile.LocalDb;
using Mobile.Pages;
using Mobile.Services;
using Mobile.ViewModels;
using Plugin.Maui.Audio;
// SkiaSharp: thư viện đồ họa 2D — dùng để vẽ bản đồ và geofence
using SkiaSharp.Views.Maui.Controls.Hosting;
// ZXing.Net.Maui: thư viện đọc mã vạch và QR code
using ZXing.Net.Maui.Controls;

namespace Mobile;

/// <summary>
/// Điểm cấu hình trung tâm của ứng dụng MAUI (tương đương Program.cs / Startup.cs trong ASP.NET).
/// Toàn bộ service, ViewModel, Page và plugin đều được đăng ký vào DI container tại đây.
/// Framework gọi CreateMauiApp() tự động khi ứng dụng khởi động.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()      // Chỉ định lớp App là root của ứng dụng
            .UseMauiCommunityToolkit() // Đăng ký CommunityToolkit.Maui (Popup, Toast...)
            .UseSkiaSharp()         // Khởi tạo SkiaSharp để có thể vẽ đồ họa (dùng bởi Mapsui)
            .UseBarcodeReader()     // Đăng ký plugin ZXing để quét mã QR/barcode trên ScanPage
            .ConfigureFonts(fonts =>
            {
                // Đăng ký font chữ tùy chỉnh — alias (tham số 2) dùng trong XAML: FontFamily="OpenSansRegular"
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---- HTTPCLIENT ----
        // Đăng ký HttpClient mặc định với timeout 10 giây — tránh treo UI khi server chậm hoặc không phản hồi.
        // OLD CODE (kept for reference): ServiceCollectionServiceExtensions.AddHttpClient(builder.Services, string.Empty, client => ...)

        builder.Services.AddHttpClient(string.Empty, client =>
        {
            client.BaseAddress = new Uri("http://10.0.2.2:5299");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Cấu hình named HttpClient mà các service đang sử dụng (ApiHttp)
        builder.Services.AddHttpClient("ApiHttp", client =>
        {
            client.BaseAddress = new Uri("http://10.0.2.2:5299");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // ---- SERVICES (Singleton — tạo 1 lần, dùng xuyên suốt app) ----
        // Singleton phù hợp cho service cần giữ state lâu dài hoặc dùng chung toàn app

        // Xác thực người dùng — đăng nhập, đăng xuất
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // Lấy danh sách gian hàng từ API — có cache, dùng chung cho MapPage và ScanPage
        builder.Services.AddSingleton<IStallService, StallService>();

        // Lấy danh sách ngôn ngữ thuyết minh từ API
        builder.Services.AddSingleton<ILanguageService, LanguageService>();

        // Lấy danh sách giọng đọc TTS theo ngôn ngữ từ API
        builder.Services.AddSingleton<IVoiceService, VoiceService>();

        // AudioManager.Current là singleton do plugin cung cấp — bao bọc lại để inject qua DI
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);

        // Điều khiển phát/tạm dừng/dừng audio thuyết minh gian hàng
        builder.Services.AddSingleton<IAudioGuideService, AudioGuideService>();

        // Lấy hoặc tạo DeviceId duy nhất cho thiết bị (dùng để nhận dạng visitor)
        builder.Services.AddSingleton<IDeviceService, DeviceService>();

        // Lưu/đọc preference (ngôn ngữ, giọng đọc) cục bộ qua Preferences — không cần mạng
        builder.Services.AddSingleton<ILocalPreferenceService, LocalPreferenceService>();

        // Lưu/lấy ngôn ngữ ưa thích của thiết bị từ API backend
        builder.Services.AddSingleton<IDevicePreferenceApiService, DevicePreferenceApiService>();

        // SQLite local database — cache stall data để hỗ trợ offline
        builder.Services.AddSingleton<ILocalStallRepository, LocalStallRepository>();

        // Download và quản lý cache file audio local theo ngôn ngữ
        builder.Services.AddSingleton<IAudioCacheService, AudioCacheService>();

        // Điều phối sync: API → SQLite → download audio
        builder.Services.AddSingleton<ISyncService, SyncService>();

        // Background service: timer 3 phút + connectivity trigger
        builder.Services.AddSingleton<ISyncBackgroundService, SyncBackgroundService>();

        // Thu thập GPS theo batch, gửi lên API để phục vụ thống kê di chuyển
        builder.Services.AddSingleton<ILocationLogService, LocationLogService>();

        // Polling GPS độc lập — tách khỏi ViewModel để chuẩn bị cho background service
        builder.Services.AddSingleton<IGpsPollingService, GpsPollingService>();

        // Lưu trạng thái QR đã verify vào Preferences để skip ScanPage khi mở lại app
        builder.Services.AddSingleton<IQrService, QrService>();

        // ---- VIEWMODELS (Transient — tạo mới mỗi khi được resolve) ----
        // Transient phù hợp cho ViewModel vì mỗi Page nên có instance ViewModel riêng,
        // tránh state cũ của trang trước bị giữ lại khi điều hướng


        // OLD CODE (kept for reference): dùng builder.Services.AddTransient<T>() trực tiếp gây ambiguous extension trong một số context.
        ServiceCollectionServiceExtensions.AddTransient<MainViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<MapViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<LanguageViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ScanViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ProfileViewModel>(builder.Services);

        // ---- PAGES (Transient — chỉ đăng ký page nào cần inject service vào constructor) ----
        // Các page không cần DI thì KHÔNG cần đăng ký ở đây — MAUI tự tạo khi điều hướng
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<LanguagePage>();
        builder.Services.AddTransient<StallPopup>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        // Chỉ bật logging ra cửa sổ Debug khi build ở chế độ DEBUG
        // Giúp theo dõi log trong quá trình phát triển mà không ảnh hưởng bản Release
        builder.Logging.AddDebug();

        // Hạ xuống Debug cho MapViewModel để thấy log polling GPS mỗi tick
        builder.Logging.AddFilter("Mobile.ViewModels.MapViewModel", LogLevel.Debug);
        builder.Logging.AddFilter("Mobile.Services.LocationLogService", LogLevel.Debug);
        builder.Logging.AddFilter("Mobile.Services.GpsPollingService", LogLevel.Debug);
#endif

        // Hoàn tất cấu hình — đóng băng DI container và trả về MauiApp để framework khởi chạy
        return builder.Build();
    }
}