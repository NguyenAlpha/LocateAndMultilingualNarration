using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Models;
using Mobile.Services;

namespace Mobile.ViewModels;

public class LanguageViewModel : INotifyPropertyChanged
{
    private readonly ILanguageService _languageService;
    private readonly IVoiceService _voiceService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly ILocalPreferenceService _localPreference;
    private readonly ILogger<LanguageViewModel> _logger;

    // Lưu lựa chọn local gần nhất để tự động restore khi mở lại LanguagePage.
    private readonly Guid? _preferredLanguageId;
    private readonly string _preferredLanguageCode = string.Empty;
    private readonly Guid? _preferredVoiceId;

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

    private LanguageOption? _selectedLanguage;
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage == value) return;
            _selectedLanguage = value;
            OnPropertyChanged();
            if (value != null)
                _ = LoadVoicesForSelectedLanguageAsync();   // Tự động load giọng khi chọn ngôn ngữ
            OnPropertyChanged(nameof(IsReadyToContinue));
        }
    }

    private bool _isLanguagePopupOpen;
    public bool IsLanguagePopupOpen
    {
        get => _isLanguagePopupOpen;
        set
        {
            if (_isLanguagePopupOpen == value) return;
            _isLanguagePopupOpen = value;
            OnPropertyChanged();
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

    private string _errorMessage = string.Empty;
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

    private bool _isOfflineMode;
    public bool IsOfflineMode
    {
        get => _isOfflineMode;
        set
        {
            if (_isOfflineMode == value) return;
            _isOfflineMode = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadDataCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }

    public LanguageViewModel(
        ILanguageService languageService,
        IVoiceService voiceService,
        IDeviceService deviceService,
        IDevicePreferenceApiService devicePreferenceApiService,
        ILocalPreferenceService localPreference,
        ILogger<LanguageViewModel> logger)
    {
        _languageService = languageService;
        _voiceService = voiceService;
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
        _localPreference = localPreference;
        _logger = logger;

        // Đọc preference local để khôi phục lựa chọn cũ cho UX mượt hơn.
        var preferred = _localPreference.Load();
        _preferredLanguageId = preferred?.LanguageId;
        _preferredLanguageCode = preferred?.LanguageCode ?? string.Empty;
        _preferredVoiceId = preferred?.VoiceId;

        if (preferred is not null)
        {
            // Khôi phục thêm các setting phụ từ local để UI đồng nhất ngay khi mở trang.
            SpeechRate = preferred.SpeechRate;
            AutoPlay = preferred.AutoPlay;
        }

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
            IsOfflineMode = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

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
                    FlagEmoji = ConvertFlagToEmoji(lang.FlagCode)
                };

                _allLanguages.Add(option);
                Languages.Add(option);
            }

            if (Languages.Count == 0)
            {
                ErrorMessage = IsOfflineMode
                    ? "Không có ngôn ngữ trong bộ nhớ offline. Hãy kết nối mạng để tải dữ liệu lần đầu."
                    : "Không có ngôn ngữ khả dụng. Vui lòng kiểm tra lại kết nối.";
                return;
            }

            FilterLanguages();

            // Chọn ngôn ngữ đầu tiên làm mặc định
            // OLD CODE (kept for reference): SelectedLanguage = Languages.FirstOrDefault();
            // Ưu tiên ngôn ngữ đã lưu local (ID -> Code), fallback về item đầu tiên như cũ.
            SelectedLanguage = Languages.FirstOrDefault(x => _preferredLanguageId.HasValue && x.Id == _preferredLanguageId.Value)
                ?? Languages.FirstOrDefault(x => !string.IsNullOrWhiteSpace(_preferredLanguageCode)
                    && string.Equals(x.Code, _preferredLanguageCode, StringComparison.OrdinalIgnoreCase))
                ?? Languages.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tải danh sách ngôn ngữ");
            ErrorMessage = IsOfflineMode
                ? "Không tải được ngôn ngữ offline. Hãy mở mạng để đồng bộ dữ liệu lần đầu."
                : "Tải danh sách ngôn ngữ thất bại. Vui lòng kiểm tra kết nối.";
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
            IsOfflineMode = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

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

            // Tự động chọn giọng mặc định nếu có, ngược lại chọn giọng đầu tiên
            // OLD CODE (kept for reference): SelectedVoice = Voices.FirstOrDefault(v => v.IsDefault) ?? Voices.FirstOrDefault();
            // Ưu tiên voice đã lưu local khi khớp language hiện tại.
            var shouldUsePreferredVoice = _preferredVoiceId.HasValue
                && ((_preferredLanguageId.HasValue && _preferredLanguageId.Value == SelectedLanguage.Id)
                    || (!string.IsNullOrWhiteSpace(_preferredLanguageCode)
                        && string.Equals(_preferredLanguageCode, SelectedLanguage.Code, StringComparison.OrdinalIgnoreCase)));

            SelectedVoice = shouldUsePreferredVoice
                ? Voices.FirstOrDefault(v => v.Id == _preferredVoiceId!.Value)
                    ?? Voices.FirstOrDefault(v => v.IsDefault)
                    ?? Voices.FirstOrDefault()
                : Voices.FirstOrDefault(v => v.IsDefault) ?? Voices.FirstOrDefault();

            if (Voices.Count == 0)
            {
                ErrorMessage = IsOfflineMode
                    ? "Ngôn ngữ này chưa có giọng đọc trong cache offline."
                    : "Ngôn ngữ này hiện chưa có giọng đọc khả dụng.";
            }
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

            // OLD CODE (kept for reference): var deviceId = _deviceService.GetOrCreateDeviceId();
            var isOffline = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

            var upsertDto = new Mobile.Models.DevicePreferenceUpsertDto
            {
                LanguageId = SelectedLanguage.Id,
                VoiceId = SelectedVoice.Id,
                SpeechRate = SpeechRate,
                AutoPlay = AutoPlay
            };

            // Offline mode: lưu local để user vẫn đổi ngôn ngữ/voice được ngay.
            if (isOffline)
            {
                _localPreference.Save(new Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto
                {
                    DeviceId = _deviceService.GetOrCreateDeviceId(),
                    LanguageId = SelectedLanguage.Id,
                    LanguageCode = SelectedLanguage.Code,
                    LanguageName = SelectedLanguage.Name,
                    LanguageDisplayName = SelectedLanguage.NativeName,
                    VoiceId = SelectedVoice.Id,
                    VoiceDisplayName = SelectedVoice.DisplayName,
                    SpeechRate = SpeechRate,
                    AutoPlay = AutoPlay
                });

                await Shell.Current.GoToAsync("//MapPage");
                return;
            }

            var result = await _devicePreferenceApiService.UpsertAsync(upsertDto);

            if (result.Success)
            {
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

// ====================== Supporting Classes ======================
// Hai class hỗ trợ UI (giữ nguyên như cũ của bạn)

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