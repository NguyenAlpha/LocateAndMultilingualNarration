using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Helpers;
using Mobile.Services;
using Shared.DTOs.Languages;

namespace Mobile.ViewModels;

public class LanguageViewModel : INotifyPropertyChanged
{
    private readonly LanguageApiService _languageApiService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LanguageDetailDto> Languages { get; } = [];

    bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

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

    public LanguageViewModel(LanguageApiService languageApiService)
    {
        _languageApiService = languageApiService;
        LoadLanguagesCommand = new Command(async () => await LoadLanguagesAsync());
        SelectLanguageCommand = new Command<LanguageDetailDto>(async lang => await OnSelectLanguageAsync(lang));
    }

    public async Task LoadLanguagesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var languages = await _languageApiService.GetActiveLanguagesAsync();
            Languages.Clear();
            foreach (var lang in languages)
                Languages.Add(lang);

            if (Languages.Count == 0)
                ErrorMessage = "Không tải được danh sách ngôn ngữ từ server.";
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

    static async Task OnSelectLanguageAsync(LanguageDetailDto? language)
    {
        if (language is null || string.IsNullOrWhiteSpace(language.Code)) return;

        LanguageHelper.SetLanguage(language.Code);
        await Shell.Current.DisplayAlertAsync("Language", $"Đã chuyển sang {language.Name}", "OK");
        Application.Current!.Windows[0].Page = new AppShell();
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
