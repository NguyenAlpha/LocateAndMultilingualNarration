// Thư viện bản đồ Mapsui — các kiểu dữ liệu cốt lõi (Map, MPoint...)
using Mapsui;
// Layer có thể ghi dữ liệu động lên bản đồ (dùng cho vòng tròn geofence)
using Mapsui.Layers;
// Hỗ trợ geometry dạng NTS (NetTopologySuite) để vẽ polygon trên bản đồ
using Mapsui.Nts;
// Chuyển đổi tọa độ GPS (lon/lat) sang tọa độ bản đồ (SphericalMercator)
using Mapsui.Projections;
// Style cho vector (màu fill, outline, brush...)
using Mapsui.Styles;
// Tạo tile layer OpenStreetMap
using Mapsui.Tiling;
// MapView control và Pin cho MAUI
using Mapsui.UI.Maui;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Extensions.Logging;
using Mobile.Services;
using Mobile.ViewModels;
using Shared.DTOs.Geo;
// Geometry của NTS — tạo polygon, coordinate, linear ring
using NetTopologySuite.Geometries;
// Alias tránh xung đột tên: Mapsui.Styles.Brush vs MAUI Brush
using MapsuiBrush = Mapsui.Styles.Brush;
// Alias tránh xung đột: Mapsui.UI.Maui.Position vs Microsoft.Maui.Devices.Sensors.Location
using MauiPosition = Mapsui.UI.Maui.Position;
// Alias rõ ràng cho kiểu Polygon của NTS
using NtsPolygon = NetTopologySuite.Geometries.Polygon;

namespace Mobile.Pages;

