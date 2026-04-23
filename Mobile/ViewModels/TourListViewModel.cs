using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Pages;
using Mobile.Services;
using Shared.DTOs.Tours;

namespace Mobile.ViewModels;

/// <summary>
/// ViewModel cho TourListPage — hiển thị danh sách tour active và điều hướng sang detail.
/// </summary>
public class TourListViewModel : INotifyPropertyChanged
{
    private readonly ITourService _tourService;
    private readonly ILogger<TourListViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TourListItemDto> Tours { get; } = [];

    bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoTours));
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
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);
    public bool HasNoTours => Tours.Count == 0 && !IsLoading;

    public ICommand LoadCommand { get; }
    public ICommand SelectTourCommand { get; }

    public TourListViewModel(ITourService tourService, ILogger<TourListViewModel> logger)
    {
        _tourService = tourService;
        _logger = logger;

        LoadCommand = new Command(async () => await LoadToursAsync(forceRefresh: true));
        SelectTourCommand = new Command<TourListItemDto>(async tour => await SelectTourAsync(tour));
    }

    public async Task LoadToursAsync(bool forceRefresh = false)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var tours = await _tourService.GetToursAsync(forceRefresh);

            Tours.Clear();
            foreach (var t in tours)
                Tours.Add(t);

            if (Tours.Count == 0)
                ErrorMessage = "Chưa có tour nào. Hãy kết nối internet để tải danh sách tour.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TourListViewModel] LoadToursAsync thất bại");
            ErrorMessage = "Không tải được danh sách tour.";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoTours));
        }
    }

    private async Task SelectTourAsync(TourListItemDto? tour)
    {
        if (tour is null) return;

        try
        {
            var route = $"{nameof(TourDetailPage)}?tourId={tour.Id}";
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TourListViewModel] Navigate sang TourDetailPage thất bại");
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
