using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Helpers;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile.ViewModels;

public class StartViewModel : INotifyPropertyChanged
{
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly ILogger<StartViewModel> _logger;
    private bool _isInitializing;
    private bool _hasNavigatedFromStart;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand LoginCommand { get; }

    public StartViewModel(
        IDeviceService deviceService,
        IDevicePreferenceApiService devicePreferenceApiService,
        ILogger<StartViewModel> logger)
    {
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
        _logger = logger;

        ScanCommand = new Command(async () => await Shell.Current.GoToAsync($"//{nameof(ScanPage)}"));
        LoginCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LoginPage)));
    }

    public async Task InitializeAsync()
    {
        // Tránh chạy Initialize đồng thời gây điều hướng lặp và crash khi profile dữ liệu lớn khởi tạo chậm.
        if (_isInitializing || _hasNavigatedFromStart)
            return;

        try
        {
            _isInitializing = true;

            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var preference = await _devicePreferenceApiService.GetAsync(deviceId);

            if (preference is null)
            {
                _hasNavigatedFromStart = true;
                await Shell.Current.GoToAsync(nameof(LanguagePage));
            }
            else
            {
                LanguageHelper.SetLanguage(preference.LanguageCode);
                if (!string.IsNullOrWhiteSpace(preference.Voice))
                    LanguageHelper.SetVoice(preference.Voice);

                _hasNavigatedFromStart = true;
                await Shell.Current.GoToAsync($"//{nameof(MapPage)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khởi tạo StartViewModel thất bại");
            await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể tải cấu hình ban đầu. Vui lòng thử lại.", "OK");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
