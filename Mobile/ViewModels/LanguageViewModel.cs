using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Helpers;
using Mobile.Models;
using Mobile.Services;

namespace Mobile.ViewModels;

public class LanguageViewModel : INotifyPropertyChanged
{
    private readonly ILanguageService _languageService;
    private readonly IVoiceService _voiceService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly ILogger<LanguageViewModel> _logger;

    private int _navigationGuard;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly List<LanguageOption> _allLanguages = new();

    public ObservableCollection<LanguageOption> Languages { get; } = new();
    public ObservableCollection<LanguageOption> FilteredLanguages { get; } = new();
    public ObservableCollection<VoiceOption> Voices { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            FilterLanguages();
        }
    }

    LanguageOption? _selectedLanguage;
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage == value) return;
            _selectedLanguage = value;
            OnPropertyChanged();
            if (value != null)
                _ = LoadVoicesForSelectedLanguageAsync();
            OnPropertyChanged(nameof(IsReadyToContinue));
        }
    }

    private decimal _speechRate = 1.0m;
    public decimal SpeechRate
    {
        get => _speechRate;
        set
        {
            if (_speechRate == value) return;
            _speechRate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpeechRateText));
        }
    }

    public string SpeechRateText => $"{SpeechRate:F1}x";

    private bool _autoPlay = true;
    public bool AutoPlay
    {
        get => _autoPlay;
        set
        {
            if (_autoPlay == value) return;
            _autoPlay = value;
            OnPropertyChanged();
        }
    }

    private VoiceOption? _selectedVoice;
    public VoiceOption? SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            if (_selectedVoice == value) return;
            _selectedVoice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReadyToContinue));
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReadyToContinue));
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
    public bool IsReadyToContinue => SelectedLanguage != null && SelectedVoice != null && !IsBusy;

    public ICommand LoadDataCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }

    public LanguageViewModel(
        ILanguageService languageService,
        IVoiceService voiceService,
        IDeviceService deviceService,
        IDevicePreferenceApiService devicePreferenceApiService,
        ILogger<LanguageViewModel> logger)
    {
        _languageService = languageService;
        _voiceService = voiceService;
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
        _logger = logger;

        LoadDataCommand = new Command(async () => await LoadLanguagesAsync());
        ConfirmSelectionCommand = new Command(async () => await ConfirmSelectionAsync());
    }

    public async Task LoadLanguagesAsync()
    {
        if (IsBusy) return;

        try
        {
            if (Interlocked.CompareExchange(ref _navigationGuard, 1, 0) == 1)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            var languages = await _languageService.GetActiveLanguagesAsync();

            _allLanguages.Clear();
            Languages.Clear();

            foreach (var lang in languages)
            {
                var option = new LanguageOption
                {
                    Id = lang.Id,
                    Code = lang.Code ?? string.Empty,
                    Name = lang.Name ?? lang.DisplayName ?? "Unknown Language",
                    NativeName = lang.DisplayName ?? lang.Name ?? "Unknown",
                    // SỬA LỖI 1: Không dùng FlagUrl nữa → dùng FlagCode hoặc bỏ qua, fallback emoji
                    FlagEmoji = ConvertFlagToEmoji(lang.FlagCode)   // đổi từ FlagUrl sang FlagCode (an toàn hơn)
                };

                _allLanguages.Add(option);
                Languages.Add(option);
            }

            if (Languages.Count == 0)
            {
                ErrorMessage = "Không có ngôn ngữ khả dụng. Vui lòng kiểm tra lại kết nối.";
                return;
            }

            FilterLanguages();

            SelectedLanguage = Languages.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tải danh sách ngôn ngữ");
            ErrorMessage = "Tải danh sách ngôn ngữ thất bại. Vui lòng kiểm tra kết nối.";
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _navigationGuard, 0);
        }
    }

    private void FilterLanguages()
    {
        FilteredLanguages.Clear();

        var searchTerm = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;

        var filtered = string.IsNullOrWhiteSpace(searchTerm)
            ? _allLanguages
            : _allLanguages.Where(l =>
                l.Name.ToLowerInvariant().Contains(searchTerm) ||
                l.NativeName.ToLowerInvariant().Contains(searchTerm) ||
                l.Code.ToLowerInvariant().Contains(searchTerm)).ToList();

        foreach (var lang in filtered)
            FilteredLanguages.Add(lang);
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
            Voices.Clear();
            ErrorMessage = string.Empty;

            var voiceList = await _voiceService.GetVoicesByLanguageAsync(SelectedLanguage.Id);

            foreach (var voice in voiceList.OrderByDescending(v => v.IsDefault).ThenBy(v => v.DisplayName))
            {
                Voices.Add(new VoiceOption
                {
                    Id = voice.Id,
                    DisplayName = voice.DisplayName ?? "Unnamed Voice",
                    Description = voice.Description,
                    IsDefault = voice.IsDefault
                });
            }

            SelectedVoice = Voices.FirstOrDefault(v => v.IsDefault) ?? Voices.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Không thể tải giọng đọc cho language {SelectedLanguage.Id}");
            ErrorMessage = "Không thể tải danh sách giọng đọc. Vui lòng chọn ngôn ngữ khác.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmSelectionAsync()
    {
        if (IsBusy || SelectedLanguage is null || SelectedVoice is null) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var deviceId = _deviceService.GetOrCreateDeviceId();

            var upsertDto = new DevicePreferenceUpsertDto
            {
                // SỬA LỖI 2: Xóa dòng DeviceId vì DTO không có property này
                // DeviceId = deviceId,     ← bị xóa để tránh lỗi CS0117

                LanguageId = SelectedLanguage.Id,
                VoiceId = SelectedVoice.Id,
                SpeechRate = SpeechRate,
                AutoPlay = AutoPlay
            };

            var result = await _devicePreferenceApiService.UpsertAsync(upsertDto);

            if (result.Success)
            {
                LanguageHelper.SetLanguage(SelectedLanguage.Code);
                await Shell.Current.GoToAsync("//MapPage");
            }
            else
            {
                ErrorMessage = result.Error?.Message ?? "Lưu cấu hình thất bại.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xác nhận lựa chọn ngôn ngữ & giọng đọc thất bại");
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

        var code = flagCode.ToUpperInvariant().Trim();
        if (code.Length == 2)
        {
            return char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A')) +
                   char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));
        }

        return "🌐";
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Hai class hỗ trợ UI
public class LanguageOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = "🌐";
}

public class VoiceOption
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }

    public string DisplayText => IsDefault ? $"{DisplayName} (Mặc định)" : DisplayName;
}