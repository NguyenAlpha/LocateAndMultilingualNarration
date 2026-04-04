using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Helpers;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile.ViewModels;

public class StartViewModel : INotifyPropertyChanged
{
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand LoginCommand { get; }

    public StartViewModel(IDeviceService deviceService, IDevicePreferenceApiService devicePreferenceApiService)
    {
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;

        ScanCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(ScanPage)));
        LoginCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LoginPage)));
    }

    public async Task InitializeAsync()
    {
        var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();

        // Offline: không gọi API, dùng cache local (Preferences)
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            var cachedLang = LanguageHelper.GetLanguage();
            if (!string.IsNullOrEmpty(cachedLang))
            {
                // Đã từng chọn ngôn ngữ → vào thẳng MapPage
                await Shell.Current.GoToAsync("//MapPage");
            }
            else
            {
                // Chưa thiết lập lần đầu, cần mạng
                await Application.Current!.MainPage!.DisplayAlert(
                    "Không có mạng",
                    "Vui lòng kết nối mạng để thiết lập lần đầu.",
                    "OK");
            }
            return;
        }

        var preference = await _devicePreferenceApiService.GetAsync(deviceId);

        if (preference is null)
        {
            // Lần đầu: chưa có preference → chọn ngôn ngữ
            await Shell.Current.GoToAsync(nameof(LanguagePage));
        }
        else
        {
            // Đã có preference → restore ngôn ngữ và vào thẳng MapPage
            LanguageHelper.SetLanguage(preference.LanguageCode);
            if (preference.Voice is not null)
                LanguageHelper.SetVoice(preference.Voice);
            await Shell.Current.GoToAsync("//MapPage");
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
