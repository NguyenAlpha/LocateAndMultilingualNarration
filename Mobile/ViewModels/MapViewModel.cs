using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Services;
using Shared.DTOs.Geo;

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
    private readonly ILogger<MapViewModel> _logger;

    // Geofence state tách sang engine riêng — VM không giữ triggered/queue/stalls nữa.
    private readonly GeofenceEngine _geofence;

    // Bảo vệ EnsureReadyAsync — tránh Page + Refresh race vào cùng lúc.
    private readonly SemaphoreSlim _readyGate = new(1, 1);

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<GeoStallDto>? FocusStallRequested;
    public event Action? PinsRefreshRequested;
    public event Action<double, double>? LocationUpdated;

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

    public ICommand RefreshCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand PauseAudioCommand { get; }
    public ICommand StopAudioCommand { get; }

    public MapViewModel(
        IStallService stallService,
        IAudioGuideService audioGuideService,
        IGpsPollingService gpsPollingService,
        ISyncService syncService,
        ILogger<MapViewModel> logger)
    {
        _stallService = stallService;
        _audioGuideService = audioGuideService;
        _gpsPollingService = gpsPollingService;
        _syncService = syncService;
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
    /// </summary>
    private async Task OnGeofenceAutoPlayAsync(GeoStallDto stall)
    {
        var audioUrl = stall.NarrationContent?.AudioUrl;
        if (string.IsNullOrWhiteSpace(audioUrl)) return;
        await _audioGuideService.PlayAsync(audioUrl);
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
