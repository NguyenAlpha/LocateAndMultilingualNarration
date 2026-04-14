using Microsoft.Extensions.Logging;
using Mobile.Models;
using Mobile.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Mobile.ViewModels;

public class StallListViewModel : INotifyPropertyChanged
{
    private readonly IStallService _stallService;
    private readonly ILogger<StallListViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<StallItem> Stalls { get; } = new();

    // Tìm kiếm
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            _ = LoadStallsAsync();
        }
    }

    // Filter hiện tại (All là mặc định vì chưa có khoảng cách thực tế)
    private string _currentFilter = "All";
    public string CurrentFilter
    {
        get => _currentFilter;
        set
        {
            _currentFilter = value;
            OnPropertyChanged();
            _ = LoadStallsAsync();
        }
    }

    // Phân trang
    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            _currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentPageDisplay));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public int PageSize { get; set; } = 10;
    public int TotalCount { get; private set; }

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage * PageSize < TotalCount;

    public string CurrentPageDisplay => $"Trang {CurrentPage} / {Math.Max(1, (TotalCount + PageSize - 1) / PageSize)}";

    public bool HasNoStalls => Stalls.Count == 0 && !IsLoading;

    private bool _isLoading = false;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoStalls));
        }
    }

    // Commands
    public ICommand SearchCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LoadMoreCommand { get; }

    public StallListViewModel(IStallService stallService, ILogger<StallListViewModel> logger)
    {
        _stallService = stallService;
        _logger = logger;

        SearchCommand = new Command(async () => await LoadStallsAsync());
        FilterCommand = new Command<string>(async filter =>
        {
            CurrentFilter = filter;
            CurrentPage = 1;
            await LoadStallsAsync();
        });

        PreviousPageCommand = new Command(async () =>
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadStallsAsync();
            }
        });

        NextPageCommand = new Command(async () =>
        {
            if (CanGoNext)
            {
                CurrentPage++;
                await LoadStallsAsync();
            }
        });

        LoadMoreCommand = new Command(async () =>
        {
            CurrentPage++;
            await LoadStallsAsync();
        });

        // Load dữ liệu khi khởi tạo
        _ = LoadStallsAsync();
    }

    private async Task LoadStallsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            var allStalls = await _stallService.GetAllStallsAsync(forceRefresh: true);

            // Lọc theo từ khóa tìm kiếm
            var filtered = allStalls.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLowerInvariant();
                filtered = filtered.Where(s =>
                    s.Name.ToLowerInvariant().Contains(term) ||
                    (s.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                    (s.Slug?.ToLowerInvariant().Contains(term) ?? false));
            }

            // Phân trang
            TotalCount = filtered.Count();
            var pagedStalls = filtered
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Stalls.Clear();
            foreach (var stall in pagedStalls)
                Stalls.Add(stall);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi tải danh sách gian hàng");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}