/// <summary>
/// Code-behind của MapPage.
/// Chịu trách nhiệm về các tác vụ UI thuần túy mà ViewModel không được chạm vào:
///   - Khởi tạo và cấu hình MapView (tile layer, circle layer)
///   - Vẽ/xóa Pin và vòng tròn geofence trên bản đồ
///   - Xử lý sự kiện tap vào Pin → hiện action sheet
///   - Yêu cầu quyền GPS và lấy vị trí hiện tại
///
/// </summary>
public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private readonly ILogger<MapPage> _logger;
    private readonly StallPopup _stallPopup;
    private readonly ISyncBackgroundService _syncBackgroundService;
    private readonly IGpsPollingService _gpsPollingService;
    private readonly ILocationLogService _locationLogService;
    private readonly ILocalPreferenceService _localPreference;

    // Depth = số lần Appearing chưa kết đôi với Disappearing. Init chạy khi 0→1.
    // Popup cũng trigger Disappearing/Appearing — depth lặp về 1, EnsureReadyAsync no-op vì State=Ready.
    private int _appearingDepth;

    // Hash của preference lần gần nhất — đổi ngôn ngữ/voice = hash đổi = force reload.
    private string? _lastPrefHash;

    // Layer riêng để vẽ vòng tròn geofence (bán kính phủ sóng) của từng gian hàng
    // Style = null để mỗi feature tự mang style riêng (màu khác nhau khi selected/unselected)
    private readonly WritableLayer _circlesLayer = new() { Name = "StallCircles", Style = null };

    // Tái sử dụng factory thay vì new mỗi lần gọi BuildCirclePolygon
    private static readonly GeometryFactory GeomFactory = new();

    // Label của pin vị trí người dùng — dùng để nhận dạng khi RenderPins cần giữ lại pin này
    private const string MyLocationLabel = "Bạn đang ở đây";


    /// <summary>
    /// Constructor: khởi tạo UI, lấy ViewModel từ DI, đăng ký event, cấu hình bản đồ.
    /// </summary>
    public MapPage(
        MapViewModel viewModel,
        ILogger<MapPage> logger,
        StallPopup stallPopup,
        ISyncBackgroundService syncBackgroundService,
        IGpsPollingService gpsPollingService,
        ILocationLogService locationLogService,
        ILocalPreferenceService localPreference)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _logger = logger;
        _stallPopup = stallPopup;
        _syncBackgroundService = syncBackgroundService;
        _gpsPollingService = gpsPollingService;
        _locationLogService = locationLogService;
        _localPreference = localPreference;
        BindingContext = _viewModel;

        // Lắng nghe event từ ViewModel để thực hiện thao tác trên MapView
        // (ViewModel không được giữ reference đến View, nên dùng event)
        _viewModel.FocusStallRequested += OnFocusStallRequested; // Di chuyển camera bản đồ
        _viewModel.PinsRefreshRequested += RenderPins;           // Vẽ lại toàn bộ pin
        _viewModel.LocationUpdated += OnLocationUpdated;         // Cập nhật pin vị trí người dùng


        // Thêm tile layer OSM (hình ảnh bản đồ nền từ OpenStreetMap)
        mapView.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
        // Thêm layer vòng tròn geofence (hiển thị phía trên tile layer)
        mapView.Map?.Layers.Add(_circlesLayer);

        // Ẩn widget debug log và performance overlay của Mapsui
        Mapsui.Widgets.InfoWidgets.LoggingWidget.ShowLoggingInMap = Mapsui.Widgets.ActiveMode.No;

        // Đăng ký sự kiện tap vào pin trên bản đồ
        // args.Pin.Tag được gán = Stall object khi tạo pin trong RenderPins()
        mapView.PinClicked += async (sender, args) =>
        {
            if (args.Pin?.Tag is GeoStallDto stall)
            {
                args.Handled = true; // Ngăn Mapsui xử lý mặc định (hiện label)
                await OnPinClickedAsync(stall);
            }
        };

        // Page Transient → cần dispose VM khi page rời visual tree để gỡ event trên Singleton service,
        // tránh leak + duplicate audio sau mỗi lần nav.
        Unloaded += OnPageUnloaded;
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        Unloaded -= OnPageUnloaded;
        _viewModel.FocusStallRequested -= OnFocusStallRequested;
        _viewModel.PinsRefreshRequested -= RenderPins;
        _viewModel.LocationUpdated -= OnLocationUpdated;
        _viewModel.Dispose();
    }

    /// <summary>
    /// OnAppearing fire cả khi nav tới và khi popup đóng. Dùng _appearingDepth để biết đây là
    /// lần đầu vào trang (0→1) hay chỉ là popup closing (cũng 0→1 sau khi Disappearing đã giảm về 0).
    /// Trong cả 2 trường hợp: start service + gọi EnsureReadyAsync (idempotent — no-op nếu State=Ready).
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _appearingDepth++;

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("[MapPage][OnAppearing] — depth={Depth}", _appearingDepth);

        _gpsPollingService.Start();
        _syncBackgroundService.Start();

        // Chỉ reset SelectedStall khi đây là lần đầu vào trang (depth=1 sau popup-close không cần).
        if (_appearingDepth == 1)
            _viewModel.SelectedStall = null;

        // Detect language/voice change qua hash — đổi thì force reload stall + audio.
        var pref = _localPreference.Load();
        var currentHash = $"{pref?.LanguageCode}|{pref?.VoiceId}";
        var prefChanged = _lastPrefHash is not null && _lastPrefHash != currentHash;
        _lastPrefHash = currentHash;

        _ = InitializePageAsync(forceReload: prefChanged);
    }

    /// <summary>
    /// Chuỗi khởi tạo bất đồng bộ. EnsureReadyAsync là idempotent — gọi lại an toàn, no-op nếu đã Ready.
    /// </summary>
    private async Task InitializePageAsync(bool forceReload)
    {
        try
        {
            await EnsureLocationPermissionAsync();

            await _viewModel.EnsureReadyAsync(forceReload);

            // Chỉ center camera lần đầu — nav-back hoặc popup-close không jump camera.
            if (_appearingDepth == 1 && !forceReload)
            {
                var located = await MoveToCurrentLocationAsync();
                if (!located)
                {
                    var (x, y) = SphericalMercator.FromLonLat(106.710669, 10.777534);
                    mapView.Map?.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 0.7, 0);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khởi tạo MapPage thất bại");
        }
    }

    /// <summary>
    /// Stop được gọi mỗi lần Disappearing — các service tự null-check để idempotent.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_appearingDepth > 0) _appearingDepth--;

        try
        {
            _gpsPollingService.Stop();
            _ = _locationLogService.FlushAsync();
            _syncBackgroundService.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Giải phóng tài nguyên MapPage thất bại trong OnDisappearing");
        }
    }

    /// <summary>
    /// Kiểm tra và xin quyền truy cập vị trí khi đang dùng app.
    /// Nếu từ chối, bản đồ vẫn hoạt động nhưng không hiển thị vị trí người dùng.
    /// </summary>
    private async Task EnsureLocationPermissionAsync()
    {
        // RequestAsync tự kiểm tra trước — nếu đã granted thì trả về ngay, không hiện dialog
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("GPS", "Bạn chưa cấp quyền vị trí. Bản đồ vẫn chạy nhưng không thể định vị bạn.", "OK");
        }
    }

    /// <summary>
    /// Lấy vị trí GPS hiện tại của người dùng, di chuyển camera đến đó
    /// và thêm pin xanh đánh dấu vị trí người dùng trên bản đồ.
    /// Trả về true nếu lấy được vị trí và đã di chuyển camera, false nếu thất bại.
    /// </summary>
    private async Task<bool> MoveToCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return false;

            // Yêu cầu GPS với độ chính xác Medium, timeout 8 giây
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is null)
                return false;

            // Chuyển tọa độ GPS sang tọa độ Mercator để dùng với Mapsui
            var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);

            // Di chuyển camera đến vị trí người dùng, Duration = 500ms: animation mượt mà
            mapView.Map?.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 0.7);

            // Thêm pin xanh đánh dấu vị trí người dùng
            mapView.Pins.Add(new Pin
            {
                Label = MyLocationLabel,
                Position = new MauiPosition(location.Latitude, location.Longitude),
                Color = Colors.Green
            });

            return true;
        }
        catch (FeatureNotEnabledException)
        {
            await DisplayAlertAsync("GPS", "Vui lòng bật GPS để xem vị trí hiện tại.", "OK");
            return false;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("GPS", $"Không thể lấy vị trí: {ex.Message}", "OK");
            return false;
        }
    }

    /// <summary>
    /// Vẽ lại toàn bộ pin gian hàng lên bản đồ.
    /// Giữ lại pin xanh "Bạn đang ở đây" trước khi xóa để không mất vị trí người dùng.
    /// Pin đang được chọn hiển thị màu đỏ, còn lại màu xanh dương.
    /// </summary>
    private void RenderPins()
    {
        try
        {
            // Lưu lại pin vị trí người dùng trước khi xóa toàn bộ
            var myLocationPin = mapView.Pins.FirstOrDefault(p => p.Label == MyLocationLabel);
            mapView.Pins.Clear();

            if (myLocationPin != null)
                mapView.Pins.Add(myLocationPin);

            foreach (var stall in _viewModel.Stalls)
            {
                var isSelected = _viewModel.SelectedStall?.StallId == stall.StallId;
                mapView.Pins.Add(new Pin
                {
                    Label = stall.StallName,
                    Address = isSelected ? "Đang chọn" : "Gian hàng",
                    Position = new MauiPosition(stall.Latitude, stall.Longitude),
                    Color = isSelected ? Colors.Red : Colors.Blue,
                    Tag = stall
                });
            }

            RenderCircles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RenderPins thất bại");
        }
    }

    /// <summary>
    /// Vẽ vòng tròn geofence cho từng gian hàng có RadiusMeters > 0.
    /// Gian hàng đang chọn: vòng tròn đỏ đậm, alpha cao hơn.
    /// Gian hàng khác: vòng tròn xanh nhạt.
    /// </summary>
    private void RenderCircles()
    {
        _circlesLayer.Clear();

        foreach (var stall in _viewModel.Stalls)
        {
            if (stall.RadiusMeters <= 0) continue;

            var isSelected = _viewModel.SelectedStall?.StallId == stall.StallId;
            var polygon = BuildCirclePolygon(stall.Latitude, stall.Longitude, stall.RadiusMeters);
            var feature = new GeometryFeature { Geometry = polygon };
            feature.Styles.Add(BuildCircleStyle(isSelected));
            _circlesLayer.Add(feature);
        }

        _circlesLayer.DataHasChanged();
    }

    // Đỏ nếu đang chọn, xanh nếu không — alpha và độ dày viền cũng khác nhau
    private static VectorStyle BuildCircleStyle(bool isSelected)
    {
        var color = isSelected
            ? new Mapsui.Styles.Color(220, 50, 50)
            : new Mapsui.Styles.Color(33, 150, 243);

        return new VectorStyle
        {
            Fill = new MapsuiBrush(color with { A = isSelected ? 70 : 40 }),
            Outline = new Pen(color with { A = 200 }, isSelected ? 3.0 : 1.5)
        };
    }

    /// <summary>
    /// Tạo polygon hình tròn xấp xỉ (dùng để vẽ geofence) từ tọa độ tâm và bán kính.
    /// Thuật toán: chia đường tròn thành N điểm đều nhau theo góc,
    /// tính tọa độ GPS từng điểm, rồi chuyển sang tọa độ Mercator.
    /// </summary>
    /// <param name="lat">Vĩ độ tâm (GPS)</param>
    /// <param name="lon">Kinh độ tâm (GPS)</param>
    /// <param name="radiusMeters">Bán kính tính bằng mét</param>
    /// <param name="segments">Số đoạn xấp xỉ — càng lớn càng tròn, mặc định 64</param>
    private static NtsPolygon BuildCirclePolygon(double lat, double lon, double radiusMeters, int segments = 64)
    {
        var latRad = lat * Math.PI / 180.0; // Chuyển vĩ độ sang radian để tính cos

        // Chuyển bán kính mét → độ kinh/vĩ
        // 111_000 mét ≈ 1 độ vĩ độ (hằng số địa lý gần đúng)
        var dLat = radiusMeters / 111_000.0;
        // Độ kinh độ thu hẹp lại theo cos(vĩ độ) vì các đường kinh hội tụ về cực
        var dLon = radiusMeters / (111_000.0 * Math.Cos(latRad));

        var coords = new Coordinate[segments + 1];
        for (int i = 0; i < segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var (x, y) = SphericalMercator.FromLonLat(lon + dLon * Math.Cos(angle), lat + dLat * Math.Sin(angle));
            coords[i] = new Coordinate(x, y);
        }

        // Điểm cuối = điểm đầu để đóng polygon
        coords[segments] = coords[0];

        return GeomFactory.CreatePolygon(GeomFactory.CreateLinearRing(coords));
    }

    /// <summary>
    /// Xử lý khi người dùng tap vào một pin gian hàng trên bản đồ.
    /// </summary>
    private async Task OnPinClickedAsync(GeoStallDto stall)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("[Popup] OnPinClickedAsync - StallId: {StallId}, NarrationContent={HasNarration}",
                stall.StallId, stall.NarrationContent != null);
        _viewModel.SelectStall(stall);

        try
        {
            _stallPopup.Init(stall);
            _logger.LogInformation("[Popup] Gọi ShowPopupAsync...");
            await this.ShowPopupAsync(_stallPopup);
            _logger.LogInformation("[Popup] ShowPopupAsync hoàn tất");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Popup] Lỗi khi hiện popup");
        }
    }

    /// <summary>
    /// Cập nhật vị trí pin "Bạn đang ở đây" mỗi khi polling GPS nhận được tọa độ mới.
    /// Chạy trên main thread vì thao tác với UI collection (mapView.Pins).
    /// </summary>
    private void OnLocationUpdated(double lat, double lng)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var pin = mapView.Pins.FirstOrDefault(p => p.Label == MyLocationLabel);
            if (pin is not null)
            {
                pin.Position = new MauiPosition(lat, lng);
            }
            else
            {
                mapView.Pins.Add(new Pin
                {
                    Label = MyLocationLabel,
                    Position = new MauiPosition(lat, lng),
                    Color = Colors.Green
                });
            }
        });
    }

    /// <summary>
    /// Handler cho event FocusStallRequested từ ViewModel.
    /// Di chuyển camera bản đồ đến vị trí gian hàng được chọn, giữ nguyên mức zoom hiện tại.
    /// </summary>
    private void OnFocusStallRequested(GeoStallDto stall)
    {
        var (x, y) = SphericalMercator.FromLonLat(stall.Longitude, stall.Latitude);
        var centerPoint = new MPoint(x, y);

        // Đọc resolution hiện tại của viewport — giữ nguyên zoom người dùng đang ở
        // Fallback về 0.7 nếu chưa có viewport (trường hợp hiếm khi map chưa render xong)
        var currentResolution = mapView.Map?.Navigator.Viewport.Resolution ?? 0.7;

        // CenterOnAndZoomTo với resolution hiện tại = chỉ pan, không zoom
        mapView.Map?.Navigator.CenterOnAndZoomTo(centerPoint, currentResolution, 500);

        // Vẽ lại pin để cập nhật màu pin đang chọn (đỏ) vs các pin còn lại (xanh)
        RenderPins();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        _gpsPollingService.Stop();
        await Shell.Current.GoToAsync("//MainPage");
    }
}
