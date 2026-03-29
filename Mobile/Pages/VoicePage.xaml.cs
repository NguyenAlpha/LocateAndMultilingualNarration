using Mobile.Helpers;
using Mobile.Services;
using Shared.DTOs.DevicePreferences;
using Shared.DTOs.TtsVoiceProfiles;

namespace Mobile.Pages;

[QueryProperty(nameof(LanguageId), "languageId")]
[QueryProperty(nameof(LanguageCode), "languageCode")]
public partial class VoicePage : ContentPage
{
    private readonly IVoiceService _voiceService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;

    public string LanguageId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;

    public VoicePage(IVoiceService voiceService, IDeviceService deviceService, IDevicePreferenceApiService devicePreferenceApiService)
    {
        InitializeComponent();
        _voiceService = voiceService;
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadVoicesAsync();
    }

    private async Task LoadVoicesAsync()
    {
        if (!Guid.TryParse(LanguageId, out var languageGuid))
        {
            LoadingIndicator.IsVisible = false;
            EmptyLabel.IsVisible = true;
            return;
        }

        try
        {
            var voices = await _voiceService.GetVoicesByLanguageAsync(languageGuid);

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (voices.Count == 0)
            {
                EmptyLabel.IsVisible = true;
                return;
            }

            VoiceList.ItemsSource = voices;
            VoiceList.IsVisible = true;
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlertAsync("Lỗi", ex.Message, "OK");
        }
    }

    async void OnVoiceTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not TtsVoiceProfileListItemDto voice) return;

        var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
        var deviceInfo = _deviceService.GetDeviceInfo();

        // Fire-and-forget: lưu voice vào DevicePreference, không block user
        _ = _devicePreferenceApiService.UpsertAsync(new DevicePreferenceUpsertDto
        {
            DeviceId     = deviceId,
            LanguageCode = LanguageCode,
            Voice        = voice.Id.ToString(),
            AutoPlay     = true,
            Platform     = deviceInfo.Platform,
            DeviceModel  = deviceInfo.DeviceModel,
            Manufacturer = deviceInfo.Manufacturer,
            OsVersion    = deviceInfo.OsVersion
        });

        LanguageHelper.SetLanguage(LanguageCode);
        await Shell.Current.GoToAsync($"//{nameof(MapPage)}");
    }
}
