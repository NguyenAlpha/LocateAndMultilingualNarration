using Mobile.Helpers;
using Mobile.Services;
using Shared.DTOs.DevicePreferences;
using Shared.DTOs.TtsVoiceProfiles;

namespace Mobile.Pages;

[QueryProperty(nameof(LanguageId), "languageId")]
[QueryProperty(nameof(LanguageCode), "languageCode")]
[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class VoicePage : ContentPage
{
    private readonly IVoiceService _voiceService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private bool _isNavigating;

    public string LanguageId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string? StallId { get; set; }
    public string? Token { get; set; }

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
        if (_isNavigating) return;

        try
        {
            _isNavigating = true;

            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var deviceInfo = _deviceService.GetDeviceInfo();

            await _devicePreferenceApiService.UpsertAsync(new DevicePreferenceUpsertDto
            {
                DeviceId = deviceId,
                LanguageCode = LanguageCode,
                Voice = voice.Id.ToString(),
                AutoPlay = true,
                Platform = deviceInfo.Platform,
                DeviceModel = deviceInfo.DeviceModel,
                Manufacturer = deviceInfo.Manufacturer,
                OsVersion = deviceInfo.OsVersion
            });

            LanguageHelper.SetLanguage(LanguageCode);

            var route = $"//{nameof(MapPage)}";
            if (!string.IsNullOrWhiteSpace(StallId))
                route += $"?boothId={Uri.EscapeDataString(StallId)}";

            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Lỗi", $"Không thể lưu giọng đọc: {ex.Message}", "OK");
        }
        finally
        {
            _isNavigating = false;
        }
    }
}
