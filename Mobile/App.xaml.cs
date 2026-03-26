// Thư viện hỗ trợ Dependency Injection của Microsoft Extensions
using Microsoft.Extensions.DependencyInjection;

namespace Mobile
{
    /// <summary>
    /// Lớp gốc của ứng dụng MAUI, kế thừa từ <see cref="Application"/>.
    /// Chịu trách nhiệm khởi tạo tài nguyên toàn cục (defined trong App.xaml)
    /// và tạo cửa sổ chính khi ứng dụng được khởi động.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Constructor khởi tạo ứng dụng.
        /// <see cref="InitializeComponent"/> được tạo tự động bởi XAML compiler,
        /// có nhiệm vụ nạp toàn bộ tài nguyên khai báo trong App.xaml
        /// (ResourceDictionary, styles, màu sắc, v.v.) vào bộ nhớ.
        /// </summary>
        public App()
        {
            InitializeComponent();
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
            // Tạo cửa sổ chính với AppShell là root page
            // AppShell quản lý toàn bộ luồng điều hướng (tab bar, flyout menu, routes...)
            return new Window(new AppShell());
        }
    }
}