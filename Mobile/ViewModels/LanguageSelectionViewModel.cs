using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Helpers;
using Mobile.Pages;
using Mobile.Services;
using Shared.DTOs.DevicePreferences;

namespace Mobile.ViewModels;

public class LanguageSelectionViewModel : INotifyPropertyChanged
{
    private readonly ILanguageService _languageService;
    private readonly IVoiceService _voiceService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly ILogger<LanguageSelectionViewModel> _logger;
    private int _navigationGuard;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LanguageOption> Languages { get; } = [];
    public ObservableCollection<VoiceOption> Voices { get; } = [];

    LanguageOption? _selectedLanguage;
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage == value) return;
            _selectedLanguage = value;
            OnPropertyChanged();
            _ = LoadVoicesForSelectedLanguageAsync();
        }
    }

    VoiceOption? _selectedVoice;
    public VoiceOption? SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            if (_selectedVoice == value) return;
            _selectedVoice = value;
            OnPropertyChanged();
        }
    }

    bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? StallId { get; private set; }
    public string? Token { get; private set; }

    public ICommand LoadDataCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }

    public LanguageSelectionViewModel(
        ILanguageService languageService,
        IVoiceService voiceService,
        IDeviceService deviceService,
        IDevicePreferenceApiService devicePreferenceApiService,
        ILogger<LanguageSelectionViewModel> logger)
    {
        _languageService = languageService;
        _voiceService = voiceService;
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
        _logger = logger;

        // Command tải dữ liệu ngôn ngữ ban đầu.
        LoadDataCommand = new Command(async () => await LoadLanguagesAsync());

        // Command xác nhận ngôn ngữ + giọng đọc và chuyển sang MapPage.
        ConfirmSelectionCommand = new Command(async () => await ConfirmSelectionAsync());
    }

    public void SetScanContext(string? stallId, string? token)
    {
        StallId = stallId;
        Token = token;
    }

    public async Task LoadLanguagesAsync()
    {
        if (IsBusy) return;

        try
        {
            // Chặn double tap nút xác nhận gây điều hướng lặp.
            if (Interlocked.CompareExchange(ref _navigationGuard, 1, 0) == 1)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            var languages = await _languageService.GetLanguagesAsync(forceRefresh: true);
            Languages.Clear();

            foreach (var language in languages)
            {
                var displayName = string.IsNullOrWhiteSpace(language.DisplayName) ? language.Name : language.DisplayName;

                Languages.Add(new LanguageOption
                {
                    Id = language.Id,
                    Code = language.Code,
                    Name = displayName,
                    FlagEmoji = ConvertFlagToEmoji(language.FlagCode)
                });
            }

            if (Languages.Count == 0)
            {
                ErrorMessage = "Không có ngôn ngữ khả dụng.";
                return;
            }

            SelectedLanguage = Languages.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tải danh sách ngôn ngữ");
            ErrorMessage = "Tải danh sách ngôn ngữ thất bại.";
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _navigationGuard, 0);
        }
    }

    private async Task LoadVoicesForSelectedLanguageAsync()
    {
        if (SelectedLanguage is null)
        {
            Voices.Clear();
            SelectedVoice = null;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Voices.Clear();

            var voiceList = await _voiceService.GetVoicesByLanguageAsync(SelectedLanguage.Id);
            foreach (var voice in voiceList.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Priority))
            {
                Voices.Add(new VoiceOption
                {
                    Id = voice.Id,
                    DisplayName = voice.DisplayName,
                    Description = voice.Description,
                    IsDefault = voice.IsDefault
                });
            }

            SelectedVoice = Voices.FirstOrDefault(v => v.IsDefault) ?? Voices.FirstOrDefault();

            if (Voices.Count == 0)
            {
                ErrorMessage = "Ngôn ngữ này chưa có giọng đọc khả dụng.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tải giọng đọc");
            ErrorMessage = "Tải giọng đọc thất bại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmSelectionAsync()
    {
        if (IsBusy) return;

        if (SelectedLanguage is null)
        {
            ErrorMessage = "Vui lòng chọn ngôn ngữ.";
            return;
        }

        if (SelectedVoice is null)
        {
            ErrorMessage = "Vui lòng chọn giọng đọc.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // Cập nhật language cho user profile (nếu user đã đăng nhập).
            _ = await _languageService.UpdateUserLanguageAsync(SelectedLanguage.Id.ToString());

            // Cập nhật device preference để API map trả audio đúng ngôn ngữ/voice.
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var deviceInfo = _deviceService.GetDeviceInfo();

            await _devicePreferenceApiService.UpsertAsync(new DevicePreferenceUpsertDto
            {
                DeviceId = deviceId,
                LanguageCode = SelectedLanguage.Code,
                Voice = SelectedVoice.Id.ToString(),
                AutoPlay = true,
                Platform = deviceInfo.Platform,
                DeviceModel = deviceInfo.DeviceModel,
                Manufacturer = deviceInfo.Manufacturer,
                OsVersion = deviceInfo.OsVersion
            });

            LanguageHelper.SetLanguage(SelectedLanguage.Code);

            // Chuyển sang map và focus stall đã quét (nếu có).
            var route = nameof(MapPage);
            if (!string.IsNullOrWhiteSpace(StallId))
            {
                route += $"?boothId={Uri.EscapeDataString(StallId)}";
            }

            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xác nhận lựa chọn ngôn ngữ/voice thất bại");
            ErrorMessage = "Không thể lưu lựa chọn. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string ConvertFlagToEmoji(string? flagCode)
    {
        if (string.IsNullOrWhiteSpace(flagCode) || flagCode.Length < 2)
            return "🌐";

        var code = flagCode.ToUpperInvariant();
        return char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A'))
             + char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class LanguageOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = "🌐";
    public string DisplayText => $"{FlagEmoji} {Name}";
}

public class VoiceOption
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public string DisplayText => IsDefault ? $"{DisplayName} (Mặc định)" : DisplayName;
}
