using Mobile.Models;
using Mobile.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Mobile.ViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private readonly ILanguageService _languageService;
        private readonly IVoiceService _voiceService;
        private readonly IDevicePreferenceApiService _devicePreferenceService;
        private readonly SessionService _sessionService;

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public ObservableCollection<LanguageDetailDto> AvailableLanguages { get; } = new();
        public ObservableCollection<VoiceOption> AvailableVoices { get; } = new();

        private LanguageDetailDto? _selectedLanguage;
        public LanguageDetailDto? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged();
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

        public ICommand SaveProfileCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand LoadProfileCommand { get; }

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

            _ = LoadProfileAsync();
        }

        public async Task LoadProfileAsync()
        {
            try
            {
                IsBusy = true;
                UserName = _sessionService.GetUserName() ?? "Du khách";

                var languages = await _languageService.GetLanguagesAsync(forceRefresh: true);

                AvailableLanguages.Clear();
                foreach (var lang in languages)
                {
                    AvailableLanguages.Add(new LanguageDetailDto
                    {
                        Id = lang.Id,
                        Code = lang.Code,
                        Name = lang.DisplayName ?? lang.Name,
                        NativeName = lang.Name,
                        Flag = ConvertFlagToEmoji(lang.FlagCode),
                        IsActive = lang.IsActive
                    });
                }

                var preference = await _devicePreferenceService.GetByDeviceIdAsync();
                if (preference is not null)
                {
                    SelectedLanguage = AvailableLanguages.FirstOrDefault(l =>
                        string.Equals(l.Code, preference.LanguageCode, StringComparison.OrdinalIgnoreCase));

                    SpeechRate = preference.SpeechRate <= 0 ? 1.0m : preference.SpeechRate;
                    AutoPlay = preference.AutoPlay;

                    if (!string.IsNullOrWhiteSpace(preference.Voice))
                    {
                        SelectedVoice = AvailableVoices.FirstOrDefault(v =>
                            string.Equals(v.Id, preference.Voice, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Lỗi", $"Không tải được hồ sơ: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
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
                var upsertDto = new DevicePreferenceUpsertDto
                {
                    LanguageId = SelectedLanguage.Id,
                    LanguageCode = SelectedLanguage.Code,
                    Voice = SelectedVoice?.Id,
                    SpeechRate = SpeechRate,
                    AutoPlay = AutoPlay
                };

                var result = await _devicePreferenceService.UpsertAsync(upsertDto);

                if (result.Success)
                {
                    await Application.Current!.MainPage!.DisplayAlertAsync("Thành công", "Đã lưu cấu hình thiết bị!", "OK");
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
        }

        /// <summary>
        /// Tải danh sách voice theo ngôn ngữ đang chọn.
        /// </summary>
        private async Task LoadVoicesBySelectedLanguageAsync()
        {
            AvailableVoices.Clear();

            if (SelectedLanguage is null)
            {
                SelectedVoice = null;
                return;
            }

            try
            {
                var voices = await _voiceService.GetVoicesByLanguageAsync(SelectedLanguage.Id);

                foreach (var voice in voices.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Priority))
                {
                    AvailableVoices.Add(new VoiceOption
                    {
                        Id = voice.Id.ToString(),
                        DisplayName = voice.DisplayName,
                        Description = voice.Description,
                        IsDefault = voice.IsDefault
                    });
                }

                // OLD CODE (kept for reference): chưa có chọn mặc định voice.
                SelectedVoice ??= AvailableVoices.FirstOrDefault(v => v.IsDefault) ?? AvailableVoices.FirstOrDefault();
            }
            catch
            {
                // Giữ im lặng để không chặn flow Profile khi API voice lỗi tạm thời.
            }
        }

        private async Task LogoutAsync()
        {
            bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Có", "Không");
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

        public class VoiceOption
        {
            public string Id { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsDefault { get; set; }
            public string DisplayText => IsDefault ? $"{DisplayName} (Mặc định)" : DisplayName;
        }
    }
}