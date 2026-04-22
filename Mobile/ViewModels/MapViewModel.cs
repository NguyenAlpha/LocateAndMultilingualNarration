using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Services;
using Shared.DTOs.Geo;
using Shared.DTOs.Tours;

namespace Mobile.ViewModels;

/// <summary>
/// Trạng thái vòng đời của MapPage — thay thế 3 bool cũ (_isLoaded/_isPopupOpen/_isStopping).
/// </summary>
public enum MapState
{
    Uninitialized,
    Syncing,
    Loading,
    Ready,
    Error
}

/// <summary>
/// ViewModel cho MapPage — binding + command, uỷ thác geofence cho <see cref="GeofenceEngine"/>.
/// </summary>
public class MapViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IStallService _stallService;
    private readonly IAudioGuideService _audioGuideService;
    private readonly IGpsPollingService _gpsPollingService;
    private readonly ISyncService _syncService;
    private readonly ITourService _tourService;
    private readonly ILocalPreferenceService _localPreference;
    private readonly ILogger<MapViewModel> _logger;

    // Geofence state tách sang engine riêng — VM không giữ triggered/queue/stalls nữa.
    private readonly GeofenceEngine _geofence;

    // Bảo vệ EnsureReadyAsync — tránh Page + Refresh race vào cùng lúc.
    private readonly SemaphoreSlim _readyGate = new(1, 1);

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<GeoStallDto>? FocusStallRequested;
    public event Action? PinsRefreshRequested;
    public event Action<double, double>? LocationUpdated;
    public event Action? TourRouteChanged;

    public ObservableCollection<GeoStallDto> Stalls { get; } = [];

    GeoStallDto? selectedStall;
    public GeoStallDto? SelectedStall
    {
        get => selectedStall;
        set
        {
            if (selectedStall == value) return;
            selectedStall = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedStall));

            if (selectedStall != null)
                FocusStallRequested?.Invoke(selectedStall);

            PinsRefreshRequested?.Invoke();
        }
    }

    MapState _state = MapState.Uninitialized;
    public MapState State
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    // Giữ IsBusy để XAML cũ không phải sửa — map sang State.
    public bool IsBusy => _state is MapState.Syncing or MapState.Loading;

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

    public bool HasSelectedStall => selectedStall != null;

    // ==================== Tour mode ====================

    Guid? _activeTourId;
    public Guid? ActiveTourId
    {
        get => _activeTourId;
        private set
        {
            if (_activeTourId == value) return;
            _activeTourId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTourMode));
        }
    }

    TourDetailDto? _activeTour;
    public TourDetailDto? ActiveTour
    {
        get => _activeTour;
        private set
        {
            if (_activeTour == value) return;
            _activeTour = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TourName));
            OnPropertyChanged(nameof(IsTourMode));
            OnPropertyChanged(nameof(TourProgressText));
        }
    }

    Guid? _nextStopStallId;
    public Guid? NextStopStallId
    {
        get => _nextStopStallId;
        private set
        {
            if (_nextStopStallId == value) return;
            _nextStopStallId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NextStopName));
            OnPropertyChanged(nameof(TourProgressText));
        }
    }

    public bool IsTourMode => _activeTour is not null;
    public string TourName => _activeTour?.Name ?? string.Empty;

    public string NextStopName
    {
        get
        {
            if (_activeTour is null || _nextStopStallId is null) return string.Empty;
            var stop = _activeTour.Stops.FirstOrDefault(s => s.StallId == _nextStopStallId.Value);
            return stop is null ? string.Empty : $"{stop.Order}. {stop.StallName}";
        }
    }

    public string TourProgressText
    {
        get
        {
            if (_activeTour is null) return string.Empty;
            var done = _localPreference.GetCompletedStops().Count(id => _activeTour.Stops.Any(s => s.StallId == id));
            return $"{done}/{_activeTour.StopCount} điểm đã qua";
        }
    }

    /// <summary>Trả về tọa độ các stop theo thứ tự để vẽ polyline.</summary>
    public IReadOnlyList<(double Lat, double Lng)> GetTourRouteCoordinates()
    {
        if (_activeTour is null) return [];
        return _activeTour.Stops
            .OrderBy(s => s.Order)
            .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
            .Select(s => ((double)s.Latitude!.Value, (double)s.Longitude!.Value))
            .ToList();
    }

    public ICommand RefreshCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand PauseAudioCommand { get; }
    public ICommand StopAudioCommand { get; }

    public MapViewModel(
        IStallService stallService,
        IAudioGuideService audioGuideService,
        IGpsPollingService gpsPollingService,
        ISyncService syncService,
        ITourService tourService,
        ILocalPreferenceService localPreference,
        ILogger<MapViewModel> logger)
    {
        _stallService = stallService;
        _audioGuideService = audioGuideService;
        _gpsPollingService = gpsPollingService;
        _syncService = syncService;
        _tourService = tourService;
        _localPreference = localPreference;
        _logger = logger;

        _geofence = new GeofenceEngine(audioGuideService, logger);
        _geofence.AutoPlayRequested += OnGeofenceAutoPlayAsync;

        _audioGuideService.PlaybackCompleted += OnPlaybackCompleted;
        _gpsPollingService.LocationUpdated += OnLocationUpdated;
        _syncService.AudioDownloaded += OnAudioDownloaded;

        RefreshCommand = new Command(async () => await RefreshAsync());
        PlayAudioCommand = new Command(async () => await PlayAudioAsync());
        PauseAudioCommand = new Command(async () => await _audioGuideService.PauseAsync());
        StopAudioCommand = new Command(async () => await _audioGuideService.StopAsync());
    }

    /// <summary>
    /// Idempotent init — sync-before-map + load stall. Gọi từ MapPage.OnAppearing.
    /// forceReload = true: bỏ qua cache stall và gọi SyncAsync force (dùng cho RefreshCommand).
    /// </summary>
    public async Task EnsureReadyAsync(bool forceReload = false, CancellationToken ct = default)
    {
        await _readyGate.WaitAsync(ct);
        try
        {
            if (!forceReload && State == MapState.Ready) return;

            ErrorMessage = string.Empty;

            State = MapState.Syncing;
            if (forceReload)
                await _syncService.SyncAsync(ct);
            else
                await _syncService.EnsureSyncedAsync(ct);

            State = MapState.Loading;
            await LoadStallsAsync(forceReload);

            State = MapState.Ready;
        }
        catch (OperationCanceledException)
        {
            State = MapState.Error;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapViewModel] EnsureReadyAsync thất bại");
            ErrorMessage = $"Tải bản đồ thất bại: {ex.Message}";
            State = MapState.Error;
        }
        finally
        {
            _readyGate.Release();
        }
    }

    public void SelectStall(GeoStallDto stall)
    {
        _logger.LogInformation("SelectStall - StallId: {StallId}", stall.StallId);
        SelectedStall = stall;
    }

    async Task RefreshAsync()
    {
        await _audioGuideService.StopAsync();
        _geofence.Reset();
        await EnsureReadyAsync(forceReload: true);
    }

    async Task LoadStallsAsync(bool forceRefresh)
    {
        SelectedStall = null;
        await _audioGuideService.StopAsync();

        var stalls = await _stallService.GetStallsAsync(forceRefresh);

        Stalls.Clear();
        foreach (var stall in stalls)
            Stalls.Add(stall);

        _geofence.SetStalls(stalls);

        if (Stalls.Count == 0)
            ErrorMessage = "Không có dữ liệu gian hàng để hiển thị.";

        PinsRefreshRequested?.Invoke();
    }

    async Task PlayAudioAsync()
    {
        var audioUrl = SelectedStall?.NarrationContent?.AudioUrl;
        if (SelectedStall is null || string.IsNullOrWhiteSpace(audioUrl))
        {
            ErrorMessage = "Gian hàng này chưa có audio.";
            return;
        }

        _logger.LogInformation("Phát audio cho stall - StallId: {StallId}", SelectedStall.StallId);
        ErrorMessage = string.Empty;
        await _audioGuideService.PlayAsync(audioUrl);
    }

    /// <summary>
    /// Phát audio của một stall cụ thể (popup gọi) — trả Task để caller có thể await.
    /// KHÔNG chạm SelectedStall: UI-selection và audio-target là 2 concept khác nhau.
    /// </summary>
    public async Task PlayStallAsync(GeoStallDto stall)
    {
        var audioUrl = stall.NarrationContent?.AudioUrl;
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            ErrorMessage = "Gian hàng này chưa có audio.";
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("PlayStall - StallId: {StallId}, AudioUrl: {AudioUrl}", stall.StallId, audioUrl);
        ErrorMessage = string.Empty;
        await _audioGuideService.PlayAsync(audioUrl);
    }

    private async void OnPlaybackCompleted()
    {
        try
        {
            await _geofence.OnPlaybackCompletedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapViewModel] OnPlaybackCompleted thất bại");
        }
    }

    private async void OnLocationUpdated(double lat, double lng, double? _)
    {
        LocationUpdated?.Invoke(lat, lng);
        try
        {
            await _geofence.OnLocationAsync(lat, lng);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapViewModel] OnLocationAsync thất bại");
        }
    }

    /// <summary>
    /// Engine quyết định phát stall → VM thực hiện phát audio. KHÔNG chạm SelectedStall
    /// để geofence không ghi đè lựa chọn UI của user (fix bug "tap A → geofence B → SelectedStall=B").
    /// Khi đang ở tour mode: mark stall đã hoàn tất, cập nhật NextStop, hiển thị alert khi tour xong.
    /// </summary>
    private async Task OnGeofenceAutoPlayAsync(GeoStallDto stall)
    {
        var audioUrl = stall.NarrationContent?.AudioUrl;
        if (string.IsNullOrWhiteSpace(audioUrl)) return;
        await _audioGuideService.PlayAsync(audioUrl);

        if (_activeTour is not null && _activeTour.Stops.Any(s => s.StallId == stall.StallId))
        {
            _localPreference.AddCompletedStop(stall.StallId);
            RecomputeNextStop();
            OnPropertyChanged(nameof(TourProgressText));

            if (_nextStopStallId is null)
                await CompleteTourAsync();
        }
    }

    /// <summary>
    /// Bật/tắt chế độ tour. Truyền null để tắt. Khi bật: tải chi tiết tour, giới hạn
    /// geofence chỉ trigger stall trong tour, tính NextStopStallId từ progress đã lưu.
    /// </summary>
    public async Task SetActiveTourAsync(Guid? tourId, CancellationToken ct = default)
    {
        if (tourId is null)
        {
            ActiveTour = null;
            ActiveTourId = null;
            NextStopStallId = null;
            _geofence.SetActiveTour(null);
            TourRouteChanged?.Invoke();
            return;
        }

        try
        {
            var tour = await _tourService.GetTourDetailAsync(tourId.Value, forceRefresh: false, ct);
            if (tour is null || !tour.IsActive)
            {
                _logger.LogWarning("[MapViewModel] Không load được tour {Id} hoặc tour đã tắt", tourId);
                _localPreference.ClearTourProgress();
                await SetActiveTourAsync(null, ct);
                return;
            }

            ActiveTour = tour;
            ActiveTourId = tour.Id;
            _geofence.SetActiveTour(tour.Stops.Select(s => s.StallId));
            _localPreference.SetActiveTourId(tour.Id);

            RecomputeNextStop();
            TourRouteChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapViewModel] SetActiveTourAsync thất bại");
        }
    }

    private void RecomputeNextStop()
    {
        if (_activeTour is null) { NextStopStallId = null; return; }

        var completed = _localPreference.GetCompletedStops();
        var next = _activeTour.Stops
            .OrderBy(s => s.Order)
            .FirstOrDefault(s => !completed.Contains(s.StallId));
        NextStopStallId = next?.StallId;
    }

    private async Task CompleteTourAsync()
    {
        var name = _activeTour?.Name ?? "tour";
        _localPreference.ClearTourProgress();

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.DisplayAlertAsync(
                "Hoàn thành tour",
                $"Bạn đã hoàn tất \"{name}\". Cảm ơn đã tham quan!",
                "OK");
        }

        await SetActiveTourAsync(null);
    }

    void OnAudioDownloaded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                _geofence.Reset();
                await LoadStallsAsync(false);
            }
            catch (Exception ex) { _logger.LogError(ex, "[MapViewModel] Reload sau AudioDownloaded thất bại"); }
        });
    }

    public void Dispose()
    {
        _audioGuideService.PlaybackCompleted -= OnPlaybackCompleted;
        _gpsPollingService.LocationUpdated -= OnLocationUpdated;
        _syncService.AudioDownloaded -= OnAudioDownloaded;
        _geofence.AutoPlayRequested -= OnGeofenceAutoPlayAsync;
        _readyGate.Dispose();
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
