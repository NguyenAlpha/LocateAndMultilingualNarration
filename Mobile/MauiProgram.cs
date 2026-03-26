// Thư viện logging tích hợp của Microsoft Extensions, dùng để ghi log trong ứng dụng
using Microsoft.Extensions.Logging;
using Mobile.Services;
// Thư viện ZXing.Net.Maui cung cấp tính năng đọc mã vạch (barcode) và mã QR
using ZXing.Net.Maui.Controls;

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
            // Đăng ký lớp App làm root application — nơi định nghĩa giao diện chính (Shell/MainPage)
            .UseMauiApp<App>()
            // Kích hoạt tính năng đọc mã vạch & mã QR từ thư viện ZXing.Net.Maui
            // Cho phép dùng các control như <zxing:CameraView> trong XAML
            .UseBarcodeReader()
            // Đăng ký các font chữ tùy chỉnh để dùng trong toàn bộ ứng dụng
            .ConfigureFonts(fonts =>
            {
                // Đăng ký font OpenSans dạng thường, có thể tham chiếu bằng alias "OpenSansRegular"
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                // Đăng ký font OpenSans dạng đậm vừa (semibold), alias "OpenSansSemibold"
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

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