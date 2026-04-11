using Mobile.ViewModels;

namespace Mobile;

[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
/// <summary>
/// Màn hình hiển thị danh sách ngôn ngữ để người dùng chọn trước khi sang màn hình chọn giọng đọc.
/// </summary>
public partial class LanguagePage : ContentPage
{
    private readonly LanguageSelectionViewModel _viewModel;

    public string? StallId { get; set; }
    public string? Token { get; set; }

    /// <summary>
    /// Khởi tạo trang và gán ViewModel qua DI.
    /// </summary>
    /// <param name="viewModel">ViewModel xử lý logic chọn ngôn ngữ/giọng đọc.</param>
    public LanguagePage(LanguageSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.SetScanContext(StallId, Token);
        await _viewModel.LoadLanguagesAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // OLD CODE (kept for reference): logic lọc và gọi API trực tiếp trong code-behind.
        // Đồng bộ SearchText về ViewModel để FilterLanguages chạy theo chuẩn MVVM.
        _viewModel.SearchText = e.NewTextValue ?? string.Empty;
    }
}