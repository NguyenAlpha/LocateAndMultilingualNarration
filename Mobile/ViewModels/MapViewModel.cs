using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mobile.Services;
using Shared.DTOs.Geo;

namespace Mobile.ViewModels;

/// <summary>
/// ViewModel cho MapPage — quản lý toàn bộ logic của trang bản đồ:
///   - Tải và hiển thị danh sách gian hàng
///   - Xử lý sự kiện chọn gian hàng và thông báo cho View di chuyển bản đồ
///   - Điều khiển phát/tạm dừng/dừng audio thuyết minh
/// </summary>
public class MapViewModel : INotifyPropertyChanged
{
    // Service lấy dữ liệu gian hàng từ API
    private readonly IStallService _stallService;

    // Service điều khiển phát audio thuyết minh
    private readonly IAudioGuideService _audioGuideService;

    private readonly ILocationLogService _locationLogService;

    private readonly ILogger<MapViewModel> _logger;

    // Cờ đánh dấu dữ liệu đã được tải lần đầu hay chưa — tránh gọi API trùng lặp
    private bool _isLoaded;

    // CancellationTokenSource để dừng polling khi rời MapPage
    private CancellationTokenSource? _pollingCts;

    // Tập hợp StallId đã được trigger trong lần ghé thăm hiện tại (đang phát hoặc đang chờ).
    // Stall bị xóa khỏi set khi user thoát khỏi vùng geofence → cho phép trigger lại khi quay lại.
    private readonly HashSet<Guid> _triggeredStallIds = [];

    // Hàng chờ audio: stall gần nhất ở đầu, phát tuần tự khi audio trước kết thúc.
    private Queue<GeoStallDto> _audioQueue = new();

    // ---- SỰ KIỆN (EVENTS) ----

    // Sự kiện chuẩn của INotifyPropertyChanged — UI tự động cập nhật khi property thay đổi
    public event PropertyChangedEventHandler? PropertyChanged;

    // Sự kiện thông báo cho MapPage (code-behind) di chuyển camera bản đồ đến gian hàng được chọn
    // ViewModel không trực tiếp thao tác MapView (không nên biết về View), nên dùng event để "ra lệnh"
    public event Action<GeoStallDto>? FocusStallRequested;

    // Sự kiện yêu cầu MapPage vẽ lại toàn bộ pin marker trên bản đồ
    // (khi tải xong dữ liệu mới hoặc khi đổi gian hàng đang chọn)
    public event Action? PinsRefreshRequested;

    // Sự kiện thông báo vị trí GPS mới nhất của người dùng — MapPage dùng để cập nhật pin vị trí
    public event Action<double, double>? LocationUpdated;
    // ---- DỮ LIỆU HIỂN THỊ ----

    // Danh sách gian hàng bind với CollectionView trong XAML
    // ObservableCollection tự động thông báo cho UI khi thêm/xóa phần tử
    public ObservableCollection<GeoStallDto> Stalls { get; } = [];

    // Gian hàng đang được chọn trong CollectionView
    GeoStallDto? selectedStall;
    public GeoStallDto? SelectedStall
    {
        get => selectedStall;
        set
        {
            if (selectedStall == value) return;
            selectedStall = value;
            OnPropertyChanged(); // Thông báo UI cập nhật
            OnPropertyChanged(nameof(HasSelectedStall));

            if (selectedStall != null)
            {
                // Yêu cầu MapPage di chuyển camera đến vị trí gian hàng vừa chọn
                FocusStallRequested?.Invoke(selectedStall);

                // Yêu cầu MapPage vẽ lại pin (để đổi màu pin đang chọn so với các pin còn lại)
                PinsRefreshRequested?.Invoke();
            }
        }
    }

    // Trạng thái đang tải dữ liệu — bind với ActivityIndicator trong XAML
    bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    // Thông báo lỗi hiển thị dưới cùng trang — bind với Label màu đỏ trong XAML
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

    // ---- COMMANDS (xử lý sự kiện từ các nút bấm) ----

    // Nút "Làm mới" — tải lại danh sách gian hàng từ API (bỏ qua cache)
    public ICommand RefreshCommand { get; }

    // Nút "▶ Phát" — phát audio thuyết minh của gian hàng đang chọn
    public ICommand PlayAudioCommand { get; }

    // Nút "⏸ Tạm dừng" — tạm dừng audio, giữ nguyên vị trí đang phát
    public ICommand PauseAudioCommand { get; }

