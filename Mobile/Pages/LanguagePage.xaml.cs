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
        private async void OnLanguageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not LanguageItem item || _isNavigating)
                return;

            try
            {
                _isNavigating = true;

                // Điều hướng sang VoicePage (đăng ký alias là AudioPage trong AppShell)
                var navigationParameters = new Dictionary<string, object>
                {
                    { "languageId", item.LanguageId },
                    { "languageCode", item.Code }
                };

                // Truyền thêm StallId/Token nếu có từ QR scan
                if (!string.IsNullOrWhiteSpace(StallId))
                    navigationParameters["stallId"] = StallId;
                else if (!string.IsNullOrWhiteSpace(Token))
                    navigationParameters["token"] = Token;

                await Shell.Current.GoToAsync("AudioPage", navigationParameters);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể chuyển sang chọn giọng đọc: {ex.Message}", "OK");
            }
            finally
            {
                _isNavigating = false;
            }
        }

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
}