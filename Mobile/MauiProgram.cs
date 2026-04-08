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
        // Đăng ký HttpClient mặc định với timeout 10 giây — tránh treo UI khi server chậm hoặc không phản hồi.
        // OLD CODE (kept for reference): ServiceCollectionServiceExtensions.AddHttpClient(builder.Services, string.Empty, client => ...)
        builder.Services.AddHttpClient(string.Empty, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        // OLD CODE (kept for reference): builder.Services.AddTransient<ProfileViewModel>();
        // ProfileViewModel đã được đăng ký ở section VIEWMODELS bên dưới để tránh đăng ký trùng.

        // ---- SERVICES (Singleton — tạo 1 lần, dùng xuyên suốt app) ----
        // Singleton phù hợp cho service cần giữ state lâu dài hoặc dùng chung toàn app

        // Quản lý phiên đăng nhập (token, user info...)
        ServiceCollectionServiceExtensions.AddSingleton<SessionService>(builder.Services);

        // Xác thực người dùng — đăng nhập, đăng xuất
        ServiceCollectionServiceExtensions.AddSingleton<IAuthService, AuthService>(builder.Services);

        // Lấy danh sách gian hàng từ API — có cache, dùng chung cho MapPage và ScanPage
        ServiceCollectionServiceExtensions.AddSingleton<IStallService, StallService>(builder.Services);

        // Lấy danh sách ngôn ngữ thuyết minh từ API
        ServiceCollectionServiceExtensions.AddSingleton<ILanguageService, LanguageService>(builder.Services);

        // Lấy danh sách giọng đọc TTS theo ngôn ngữ từ API
        ServiceCollectionServiceExtensions.AddSingleton<IVoiceService, VoiceService>(builder.Services);

        // AudioManager.Current là singleton do plugin cung cấp — bao bọc lại để inject qua DI
        ServiceCollectionServiceExtensions.AddSingleton<IAudioManager>(builder.Services, _ => AudioManager.Current);

        // Điều khiển phát/tạm dừng/dừng audio thuyết minh gian hàng
        ServiceCollectionServiceExtensions.AddSingleton<IAudioGuideService, AudioGuideService>(builder.Services);

        // Lấy hoặc tạo DeviceId duy nhất cho thiết bị (dùng để nhận dạng visitor)
        ServiceCollectionServiceExtensions.AddSingleton<IDeviceService, DeviceService>(builder.Services);

        // Lưu/lấy ngôn ngữ ưa thích của thiết bị từ API backend
        ServiceCollectionServiceExtensions.AddSingleton<IDevicePreferenceApiService, DevicePreferenceApiService>(builder.Services);

        // SQLite local database — cache stall data để hỗ trợ offline
        ServiceCollectionServiceExtensions.AddSingleton<ILocalStallRepository, LocalStallRepository>(builder.Services);

        // Download và quản lý cache file audio local theo ngôn ngữ
        ServiceCollectionServiceExtensions.AddSingleton<IAudioCacheService, AudioCacheService>(builder.Services);

        // Điều phối sync: API → SQLite → download audio
        ServiceCollectionServiceExtensions.AddSingleton<ISyncService, SyncService>(builder.Services);

        // Background service: timer 3 phút + connectivity trigger
        ServiceCollectionServiceExtensions.AddSingleton<ISyncBackgroundService, SyncBackgroundService>(builder.Services);

        // ---- VIEWMODELS (Transient — tạo mới mỗi khi được resolve) ----
        // Transient phù hợp cho ViewModel vì mỗi Page nên có instance ViewModel riêng,
        // tránh state cũ của trang trước bị giữ lại khi điều hướng

        // OLD CODE (kept for reference): dùng builder.Services.AddTransient<T>() trực tiếp gây ambiguous extension trong một số context.
        ServiceCollectionServiceExtensions.AddTransient<StartViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<LoginViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<MainViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<MapViewModel>(builder.Services);
        // OLD CODE (kept for reference): ServiceCollectionServiceExtensions.AddTransient<LanguageViewModel>(builder.Services);
        // Tạm comment do class chưa tồn tại/đang chưa sẵn sàng trong workspace hiện tại.
        // ServiceCollectionServiceExtensions.AddTransient<LanguageViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ScanViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ProfileViewModel>(builder.Services);

        // ---- PAGES (Transient — chỉ đăng ký page nào cần inject service vào constructor) ----
        // Các page không cần DI thì KHÔNG cần đăng ký ở đây — MAUI tự tạo khi điều hướng
        ServiceCollectionServiceExtensions.AddTransient<MainPage>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<MapPage>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<LanguagePage>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<VoicePage>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<StartPage>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<StallPopup>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ScanPage>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ProfilePage>(builder.Services);

        // OLD CODE (kept for reference): service profile visitor chưa tồn tại trong solution hiện tại.
        // ServiceCollectionServiceExtensions.AddTransient<IVisitorProfileService, VisitorProfileService>(builder.Services);

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