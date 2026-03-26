using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

    // Cờ đánh dấu dữ liệu đã được tải lần đầu hay chưa — tránh gọi API trùng lặp
    private bool _isLoaded;

    // CancellationTokenSource để dừng polling khi rời MapPage
    private CancellationTokenSource? _pollingCts;

    // StallId đang được geofence tự động chọn (tránh retrigger)
    private Guid? _geofenceTriggeredStallId;

    // ---- SỰ KIỆN (EVENTS) ----

    // Sự kiện chuẩn của INotifyPropertyChanged — UI tự động cập nhật khi property thay đổi
    public event PropertyChangedEventHandler? PropertyChanged;

    // Sự kiện thông báo cho MapPage (code-behind) di chuyển camera bản đồ đến gian hàng được chọn
    // ViewModel không trực tiếp thao tác MapView (không nên biết về View), nên dùng event để "ra lệnh"
    public event Action<GeoStallDto>? FocusStallRequested;

    // Sự kiện yêu cầu MapPage vẽ lại toàn bộ pin marker trên bản đồ
    // (khi tải xong dữ liệu mới hoặc khi đổi gian hàng đang chọn)
    public event Action? PinsRefreshRequested;

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
    public MapViewModel(IStallService stallService, IAudioGuideService audioGuideService)
    {
        _stallService = stallService;
        _audioGuideService = audioGuideService;

        // forceRefresh = true: bỏ qua cache, luôn gọi API mới
        RefreshCommand = new Command(async () => await LoadStallsAsync(true));
        PlayAudioCommand  = new Command(async () => await PlayAudioAsync());
        PauseAudioCommand = new Command(async () => await _audioGuideService.PauseAsync());
        StopAudioCommand  = new Command(async () => await _audioGuideService.StopAsync());
    }

    /// <summary>
    /// Khởi tạo dữ liệu trang — được MapPage gọi trong OnAppearing().
    /// Chỉ tải dữ liệu lần đầu (lazy load), các lần sau không gọi lại API.
    /// Nếu có boothId (từ QR code), tự động chọn và focus vào gian hàng đó.
    /// </summary>
    /// <param name="boothId">ID gian hàng cần focus ngay khi vào trang (có thể null)</param>
    public async Task InitializeAsync(string? boothId = null)
    {
        if (!_isLoaded)
        {
            await LoadStallsAsync(false); // false = dùng cache nếu có
            _isLoaded = true;
        }

        // Nếu có boothId được truyền vào (ví dụ từ kết quả quét QR), tự động chọn gian hàng đó
        if (!string.IsNullOrWhiteSpace(boothId))
        {
            // OrdinalIgnoreCase: so sánh không phân biệt hoa thường
            SelectedStall = Stalls.FirstOrDefault(x => x.StallId.ToString().Equals(boothId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Cho phép code-behind MapPage chọn gian hàng theo chương trình
    /// (ví dụ: khi người dùng tap vào pin trên bản đồ thay vì tap vào danh sách).
    /// </summary>
    public void SelectStall(GeoStallDto stall)
    {
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
        if (SelectedStall is null || string.IsNullOrWhiteSpace(SelectedStall.AudioUrl))
        {
            ErrorMessage = "Gian hàng này chưa có audio.";
            return;
        }

        ErrorMessage = string.Empty;
        await _audioGuideService.PlayAsync(SelectedStall.AudioUrl);
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
    /// Vòng lặp polling GPS mỗi 5 giây, kiểm tra geofence của từng stall.
    /// </summary>
    private async Task StartLocationPollingAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium), ct);

                if (location is not null)
                    await CheckGeofencesAsync(location.Latitude, location.Longitude);
            }
            catch { /* ignore — GPS không khả dụng hoặc bị hủy */ }

            await Task.Delay(TimeSpan.FromSeconds(5), ct).ContinueWith(_ => { }); // không throw khi bị cancel
        }
    }

    /// <summary>
    /// Kiểm tra xem vị trí hiện tại có nằm trong geofence của stall nào không.
    /// Nếu có, tự động chọn stall và phát audio (nếu chưa phát).
    /// </summary>
    private async Task CheckGeofencesAsync(double lat, double lng)
    {
        foreach (var stall in Stalls)
        {
            var distance = CalculateDistance(lat, lng, stall.Latitude, stall.Longitude);
            if (distance <= stall.RadiusMeters)
            {
                // Đã kích hoạt stall này rồi → bỏ qua
                if (_geofenceTriggeredStallId == stall.StallId) return;

                // Audio đang phát → không ngắt
                if (_audioGuideService.IsPlaying) return;

                _geofenceTriggeredStallId = stall.StallId;
                SelectedStall = stall;

                if (!string.IsNullOrWhiteSpace(stall.AudioUrl))
                    await _audioGuideService.PlayAsync(stall.AudioUrl);

                return; // chỉ kích hoạt 1 stall gần nhất
            }
        }

        // Không còn trong geofence nào — reset để lần sau vào lại sẽ phát lại
        if (_geofenceTriggeredStallId.HasValue)
        {
            _geofenceTriggeredStallId = null;
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
