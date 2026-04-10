using Mobile.ViewModels;

namespace Mobile.Pages
{
    /// <summary>
    /// Trang Hồ sơ cá nhân - Quản lý ngôn ngữ ưu tiên, giọng đọc, tốc độ đọc và cài đặt tự động phát
    /// Sử dụng MVVM pattern với ProfileViewModel để xử lý logic và dữ liệu.
    /// </summary>
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _viewModel;

        /// <summary>
        /// Constructor chính - Nhận ProfileViewModel từ Dependency Injection (DI)
        /// </summary>
        /// <param name="viewModel">ViewModel quản lý logic và dữ liệu của trang Profile</param>
        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;   // Bind ViewModel vào trang để sử dụng data binding
        }

        /// <summary>
        /// Sự kiện xảy ra khi trang Profile xuất hiện trên màn hình.
        /// Đây là nơi phù hợp nhất để tải dữ liệu từ API (ngôn ngữ, giọng đọc, cấu hình thiết bị).
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Tải thông tin hồ sơ người dùng và cấu hình hiện tại từ DevicePreferences
            if (_viewModel != null)
            {
                await _viewModel.LoadProfileAsync();
            }
        }

        /// <summary>
        /// Sự kiện xảy ra khi trang Profile biến mất khỏi màn hình.
        /// Có thể dùng để cleanup tài nguyên hoặc lưu tạm dữ liệu nếu cần.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // TODO: Có thể thêm logic dừng audio hoặc lưu draft nếu cần trong tương lai
            // Ví dụ: _viewModel.SaveDraftSettings();
        }

        /// <summary>
        /// (Tùy chọn) Xử lý khi người dùng nhấn nút Back trên thiết bị Android.
        /// Có thể hỏi xác nhận trước khi thoát trang.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            // Có thể thêm xác nhận trước khi quay lại
            // Ví dụ:
            // if (_viewModel.HasUnsavedChanges)
            // {
            //     // Hiển thị dialog hỏi người dùng có muốn lưu không
            // }

            return base.OnBackButtonPressed();
        }

        /// <summary>
        /// Phương thức công khai để reload dữ liệu thủ công (nếu cần gọi từ nơi khác)
        /// </summary>
        public async Task RefreshProfileAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadProfileAsync();
            }
        }

        /// <summary>
        /// Phương thức hỗ trợ hiển thị thông báo nhanh (Toast-like) từ code-behind
        /// </summary>
        private async Task ShowMessageAsync(string title, string message)
        {
            await DisplayAlert(title, message, "OK");
        }
    }
}