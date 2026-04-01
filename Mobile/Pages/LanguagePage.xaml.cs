using Mobile.Helpers;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile;

[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class LanguagePage : ContentPage
{
    private readonly ILanguageService _languageService;
    private bool _isNavigating;

    public string? StallId { get; set; }
    public string? Token { get; set; }

    public LanguagePage(ILanguageService languageService)
    {
        InitializeComponent();
        _languageService = languageService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLanguagesAsync();
    }

    private async Task LoadLanguagesAsync()
    {
        try
        {
            var languages = await _languageService.GetLanguagesAsync();

            var items = languages.Select(l => new LanguageItem
            {
                Code = l.Code,
                LanguageId = l.Id,
                FlagEmoji = FlagCodeToEmoji(l.FlagCode),
                DisplayLabel = l.DisplayName ?? l.Name
            }).ToList();

            LanguageList.ItemsSource = items;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LanguageList.IsVisible = true;
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsVisible = false;
            // OLD CODE (kept for reference): await DisplayAlert("Lỗi", ex.Message, "OK");
            await DisplayAlertAsync("Lỗi", ex.Message, "OK");
        }
    }

    async void OnLanguageTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not LanguageItem item) return;
        if (_isNavigating) return;

        try
        {
            _isNavigating = true;

            // OLD CODE (kept for reference):
            // await Shell.Current.GoToAsync($"{nameof(VoicePage)}?languageId={item.LanguageId}&languageCode={Uri.EscapeDataString(item.Code)}");

            // Dùng route AudioPage (alias của VoicePage) để đúng flow yêu cầu: QR -> Language -> Audio -> Map.
            var route = $"AudioPage?languageId={item.LanguageId}&languageCode={Uri.EscapeDataString(item.Code)}";

            // Truyền stallId/token từ QR để AudioPage điều hướng chính xác sang MapPage.
            if (!string.IsNullOrWhiteSpace(StallId))
                route += $"&stallId={Uri.EscapeDataString(StallId)}";
            else if (!string.IsNullOrWhiteSpace(Token))
                route += $"&token={Uri.EscapeDataString(Token)}";

            await Shell.Current.GoToAsync(route);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private static string FlagCodeToEmoji(string? flagCode)
    {
        if (string.IsNullOrWhiteSpace(flagCode) || flagCode.Length < 2)
            return "🌐";

        var code = flagCode.ToUpperInvariant();
        return char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A'))
             + char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));
    }

    private class LanguageItem
    {
        public string Code { get; set; } = null!;
        public Guid LanguageId { get; set; }
        public string FlagEmoji { get; set; } = null!;
        public string DisplayLabel { get; set; } = null!;
    }
}
