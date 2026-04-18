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
    private const string ApiHttpClientName = "ApiHttp";
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(10);

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var apiBaseUrl = ResolveApiBaseUrl();

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

        ConfigureHttpClients(builder.Services, apiBaseUrl);

        // ---- SERVICES (Singleton — tạo 1 lần, dùng xuyên suốt app) ----
        // Singleton phù hợp cho service cần giữ state lâu dài hoặc dùng chung toàn app
        builder.Services.AddSingleton<IAudioCacheService, AudioCacheService>();
        builder.Services.AddSingleton<IAudioGuideService, AudioGuideService>();
        builder.Services.AddSingleton<IDevicePreferenceApiService, DevicePreferenceApiService>();
        builder.Services.AddSingleton<IDeviceService, DeviceService>();
        builder.Services.AddSingleton<IGpsPollingService, GpsPollingService>();
        builder.Services.AddSingleton<ILanguageService, LanguageService>();
        builder.Services.AddSingleton<ILocalPreferenceService, LocalPreferenceService>();
        builder.Services.AddSingleton<ILocationLogService, LocationLogService>();
        builder.Services.AddSingleton<IQrService, QrService>();
        builder.Services.AddSingleton<IStallService, StallService>();
        builder.Services.AddSingleton<ISyncBackgroundService, SyncBackgroundService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<IVoiceService, VoiceService>();

        builder.Services.AddSingleton<ILocalStallRepository, LocalStallRepository>();

        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);

        // ---- VIEWMODELS (Transient — tạo mới mỗi khi được resolve) ----
        // Transient phù hợp cho ViewModel vì mỗi Page nên có instance ViewModel riêng,
        // tránh state cũ của trang trước bị giữ lại khi điều hướng
        ServiceCollectionServiceExtensions.AddTransient<LanguageViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<MainViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<MapViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ProfileViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<ScanViewModel>(builder.Services);
        ServiceCollectionServiceExtensions.AddTransient<StallListViewModel>(builder.Services);

        // ---- PAGES (Transient — chỉ đăng ký page nào cần inject service vào constructor) ----
        // Các page không cần DI thì KHÔNG cần đăng ký ở đây — MAUI tự tạo khi điều hướng
        builder.Services.AddTransient<LanguagePage>();
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<StallListPage>();
        builder.Services.AddTransient<StallPopup>();

        ConfigureLogging(builder.Logging);

        // Hoàn tất cấu hình — đóng băng DI container và trả về MauiApp để framework khởi chạy
        return builder.Build();
    }

    private static void ConfigureHttpClients(IServiceCollection services, string apiBaseUrl)
    {
        var baseUri = new Uri(apiBaseUrl, UriKind.Absolute);

        // Cấu hình default HttpClient để các service gọi CreateClient() không cần hard-code URL.
        services.AddHttpClient(string.Empty, client =>
        {
            client.BaseAddress = baseUri;
            client.Timeout = DefaultHttpTimeout;
        });

        // Cấu hình named HttpClient mà các service đang sử dụng (ApiHttp)
        services.AddHttpClient(ApiHttpClientName, client =>
        {
            client.BaseAddress = baseUri;
            client.Timeout = DefaultHttpTimeout;
        });
    }

    private static string ResolveApiBaseUrl()
    {
        var envValue = Environment.GetEnvironmentVariable("MOBILE_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envValue))
            return envValue.TrimEnd('/');

        var fallback = DevConfig.ApiBaseUrl.TrimEnd('/');

#if !DEBUG
        fallback = DevConfig.ProductionApiBaseUrl.TrimEnd('/');
#endif

#if !DEBUG
        if (fallback.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Release build phải cấu hình API HTTPS. Hãy set MOBILE_API_BASE_URL.");
        }
#endif

        return fallback;
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
#if DEBUG
        // Chỉ bật logging chi tiết trong DEBUG để không ảnh hưởng hiệu năng Release.
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Debug);

        // Hạ xuống Debug cho MapViewModel để thấy log polling GPS mỗi tick
        logging.AddFilter("Mobile.ViewModels.MapViewModel", LogLevel.Debug);
        logging.AddFilter("Mobile.Services.LocationLogService", LogLevel.Debug);
        logging.AddFilter("Mobile.Services.GpsPollingService", LogLevel.Debug);
#else
        // Release chỉ giữ mức cảnh báo để giảm log nhiễu và rủi ro lộ thông tin.
        logging.SetMinimumLevel(LogLevel.Warning);
#endif
    }
}