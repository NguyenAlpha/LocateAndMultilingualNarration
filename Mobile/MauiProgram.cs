using CommunityToolkit.Maui;
// API của Microsoft để đăng ký và resolve service (AddSingleton, AddTransient...)
using Microsoft.Extensions.DependencyInjection;
// Hỗ trợ logging trong ứng dụng .NET
using Microsoft.Extensions.Logging;
using Mobile.LocalDb;
using Mobile.Pages;
using Mobile.Services;
using Mobile.ViewModels;
// Plugin phát audio (IAudioManager, AudioManager.Current)
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
        // Đăng ký HttpClient mặc định vào DI (dùng cho các service tự inject IHttpClientFactory)
        builder.Services.AddHttpClient();

        // ---- SERVICES (Singleton — tạo 1 lần, dùng xuyên suốt app) ----
        // Singleton phù hợp cho service cần giữ state lâu dài hoặc dùng chung toàn app

        // Quản lý phiên đăng nhập (token, user info...)
        builder.Services.AddSingleton<SessionService>();

        // Xác thực người dùng — đăng nhập, đăng xuất
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // Lấy danh sách gian hàng từ API — có cache, dùng chung cho MapPage và ScanPage
        builder.Services.AddSingleton<IStallService, StallService>();

        // Lấy danh sách ngôn ngữ thuyết minh từ API
        builder.Services.AddSingleton<ILanguageService, LanguageService>();

        // Lấy danh sách giọng đọc TTS theo ngôn ngữ từ API
        builder.Services.AddSingleton<IVoiceService, VoiceService>();

        // AudioManager.Current là singleton do plugin cung cấp — bao bọc lại để inject qua DI
        builder.Services.AddSingleton<IAudioManager>(_ => AudioManager.Current);

        // Điều khiển phát/tạm dừng/dừng audio thuyết minh gian hàng
        builder.Services.AddSingleton<IAudioGuideService, AudioGuideService>();

        // Lấy hoặc tạo DeviceId duy nhất cho thiết bị (dùng để nhận dạng visitor)
        builder.Services.AddSingleton<IDeviceService, DeviceService>();

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

        // ---- VIEWMODELS (Transient — tạo mới mỗi khi được resolve) ----
        // Transient phù hợp cho ViewModel vì mỗi Page nên có instance ViewModel riêng,
        // tránh state cũ của trang trước bị giữ lại khi điều hướng

        builder.Services.AddTransient<StartViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<LanguageViewModel>();
        builder.Services.AddTransient<ScanViewModel>();

        // ---- PAGES (Transient — chỉ đăng ký page nào cần inject service vào constructor) ----
        // Các page không cần DI thì KHÔNG cần đăng ký ở đây — MAUI tự tạo khi điều hướng
        builder.Services.AddTransient<LanguagePage>();
        builder.Services.AddTransient<VoicePage>();
        builder.Services.AddTransient<StartPage>();
        builder.Services.AddTransient<StallPopup>();
        builder.Services.AddTransient<ScanPage>();

#if DEBUG
        // Chỉ bật logging ra cửa sổ Debug khi build ở chế độ DEBUG
        // Giúp theo dõi log trong quá trình phát triển mà không ảnh hưởng bản Release
        builder.Logging.AddDebug();

        // Hạ xuống Debug cho MapViewModel để thấy log polling GPS mỗi tick
        builder.Logging.AddFilter("Mobile.ViewModels.MapViewModel", LogLevel.Debug);
#endif

        // Hoàn tất cấu hình — đóng băng DI container và trả về MauiApp để framework khởi chạy
        return builder.Build();
    }
}