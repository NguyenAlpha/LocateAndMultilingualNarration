using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    readonly SessionService sessionService = new();
    bool isAudioOn;
    string audioGlyph = "🔊";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }
    public ICommand MapCommand { get; }
    public ICommand LanguageCommand { get; }
    public ICommand AudioCommand { get; }
    public ICommand ProfileCommand { get; }
    public ICommand LogoutCommand { get; }

    public string AudioGlyph
    {
        get => audioGlyph;
        private set
        {
            if (audioGlyph == value) return;
            audioGlyph = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        StartCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LanguagePage)));
        MapCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(MapPage)));
        LanguageCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LanguagePage)));
        AudioCommand = new Command(async () => await ToggleAudioAsync());
        ProfileCommand = new Command(async () => await ShowProfileAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
    }

    async Task ToggleAudioAsync()
    {
        isAudioOn = !isAudioOn;
        AudioGlyph = isAudioOn ? "🔇" : "🔊";

        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Audio",
                isAudioOn ? "Đã bật thuyết minh" : "Đã tắt thuyết minh", "OK");
        }
    }

    async Task ShowProfileAsync()
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Profile", "Trang cá nhân", "OK");
        }
    }

    async Task LogoutAsync()
    {
        sessionService.ClearSession();
        await Shell.Current.GoToAsync("//StartPage");
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
