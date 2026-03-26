using Mobile.Helpers;
using Mobile.Pages;
using Mobile.Services;
using Shared.DTOs.DevicePreferences;
using Shared.DTOs.Languages;

namespace Mobile;

public partial class LanguagePage : ContentPage
{
    private readonly LanguageApiService _languageApiService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;

    public LanguagePage(LanguageApiService languageApiService, IDeviceService deviceService, IDevicePreferenceApiService devicePreferenceApiService)
    {
        InitializeComponent();
        _languageApiService = languageApiService;
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
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

        var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
        var deviceInfo = _deviceService.GetDeviceInfo();

        // Fire-and-forget: lỗi API không block user
        _ = _devicePreferenceApiService.UpsertAsync(new DevicePreferenceUpsertDto
        {
            DeviceId     = deviceId,
            LanguageCode = item.Code,
            AutoPlay     = true,
            Platform     = deviceInfo.Platform,
            DeviceModel  = deviceInfo.DeviceModel,
            Manufacturer = deviceInfo.Manufacturer,
            OsVersion    = deviceInfo.OsVersion
        });

        LanguageHelper.SetLanguage(item.Code);
        await Shell.Current.GoToAsync($"//{nameof(MapPage)}");
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
