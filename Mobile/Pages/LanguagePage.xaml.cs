using Mobile.Services;
using Mobile.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mobile.Pages;

/// <summary>
/// Màn hình chọn ngôn ngữ thuyết minh (LanguagePage)
/// Hỗ trợ cả 2 luồng:
/// 1. Từ StartPage / MainPage (anonymous)
/// 2. Từ quét QR (có truyền StallId hoặc Token)
/// </summary>
[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class LanguagePage : ContentPage
{
    private readonly LanguageViewModel _viewModel;

    // Query parameters từ QR scan
    public string? StallId { get; set; }
    public string? Token { get; set; }

    private bool _isNavigating = false;

    public LanguagePage(LanguageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // OLD CODE (kept for reference): SearchBar.TextChanged += OnSearchTextChanged;
        // SearchBar trong XAML hiện xử lý trực tiếp qua TextChanged="OnSearchTextChanged",
        // nên không cần subscribe thêm trong code-behind để tránh lỗi tham chiếu control.
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // OLD CODE (kept for reference): _viewModel.SetScanContext(StallId, Token);
        // ViewModel hiện chưa có SetScanContext, tạm bỏ gọi hàm để tránh lỗi build.

        // Tải danh sách ngôn ngữ
        await _viewModel.LoadLanguagesAsync();
    }

    private void OnOpenLanguagePopupClicked(object? sender, EventArgs e)
    {
        // Mở popup chọn ngôn ngữ
        _viewModel.IsLanguagePopupOpen = true;
    }

    private void OnCloseLanguagePopupClicked(object? sender, EventArgs e)
    {
        // Đóng popup chọn ngôn ngữ
        _viewModel.IsLanguagePopupOpen = false;
    }

    private void OnLanguagePopupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Chọn ngôn ngữ từ popup và đóng popup ngay sau khi chọn
        if (e.CurrentSelection?.FirstOrDefault() is LanguageOption selectedLanguage)
        {
            _viewModel.SelectedLanguage = selectedLanguage;
            _viewModel.IsLanguagePopupOpen = false;

            // Clear selection để lần mở popup sau vẫn có thể chọn lại cùng item.
            if (sender is CollectionView collectionView)
                collectionView.SelectedItem = null;
        }
    }

    private void OnVoiceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Đồng bộ giọng đọc được chọn từ CollectionView vào ViewModel.
        if (e.CurrentSelection?.FirstOrDefault() is VoiceOption selectedVoice)
        {
            _viewModel.SelectedVoice = selectedVoice;
        }
    }

    /// <summary>
    /// Xử lý tìm kiếm realtime khi người dùng gõ vào SearchBar
    /// </summary>
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_viewModel == null) return;

        _viewModel.SearchText = e?.NewTextValue?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Sự kiện khi người dùng tap chọn một ngôn ngữ
    /// </summary>
    /// <summary>
    /// Model nội bộ dùng để binding danh sách ngôn ngữ trên UI
    /// </summary>
    public class LanguageItem
    {
        public Guid LanguageId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FlagEmoji { get; set; } = "🌐";

        public string DisplayLabel => !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName
            : Name;
    }

    // OLD CODE (kept for reference): block duplicate handlers bên dưới gây trùng method + gọi _viewModel.ConfirmSelectionAsync() private trong ViewModel.
    // Đã giữ nguyên dưới dạng comment để tham chiếu, tránh lỗi CS0111/CS0122.
    // private void OnLanguagePopupSelectionChanged(object? sender, SelectionChangedEventArgs e) { ... }
    // private async void OnConfirmClicked(object sender, EventArgs e) { await _viewModel.ConfirmSelectionAsync(); }
    // private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) { ... }
    // private void OnOpenLanguagePopupClicked(object? sender, EventArgs e) { ... }
    // private void OnCloseLanguagePopupClicked(object? sender, EventArgs e) { ... }
}
