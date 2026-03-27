// Namespace chứa các trang phụ (không khai báo trong XAML mà đăng ký qua Routing)
using Mobile.Pages;

namespace Mobile
{
    /// <summary>
    /// Code-behind của AppShell — trung tâm điều hướng của ứng dụng.
    /// Có hai loại route trong Shell:
    ///   - Absolute route (//RouteName) : khai báo bằng ShellContent trong AppShell.xaml,
    ///     đây là các trang gốc, điều hướng sẽ xóa toàn bộ back stack.
    ///   - Relative route (RouteName)   : đăng ký bằng Routing.RegisterRoute() ở đây,
    ///     đây là các trang phụ, điều hướng sẽ push lên back stack (có thể quay lại).
    /// </summary>
    public partial class AppShell : Shell
    {
        /// <summary>
        /// Khởi tạo Shell và đăng ký toàn bộ route điều hướng của ứng dụng.
        /// Tất cả route phải được đăng ký trước khi dùng Shell.GoToAsync(),
        /// nếu không sẽ bị lỗi "Route not found" lúc runtime.
        /// </summary>
        public AppShell()
        {
            // Nạp cấu trúc Shell từ AppShell.xaml (các ShellContent đã khai báo)
            InitializeComponent();

            // Đăng ký các trang phụ (relative routes) — không hiển thị trên tab bar/flyout,
            // chỉ có thể điều hướng đến bằng: await Shell.Current.GoToAsync(nameof(XxxPage))
            // nameof() trả về tên lớp dưới dạng string, tránh lỗi typo và hỗ trợ refactor

            // Trang đăng nhập — hiển thị khi người dùng chưa xác thực
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));

            // Trang quét mã QR / barcode — cho phép người dùng quét để định vị gian hàng
            Routing.RegisterRoute(nameof(ScanPage), typeof(ScanPage));

            // Trang chọn ngôn ngữ thuyết minh — hiển thị sau khi quét thành công
            Routing.RegisterRoute(nameof(LanguagePage), typeof(LanguagePage));

            // Trang chọn giọng đọc — hiển thị sau khi chọn ngôn ngữ
            Routing.RegisterRoute(nameof(VoicePage), typeof(VoicePage));

            // Trang bản đồ — hiển thị vị trí gian hàng và hành trình của người dùng
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        }
    }
}
