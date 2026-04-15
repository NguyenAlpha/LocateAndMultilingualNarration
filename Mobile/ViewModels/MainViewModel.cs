using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Mobile.Models;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    readonly IQrAccessService _qrAccessService;
    readonly IStallService stallService;
    private int _quickActionNavigationGuard;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }
    public ICommand MapCommand { get; }
    public ICommand LanguageCommand { get; }
    public ICommand AudioCommand { get; }
    public ICommand ProfileCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand LoadDataCommand { get; }
    public ICommand StallListCommand { get; }

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

    public ObservableCollection<StallItem> FeaturedStalls { get; } = new();

    bool isLoadingStalls;
    public bool IsLoadingStalls
    {
        get => isLoadingStalls;
        set
        {
            if (isLoadingStalls == value) return;
            isLoadingStalls = value;
            OnPropertyChanged();
        }
    }

    bool hasStalls;
    public bool HasStalls
    {
        get => hasStalls;
        set
        {
            if (hasStalls == value) return;
            hasStalls = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(IQrAccessService qrAccessService, IStallService stallService)
    {
        _qrAccessService = qrAccessService;
        this.stallService = stallService;
        LoadUserName();

        StartCommand = new Command(async () => await NavigateQuickActionAsync(nameof(LanguagePage)));
        // OLD CODE (kept for reference): MapCommand = new Command(async () => await NavigateQuickActionAsync(nameof(MapPage)));
        // OLD CODE (kept for reference): MapCommand = new Command(async () => await NavigateQuickActionAsync($"//{nameof(MapPage)}"));
        MapCommand = new Command(async () => await NavigateQuickActionAsync("//MapPage"));
        LanguageCommand = new Command(async () => await NavigateQuickActionAsync(nameof(LanguagePage)));
        AudioCommand = new Command(async () => await NavigateQuickActionAsync("AudioPage"));
        ProfileCommand = new Command(async () => await ShowProfileAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        LoadDataCommand = new Command(async () => await LoadFeaturedStallsAsync());
        StallListCommand = new Command(async () => await NavigateToStallListAsync());

        _ = LoadFeaturedStallsAsync();
    }
    private async Task GoToStallListAsync()
    {
        await Shell.Current.GoToAsync("StallListPage");
    }

    public void LoadUserName()
    {
        UserName = "Du khách";
    }

    public async Task LoadFeaturedStallsAsync()
    {
        if (IsLoadingStalls) return;

        try
        {
            IsLoadingStalls = true;
            var stalls = await stallService.GetFeaturedStallsAsync();

            FeaturedStalls.Clear();
            foreach (var stall in stalls)
            {
                FeaturedStalls.Add(stall);
            }

            HasStalls = FeaturedStalls.Count > 0;
        }
        catch
        {
            HasStalls = false;
        }
        finally
        {
            IsLoadingStalls = false;
        }
    }

    async Task ShowAudioHintAsync()
    {
        if (Application.Current?.Windows[0].Page != null)
        {
            await Application.Current.Windows[0].Page!.DisplayAlertAsync("Audio", "Chọn gian hàng trên bản đồ để phát thuyết minh.", "OK");
        }
    }

    // OLD CODE (kept for reference): ShowProfileAsync cũ chỉ hiển thị alert.
    // async Task ShowProfileAsync()
    // {
    //     if (Application.Current?.Windows[0].Page != null)
    //     {
    //         await Application.Current.Windows[0].Page!.DisplayAlertAsync("Profile", "Trang cá nhân", "OK");
    //     }
    // }
    async Task ShowProfileAsync()
    {
        try
        {
            // OLD CODE (kept for reference): await Shell.Current.GoToAsync(nameof(ProfilePage));
            await Shell.Current.GoToAsync("//profile");
        }
        catch (Exception ex)
        {
            if (Application.Current?.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync("Lỗi", $"Không thể mở trang Hồ sơ: {ex.Message}", "OK");
            }
        }
    }

    async Task LogoutAsync()
    {
        _qrAccessService.ClearAccess();
        await Shell.Current.GoToAsync("//ScanPage");
    }

    private async Task NavigateQuickActionAsync(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return;

        if (Interlocked.CompareExchange(ref _quickActionNavigationGuard, 1, 0) == 1)
            return;

        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            if (Application.Current?.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync("Lỗi điều hướng", ex.Message, "OK");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _quickActionNavigationGuard, 0);
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private async Task NavigateToStallListAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("StallListPage");
        }
        catch (Exception ex)
        {
            // Log lỗi nếu cần
            Console.WriteLine($"Navigate to StallListPage failed: {ex.Message}");
        }
    }
}
