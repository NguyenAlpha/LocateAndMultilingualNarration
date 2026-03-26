using Mobile.Helpers;
using Mobile.Services;
using Shared.DTOs.Languages;

namespace Mobile;

public partial class LanguagePage : ContentPage
{
    private readonly LanguageApiService _languageApiService;

    public LanguagePage(LanguageApiService languageApiService)
    {
        InitializeComponent();
        _languageApiService = languageApiService;
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
            var languages = await _languageApiService.GetActiveLanguagesAsync();

            var items = languages.Select(l => new LanguageItem
            {
                Code = l.Code,
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
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    async void OnLanguageTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not LanguageItem item) return;

        LanguageHelper.SetLanguage(item.Code);
        await DisplayAlert("Ngôn ngữ", $"Đã chọn: {item.DisplayLabel}", "OK");
        Application.Current!.MainPage = new AppShell();
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
        public string FlagEmoji { get; set; } = null!;
        public string DisplayLabel { get; set; } = null!;
    }
}