    // Nút "⏹ Dừng" — dừng hẳn và reset về đầu
    public ICommand StopAudioCommand { get; }

    /// <summary>
    /// Constructor nhận service qua Dependency Injection (đăng ký trong MauiProgram.cs).
    /// Khởi tạo tất cả Command ngay tại đây.
    /// </summary>
    public MapViewModel(IStallService stallService, IAudioGuideService audioGuideService, ILocationLogService locationLogService, ILogger<MapViewModel> logger)
    {
        _stallService = stallService;
        _audioGuideService = audioGuideService;
        _locationLogService = locationLogService;
        _logger = logger;

        _audioGuideService.PlaybackCompleted += OnPlaybackCompleted;

        // forceRefresh = true: bỏ qua cache, luôn gọi API mới
        RefreshCommand = new Command(async () => await LoadStallsAsync(true));
        PlayAudioCommand = new Command(async () => await PlayAudioAsync());
        PauseAudioCommand = new Command(async () => await _audioGuideService.PauseAsync());
        StopAudioCommand = new Command(async () => await _audioGuideService.StopAsync());
    }

    /// <summary>
    /// Khởi tạo dữ liệu trang — được MapPage gọi trong OnAppearing().
    /// Chỉ tải dữ liệu lần đầu (lazy load), các lần sau không gọi lại API.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!_isLoaded)
        {
            await LoadStallsAsync(false);
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Tải lại dữ liệu gian hàng từ API — dùng khi người dùng quay lại MapPage sau khi đổi ngôn ngữ/giọng đọc.
    /// </summary>
    public Task ReloadAsync() => LoadStallsAsync(true);

    /// <summary>
    /// Cho phép code-behind MapPage chọn gian hàng theo chương trình
    /// (ví dụ: khi người dùng tap vào pin trên bản đồ thay vì tap vào danh sách).
    /// </summary>
    public void SelectStall(GeoStallDto stall)
    {
        _logger.LogInformation("SelectStall - StallId: {StallId}", stall.StallId);
        SelectedStall = stall;
    }

    /// <summary>
    /// Tải danh sách gian hàng từ API và cập nhật vào Stalls.
    /// </summary>
    /// <param name="forceRefresh">true = bỏ qua cache, gọi API mới; false = dùng cache nếu có</param>
    async Task LoadStallsAsync(bool forceRefresh)
    {
        // Chống gọi chồng — nếu đang tải thì bỏ qua
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty; // Xóa thông báo lỗi cũ

            var stalls = await _stallService.GetStallsAsync(forceRefresh);

            // Cập nhật ObservableCollection — UI tự động refresh CollectionView
            Stalls.Clear();
            foreach (var stall in stalls)
            {
                Stalls.Add(stall);
            }

            if (Stalls.Count == 0)
            {
                ErrorMessage = "Không có dữ liệu gian hàng để hiển thị.";
            }

            // Yêu cầu vẽ lại toàn bộ pin sau khi có dữ liệu mới
            PinsRefreshRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Tải bản đồ thất bại: {ex.Message}";
        }
        finally
        {
            // Luôn tắt loading dù thành công hay thất bại
            IsBusy = false;
        }
    }

    /// <summary>
    /// Phát audio thuyết minh của gian hàng đang chọn.
    /// Kiểm tra hợp lệ trước khi phát: phải có gian hàng được chọn và có AudioUrl.
    /// </summary>
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

    public async void PlayStall(GeoStallDto stall)
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

    // ---- AUDIO QUEUE ----

    /// <summary>
    /// Gọi khi audio kết thúc tự nhiên — phát stall tiếp theo trong hàng chờ nếu có.
    /// </summary>
    private async void OnPlaybackCompleted()
    {
        if (_audioQueue.TryDequeue(out var next))
        {
            _logger.LogInformation("[Queue] Phát tiếp: {StallName}", next.StallName);
            SelectedStall = next;
            await _audioGuideService.PlayAsync(next.NarrationContent!.AudioUrl!);
        }
    }

    // ---- GEOFENCING ----

    /// <summary>
    /// Bắt đầu polling GPS định kỳ. Được MapPage gọi trong OnAppearing.
    /// </summary>
    public void StartPolling()
    {
        StopPolling(); // hủy polling cũ nếu có
        _pollingCts = new CancellationTokenSource();
        _ = StartLocationPollingAsync(_pollingCts.Token);
    }

