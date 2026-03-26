using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Mobile.Services;
// Thư viện ZXing.Net.Maui cung cấp tính năng đọc mã vạch (barcode) và mã QR
using ZXing.Net.Maui.Controls;
using Mobile.Services;
using Mobile.ViewModels;

namespace Mobile;

/// <summary>
/// Lớp khởi tạo ứng dụng MAUI. Đây là điểm đầu vào (entry point) của ứng dụng,
/// chịu trách nhiệm cấu hình và xây dựng toàn bộ ứng dụng trước khi chạy.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Phương thức tạo và cấu hình ứng dụng MAUI.
    /// Được gọi tự động bởi framework khi ứng dụng khởi động.
    /// </summary>
    /// <returns>Đối tượng MauiApp đã được cấu hình đầy đủ, sẵn sàng để chạy.</returns>
    public static MauiApp CreateMauiApp()
    {
        // Tạo builder để cấu hình ứng dụng theo mô hình builder pattern
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IStallService, StallService>();
        builder.Services.AddSingleton<ILanguageService, LanguageService>();
        builder.Services.AddSingleton<IAudioManager>(_ => AudioManager.Current);
        builder.Services.AddSingleton<IAudioGuideService, AudioGuideService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<LanguageViewModel>();

#if DEBUG
        // Chỉ bật logging ra cửa sổ Debug khi build ở chế độ DEBUG
        // Giúp theo dõi log trong quá trình phát triển mà không ảnh hưởng bản Release
        builder.Logging.AddDebug();
#endif

        // Đăng ký HttpClient trỏ tới API backend
        builder.Services.AddHttpClient<LanguageApiService>(client =>
        {
            client.BaseAddress = new Uri("http://10.0.2.2:5299/");
        });

        builder.Services.AddTransient<LanguagePage>();

        // Hoàn tất cấu hình và trả về đối tượng MauiApp để framework khởi chạy
        return builder.Build();
    }
}