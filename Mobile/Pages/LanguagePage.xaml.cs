using Mobile.ViewModels;

namespace Mobile;

//[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class LanguagePage : ContentPage
{
    private readonly LanguageViewModel _viewModel;

    public string? StallId { get; set; }
    public string? Token { get; set; }

    /// <summary>
    /// Khởi tạo trang và gán ViewModel qua DI.
    /// </summary>
    /// <param name="viewModel">ViewModel xử lý logic chọn ngôn ngữ/giọng đọc.</param>
    public LanguagePage(LanguageViewModel viewModel)
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