    /// <summary>
    /// Dừng polling GPS. Được MapPage gọi trong OnDisappearing.
    /// </summary>
    public void StopPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }

    /// <summary>
    /// Vòng lặp polling GPS mỗi 1 giây, kiểm tra geofence của từng stall.
    /// </summary>
    private async Task StartLocationPollingAsync(CancellationToken ct)
    {
        _logger.LogInformation("[Polling] Bắt đầu polling GPS");
        var tickCount = 0;

        while (!ct.IsCancellationRequested)
        {
            tickCount++;
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Low), ct);

                if (location is not null)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("[Polling] Tick #{Tick} — lat={Lat:F6}, lng={Lng:F6}",
                            tickCount, location.Latitude, location.Longitude);
                    LocationUpdated?.Invoke(location.Latitude, location.Longitude);
                    await CheckGeofencesAsync(location.Latitude, location.Longitude);
                    _locationLogService.TrySample(location.Latitude, location.Longitude, location.Accuracy);
                }
                else
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("[Polling] Tick #{Tick} — GPS trả về null", tickCount);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[Polling] Bị hủy ở tick #{Tick}", tickCount);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Polling] Tick #{Tick} lỗi GPS: {Message}", tickCount, ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct).ContinueWith(_ => { }); // không throw khi bị cancel
        }

        _logger.LogInformation("[Polling] Dừng polling GPS sau {Tick} tick", tickCount);
    }

    /// <summary>
    /// Kiểm tra geofence mỗi tick GPS.
    /// - Stall mới vào vùng → thêm vào hàng chờ (theo thứ tự gần nhất, không trùng lặp).
    /// - Stall thoát vùng → xóa khỏi hàng chờ và triggered set.
    /// - Nếu không đang phát → dequeue và phát ngay.
    /// </summary>
    private async Task CheckGeofencesAsync(double lat, double lng)
    {
        // Bước 1: tìm tất cả stall đang trong vùng, sắp xếp theo khoảng cách gần nhất
        var inRange = Stalls
            .Select(s => (stall: s, dist: CalculateDistance(lat, lng, s.Latitude, s.Longitude)))
            .Where(x => x.dist <= x.stall.RadiusMeters)
            .OrderBy(x => x.dist)
            .Select(x => x.stall)
            .ToList();

        var inRangeIds = inRange.Select(s => s.StallId).ToHashSet();

        // Bước 2: stall vừa thoát vùng → xóa khỏi triggered set + dọn khỏi queue
        var exited = _triggeredStallIds.Where(id => !inRangeIds.Contains(id)).ToList();
        if (exited.Count > 0)
        {
            foreach (var id in exited)
                _triggeredStallIds.Remove(id);

            // Queue<T> không hỗ trợ xóa theo điều kiện → rebuild bỏ stall đã thoát
            _audioQueue = new Queue<GeoStallDto>(_audioQueue.Where(s => inRangeIds.Contains(s.StallId)));

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[Geofence] {Count} stall thoát vùng", exited.Count);
        }

        // Bước 3: stall mới vào vùng → enqueue (nếu có audio và chưa trigger)
        foreach (var stall in inRange)
        {
            if (_triggeredStallIds.Contains(stall.StallId)) continue;
            if (string.IsNullOrWhiteSpace(stall.NarrationContent?.AudioUrl)) continue;

            _triggeredStallIds.Add(stall.StallId);
            _audioQueue.Enqueue(stall);
            _logger.LogInformation("[Queue] Thêm vào hàng chờ: {StallName} (queue size: {Size})",
                stall.StallName, _audioQueue.Count);
        }

        // Bước 4: nếu không đang phát → dequeue và phát ngay
        if (!_audioGuideService.IsPlaying && _audioQueue.TryDequeue(out var next))
        {
            _logger.LogInformation("[Queue] Bắt đầu phát: {StallName}", next.StallName);
            SelectedStall = next;
            await _audioGuideService.PlayAsync(next.NarrationContent!.AudioUrl!);
        }
    }

    /// <summary>
    /// Tính khoảng cách giữa 2 tọa độ GPS bằng công thức Haversine (đơn vị: mét).
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Kích hoạt sự kiện PropertyChanged để thông báo cho UI cập nhật property tương ứng.
    /// [CallerMemberName] tự động điền tên property đang gọi, không cần truyền thủ công.
    /// </summary>
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
