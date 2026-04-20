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
    readonly IQrService _qrAccessService;
    readonly IStallService _stallService;
    private int _quickActionNavigationGuard;

    private List<StallItem> _allStalls = [];
    private const int PageSize = 3;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand MapCommand { get; }
    public ICommand LanguageCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand LoadDataCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }

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
        set { if (isLoadingStalls == value) return; isLoadingStalls = value; OnPropertyChanged(); }
    }

    bool hasStalls;
    public bool HasStalls
    {
        get => hasStalls;
        set { if (hasStalls == value) return; hasStalls = value; OnPropertyChanged(); }
    }

    int currentPage = 1;
    public int CurrentPage
    {
        get => currentPage;
        set { if (currentPage == value) return; currentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentPageDisplay)); OnPropertyChanged(nameof(CanGoPrevious)); OnPropertyChanged(nameof(CanGoNext)); }
    }

    public int TotalPages => _allStalls.Count == 0 ? 1 : (int)Math.Ceiling(_allStalls.Count / (double)PageSize);
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public string CurrentPageDisplay => $"Trang {CurrentPage} / {TotalPages}";

    public MainViewModel(IQrService qrAccessService, IStallService stallService)
    {
        _qrAccessService = qrAccessService;
        _stallService = stallService;
        LoadUserName();

        MapCommand = new Command(async () => await NavigateQuickActionAsync("//MapPage"));
        LanguageCommand = new Command(async () => await NavigateQuickActionAsync(nameof(LanguagePage)));
        LogoutCommand = new Command(async () => await LogoutAsync());
        LoadDataCommand = new Command(async () => await LoadFeaturedStallsAsync());
        PreviousPageCommand = new Command(() => { if (CanGoPrevious) { CurrentPage--; RenderPage(); } });
        NextPageCommand = new Command(() => { if (CanGoNext) { CurrentPage++; RenderPage(); } });
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
            _allStalls = await _stallService.GetAllStallsAsync();
            CurrentPage = 1;
            RenderPage();
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

    // Hiển thị đúng trang hiện tại từ _allStalls
    private void RenderPage()
    {
        var pageItems = _allStalls.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        FeaturedStalls.Clear();
        foreach (var stall in pageItems)
            FeaturedStalls.Add(stall);

        HasStalls = _allStalls.Count > 0;
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CurrentPageDisplay));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    async Task ShowProfileAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//profile");
        }
        catch (Exception ex)
        {
            if (Application.Current?.Windows[0].Page != null)
                await Application.Current.Windows[0].Page!.DisplayAlertAsync("Lỗi", $"Không thể mở trang Hồ sơ: {ex.Message}", "OK");
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
                await Application.Current.Windows[0].Page!.DisplayAlertAsync("Lỗi điều hướng", ex.Message, "OK");
        }
        finally
        {
            Interlocked.Exchange(ref _quickActionNavigationGuard, 0);
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
