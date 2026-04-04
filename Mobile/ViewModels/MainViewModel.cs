using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    readonly SessionService sessionService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }
    public ICommand MapCommand { get; }
    public ICommand LanguageCommand { get; }
    public ICommand AudioCommand { get; }
    public ICommand ProfileCommand { get; }
    public ICommand LogoutCommand { get; }

    string userName = "Guest";
    public string UserName
    {
        get => userName;
        set
        {
            if (userName == value) return;
            userName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HelloText));
        }
    }

    public string HelloText => $"Hello, {UserName}";

    public MainViewModel(SessionService sessionService)
    {
        this.sessionService = sessionService;
        LoadUserName();

        StartCommand = new Command(async () => await Shell.Current.GoToAsync("LanguagePage"));
        MapCommand = new Command(async () => await Shell.Current.GoToAsync("//MapPage"));
        LanguageCommand = new Command(async () => await Shell.Current.GoToAsync("LanguagePage"));
        AudioCommand = new Command(async () => await ShowAudioHintAsync());
        ProfileCommand = new Command(async () => await ShowProfileAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
    }

    public void LoadUserName()
    {
        UserName = sessionService.GetUserName();
    }

    async Task ShowAudioHintAsync()
    {
        if (Application.Current?.Windows[0].Page != null)
        {
            await Application.Current.Windows[0].Page!.DisplayAlertAsync("Audio", "Chọn gian hàng trên bản đồ để phát thuyết minh.", "OK");
        }
    }

    async Task ShowProfileAsync()
    {
        if (Application.Current?.Windows[0].Page != null)
        {
            await Application.Current.Windows[0].Page!.DisplayAlertAsync("Profile", "Trang cá nhân", "OK");
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
