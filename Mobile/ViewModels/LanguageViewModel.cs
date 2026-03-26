using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Helpers;
using Mobile.Models;
using Mobile.Services;

namespace Mobile.ViewModels
{
    public class LanguageViewModel : INotifyPropertyChanged
    {
        private readonly ILanguageService _languageService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<AppLanguage> Languages { get; } = new();

        string? _selectedDisplay;
        public string? SelectedDisplay
        {
            get => _selectedDisplay;
            set
            {
                if (_selectedDisplay == value) return;
                _selectedDisplay = value;
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
            }
        }

        public ICommand LoadLanguagesCommand { get; }
        public ICommand SelectLanguageCommand { get; }

        public LanguageViewModel(ILanguageService languageService)
        {
            _languageService = languageService;

            SelectedDisplay = LanguageHelper.GetLanguageDisplay();
            LoadLanguagesCommand = new Command(async () => await LoadLanguagesAsync());
            SelectLanguageCommand = new Command<AppLanguage>(async lang => await OnSelectLanguageAsync(lang));
        }

        public async Task LoadLanguagesAsync(bool forceRefresh = false)
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                var languages = await _languageService.GetLanguagesAsync(forceRefresh);
                Languages.Clear();

                foreach (var language in languages)
                {
                    Languages.Add(language);
                }

                if (Languages.Count == 0)
                {
                    ErrorMessage = "Không tải được danh sách ngôn ngữ từ server.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải ngôn ngữ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task OnSelectLanguageAsync(AppLanguage? language)
        {
            if (language is null || string.IsNullOrWhiteSpace(language.Code))
            {
                return;
            }

            // Lưu local để UI đổi ngay lập tức.
            LanguageHelper.SetLanguage(language.Code);
            SelectedDisplay = LanguageHelper.GetLanguageDisplay();

            // Đồng bộ ngôn ngữ với backend nếu có token.
            _ = await _languageService.UpdateUserLanguageAsync(language.Id);

            await Shell.Current.DisplayAlert("Language", $"Đã chuyển sang {language.Name}", "OK");

            // Reload shell để các màn hình đọc lại state ngôn ngữ mới.
            Application.Current!.MainPage = new AppShell();

            // OLD CODE
            // try
            // {
            //     Shell.Current.GoToAsync("..");
            // }
            // catch
            // {
            // }
        }

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
