using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Services;
using Shared.DTOs.Tours;

namespace Mobile.ViewModels;

/// <summary>
/// ViewModel cho TourDetailPage — hiển thị chi tiết tour + danh sách stops, cho phép bắt đầu/tiếp tục/huỷ.
/// </summary>
public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly ITourService _tourService;
    private readonly ILocalPreferenceService _localPreference;
    private readonly ILogger<TourDetailViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TourStopDetailDto> Stops { get; } = [];

    Guid _tourId;
    public Guid TourId
    {
        get => _tourId;
        set { if (_tourId == value) return; _tourId = value; OnPropertyChanged(); }
    }

    TourDetailDto? _tour;
    public TourDetailDto? Tour
    {
        get => _tour;
        set
        {
            if (_tour == value) return;
            _tour = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(EstimatedMinutesText));
            OnPropertyChanged(nameof(StopCountText));
            OnPropertyChanged(nameof(HasTour));
        }
    }

    public string Name => _tour?.Name ?? string.Empty;
    public string Description => _tour?.Description ?? string.Empty;
    public string EstimatedMinutesText => _tour?.EstimatedMinutes is int m ? $"≈ {m} phút" : "Không giới hạn thời gian";
    public string StopCountText => _tour is null ? string.Empty : $"{_tour.StopCount} điểm dừng";
    public bool HasTour => _tour is not null;

    bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(); }
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

    bool _isActiveTour;
    /// <summary>True nếu tour này đang là tour đang chạy trong LocalPreference.</summary>
    public bool IsActiveTour
    {
        get => _isActiveTour;
        set
        {
            if (_isActiveTour == value) return;
            _isActiveTour = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StartButtonText));
            OnPropertyChanged(nameof(ShowCancelButton));
        }
    }

    public string StartButtonText => IsActiveTour ? "Tiếp tục tour" : "Bắt đầu tour";
    public bool ShowCancelButton => IsActiveTour;

    public ICommand LoadCommand { get; }
    public ICommand StartTourCommand { get; }
    public ICommand CancelTourCommand { get; }

    public TourDetailViewModel(
        ITourService tourService,
        ILocalPreferenceService localPreference,
        ILogger<TourDetailViewModel> logger)
    {
        _tourService = tourService;
        _localPreference = localPreference;
        _logger = logger;

        LoadCommand = new Command(async () => await LoadAsync(forceRefresh: true));
        StartTourCommand = new Command(async () => await StartTourAsync());
        CancelTourCommand = new Command(() => CancelTour());
    }

    public async Task LoadAsync(bool forceRefresh = false)
    {
        if (TourId == Guid.Empty)
        {
            ErrorMessage = "Tour không hợp lệ.";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var detail = await _tourService.GetTourDetailAsync(TourId, forceRefresh);
            if (detail is null)
            {
                ErrorMessage = "Không tìm thấy tour.";
                Tour = null;
                Stops.Clear();
                return;
            }

            Tour = detail;
            Stops.Clear();
            foreach (var s in detail.Stops.OrderBy(x => x.Order))
                Stops.Add(s);

            IsActiveTour = _localPreference.GetActiveTourId() == detail.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TourDetailViewModel] LoadAsync thất bại");
            ErrorMessage = "Không tải được chi tiết tour.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartTourAsync()
    {
        if (Tour is null) return;

        try
        {
            // Nếu đang có tour khác đang chạy → confirm override + clear progress cũ.
            var currentActive = _localPreference.GetActiveTourId();
            if (currentActive is not null && currentActive != Tour.Id)
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page is not null)
                {
                    var confirm = await page.DisplayAlertAsync(
                        "Đổi tour",
                        "Bạn đang có một tour khác. Bắt đầu tour mới sẽ xoá tiến độ của tour cũ. Tiếp tục?",
                        "Đồng ý", "Huỷ");
                    if (!confirm) return;
                }
                _localPreference.ClearTourProgress();
            }

            _localPreference.SetActiveTourId(Tour.Id);
            IsActiveTour = true;

            await Shell.Current.GoToAsync($"//MapPage?tourId={Tour.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TourDetailViewModel] StartTourAsync thất bại");
            ErrorMessage = "Không thể bắt đầu tour.";
        }
    }

    private void CancelTour()
    {
        _localPreference.ClearTourProgress();
        IsActiveTour = false;
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
