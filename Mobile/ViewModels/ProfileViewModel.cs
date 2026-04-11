using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Models;
using Mobile.Services;

namespace Mobile.ViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private readonly ILanguageService _languageService;
        private readonly IVoiceService _voiceService;
        private readonly IDevicePreferenceApiService _devicePreferenceService;
        private readonly SessionService _sessionService;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Thông tin cơ bản
        private string _userName = "Du khách";
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        private string _userEmail = string.Empty;
        public string UserEmail
        {
            get => _userEmail;
            set { _userEmail = value; OnPropertyChanged(); }
        }

        private string _userAvatar = "dotnet_bot.png";
        public string UserAvatar
        {
            get => _userAvatar;
            set { _userAvatar = value; OnPropertyChanged(); }
        }

        // Danh sách
        public ObservableCollection<LanguageDetailDto> AvailableLanguages { get; } = new();
        public ObservableCollection<VoiceOption> AvailableVoices { get; } = new();

        // Lựa chọn
        private LanguageDetailDto? _selectedLanguage;
        public LanguageDetailDto? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                if (value != null)
                    _ = LoadVoicesBySelectedLanguageAsync();
            }
        }

        private VoiceOption? _selectedVoice;
        public VoiceOption? SelectedVoice
        {
            get => _selectedVoice;
            set
            {
                _selectedVoice = value;
                OnPropertyChanged();
            }
        }

        private decimal _speechRate = 1.0m;
        public decimal SpeechRate
        {
            get => _speechRate;
            set
            {
                if (Math.Abs(_speechRate - value) < 0.01m) return;
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

        private bool _isBusy;
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

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SaveProfileCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand LoadProfileCommand { get; }
        public ICommand ResetSettingsCommand { get; }

        public ProfileViewModel(
            ILanguageService languageService,
            IVoiceService voiceService,
            IDevicePreferenceApiService devicePreferenceService,
            SessionService sessionService)
        {
            _languageService = languageService;
            _voiceService = voiceService;
            _devicePreferenceService = devicePreferenceService;
            _sessionService = sessionService;

            SaveProfileCommand = new Command(async () => await SaveProfileAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());
            LoadProfileCommand = new Command(async () => await LoadProfileAsync());
            ResetSettingsCommand = new Command(async () => await ResetToDefaultAsync());

            _ = LoadProfileAsync();
        }

        public async Task LoadProfileAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Đang tải thông tin hồ sơ...";

                UserName = _sessionService.GetUserName() ?? "Du khách";

                // Load danh sách ngôn ngữ
                var languages = await _languageService.GetLanguagesAsync(forceRefresh: true);
                AvailableLanguages.Clear();

                foreach (var lang in languages.Where(l => l.IsActive))
                {
                    AvailableLanguages.Add(new LanguageDetailDto
                    {
                        Id = lang.Id,
                        Code = lang.Code,
                        Name = lang.DisplayName ?? lang.Name,
                        // OLD CODE (kept for reference): NativeName = lang.NativeName ?? lang.Name,
                        // Shared LanguageDetailDto hiện chưa có NativeName nên fallback theo Name.
                        NativeName = lang.Name,
                        Flag = ConvertFlagToEmoji(lang.FlagCode),
                        IsActive = lang.IsActive
                    });
                }

                // Load cấu hình thiết bị hiện tại
                var preference = await _devicePreferenceService.GetByDeviceIdAsync();
                if (preference != null)
                {
                    SelectedLanguage = AvailableLanguages.FirstOrDefault(l =>
                        string.Equals(l.Code, preference.LanguageCode, StringComparison.OrdinalIgnoreCase));

                    SpeechRate = preference.SpeechRate > 0 ? preference.SpeechRate : 1.0m;
                    AutoPlay = preference.AutoPlay;

                    if (preference.VoiceId.HasValue)
                    {
                        SelectedVoice = AvailableVoices.FirstOrDefault(v => v.Id == preference.VoiceId.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Không tải được hồ sơ";
                await Application.Current!.MainPage!.DisplayAlertAsync("Lỗi", $"Không thể tải hồ sơ: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadVoicesBySelectedLanguageAsync()
        {
            AvailableVoices.Clear();
            SelectedVoice = null;

            if (SelectedLanguage == null) return;

            try
            {
                var voices = await _voiceService.GetVoicesByLanguageAsync(SelectedLanguage.Id);

                foreach (var voice in voices.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Priority))
                {
                    AvailableVoices.Add(new VoiceOption
                    {
                        Id = voice.Id,
                        DisplayName = voice.DisplayName,
                        Description = voice.Description ?? "Giọng đọc chuẩn",
                        IsDefault = voice.IsDefault
                    });
                }

                SelectedVoice = AvailableVoices.FirstOrDefault(v => v.IsDefault) ?? AvailableVoices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Không chặn flow nếu API voice lỗi
                System.Diagnostics.Debug.WriteLine($"Load voice error: {ex.Message}");
            }
        }

        private async Task SaveProfileAsync()
        {
            if (SelectedLanguage == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Thông báo", "Vui lòng chọn ngôn ngữ ưu tiên", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Đang lưu cấu hình...";

                var upsertDto = new DevicePreferenceUpsertDto
                {
                    LanguageId = SelectedLanguage.Id,
                    VoiceId = SelectedVoice?.Id,
                    SpeechRate = SpeechRate,
                    AutoPlay = AutoPlay
                };

                var result = await _devicePreferenceService.UpsertAsync(upsertDto);

                if (result.Success)
                {
                    StatusMessage = "Đã lưu thành công!";
                    await Application.Current!.MainPage!.DisplayAlertAsync("Thành công", "Cấu hình thuyết minh đã được cập nhật.", "OK");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlertAsync("Lỗi", result.Error?.Message ?? "Lưu thất bại", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ResetToDefaultAsync()
        {
            bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Đặt lại mặc định",
                "Bạn có muốn đặt lại tất cả cài đặt về mặc định không?",
                "Có", "Không");

            if (!confirm) return;

            SpeechRate = 1.0m;
            AutoPlay = true;
            SelectedVoice = null;
            await Application.Current!.MainPage!.DisplayAlertAsync("Thành công", "Đã đặt lại cài đặt về mặc định.", "OK");
        }

        private async Task LogoutAsync()
        {
            bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Đăng xuất",
                "Bạn có chắc muốn đăng xuất khỏi thiết bị này?",
                "Có", "Không");

            if (!confirm) return;

            _sessionService.ClearSession();
            await Shell.Current.GoToAsync("//StartPage");
        }

        private static string ConvertFlagToEmoji(string? flagCode)
        {
            if (string.IsNullOrWhiteSpace(flagCode) || flagCode.Length < 2)
                return "🌐";

            var code = flagCode.ToUpperInvariant();
            return char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A'))
                 + char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Class hỗ trợ hiển thị giọng đọc trong Picker
        public class VoiceOption
        {
            public Guid Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsDefault { get; set; }

            public string DisplayText => IsDefault
                ? $"{DisplayName} (Mặc định)"
                : DisplayName;
        }
    }
}