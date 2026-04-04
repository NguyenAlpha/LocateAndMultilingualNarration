using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    readonly SessionService sessionService;
    private int _quickActionNavigationGuard;

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

        StartCommand = new Command(async () => await NavigateQuickActionAsync(nameof(LanguagePage)));
        MapCommand = new Command(async () => await NavigateQuickActionAsync(nameof(MapPage)));
        LanguageCommand = new Command(async () => await NavigateQuickActionAsync(nameof(LanguagePage)));
        AudioCommand = new Command(async () => await NavigateQuickActionAsync("AudioPage"));
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

    /// <summary>
    /// Điều hướng quick action có guard để tránh nhấn liên tục gây điều hướng trùng.
    /// </summary>
    private async Task NavigateQuickActionAsync(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return;

        if (Interlocked.CompareExchange(ref _quickActionNavigationGuard, 1, 0) == 1)
            return;

        try
        {
            await Shell.Current.GoToAsync(route);
        }
        finally
        {
            Interlocked.Exchange(ref _quickActionNavigationGuard, 0);
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
