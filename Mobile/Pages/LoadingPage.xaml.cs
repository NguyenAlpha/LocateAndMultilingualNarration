using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Mobile.Services;

namespace Mobile.Pages;

public partial class LoadingPage : ContentPage
{
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly ILocalPreferenceService _localPreference;
    private readonly IQrSessionService _qrSessionService;
    private readonly ILogger<LoadingPage> _logger;

    public LoadingPage(
        IDevicePreferenceApiService devicePreferenceApiService,
        ILocalPreferenceService localPreference,
        IQrSessionService qrSessionService,
        ILogger<LoadingPage> logger)
    {
        InitializeComponent();
        _devicePreferenceApiService = devicePreferenceApiService;
        _localPreference = localPreference;
        _qrSessionService = qrSessionService;
        _logger = logger;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await NavigateToStartupPageAsync();
    }

    private async Task NavigateToStartupPageAsync()
    {
        try
        {
            var deviceId = Preferences.Get("device_id", null);
            _logger.LogInformation("[LoadingPage] DeviceId={DeviceId}", deviceId ?? "null");

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogInformation("[LoadingPage] Không có DeviceId → ScanPage");
                await Shell.Current.GoToAsync("//ScanPage");
                return;
            }

            if (!_qrSessionService.IsSessionValid())
            {
                _logger.LogInformation("[LoadingPage] QR chưa verify hoặc đã hết hạn → ScanPage");
                await Shell.Current.GoToAsync("//ScanPage");
                return;
            }

            // Đọc local cache trước — không cần mạng, điều hướng ngay lập tức
            var local = _localPreference.Load();
            if (local is not null)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("[LoadingPage] Có local cache (lang={Lang}) → MainPage (không gọi API)", local.LanguageCode);
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            // Chưa có local cache → gọi API lần đầu
            _logger.LogInformation("[LoadingPage] Không có local cache → gọi API");
            var preference = await _devicePreferenceApiService.GetAsync(deviceId);
            var hasPreference = preference is not null;
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("[LoadingPage] API hasPreference={HasPreference} → {Route}",
                    hasPreference, hasPreference ? "MainPage" : "ScanPage");

            if (hasPreference)
            {
                // Lưu local để các lần sau không cần gọi API nữa
                _localPreference.Save(preference!);
            }

            await Shell.Current.GoToAsync(hasPreference ? "//MainPage" : "//ScanPage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LoadingPage] Startup navigation thất bại, fallback về ScanPage");
            await Shell.Current.GoToAsync("//ScanPage");
        }
    }
}
