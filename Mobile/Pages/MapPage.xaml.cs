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
using Mobile.Helpers;
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

    // Cờ tránh chạy logic khởi tạo nhiều lần khi quay lại trang (OnAppearing gọi lại nhiều lần)
    private bool _isInitialized;

    // Cờ báo hiệu đang hiển thị popup — tránh StopPolling/StartPolling không cần thiết khi popup mở/đóng.
    private bool _isPopupOpen;

    // Lưu ngôn ngữ/voice lần trước để so sánh — chỉ reload khi thực sự thay đổi.
    private string? _lastLanguageCode;
    private string? _lastVoiceId;

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
    public MapPage()
    {
        InitializeComponent(); // Nạp MapPage.xaml

        // Lấy ViewModel từ DI container thay vì new trực tiếp (để inject đúng service)
        _viewModel = ServiceHelper.GetService<MapViewModel>();
        _logger = ServiceHelper.GetService<ILogger<MapPage>>();
        BindingContext = _viewModel; // Kết nối binding XAML với ViewModel

        // Lắng nghe event từ ViewModel để thực hiện thao tác trên MapView
        // (ViewModel không được giữ reference đến View, nên dùng event)
        _viewModel.FocusStallRequested += OnFocusStallRequested; // Di chuyển camera bản đồ
        _viewModel.PinsRefreshRequested += RenderPins;           // Vẽ lại toàn bộ pin
        _viewModel.LocationUpdated += OnLocationUpdated;         // Cập nhật pin vị trí người dùng


        // Thêm tile layer OSM (hình ảnh bản đồ nền từ OpenStreetMap)
        mapView.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
        // Thêm layer vòng tròn geofence (hiển thị phía trên tile layer)
        mapView.Map?.Layers.Add(_circlesLayer);

        // Ẩn widget debug log của Mapsui khỏi bản đồ (chỉ cần khi dev)
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
    }

    /// <summary>
    /// Chạy mỗi khi trang hiện ra (lần đầu và khi quay lại từ trang khác).
    /// Không async để tránh async void — delegate toàn bộ logic async sang InitializePageAsync.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("[MapPage][OnAppearing] — _isInitialized={IsInitialized}", _isInitialized);

        // Popup mở/đóng cũng trigger OnAppearing/OnDisappearing — bỏ qua để không restart polling thừa.
        if (_isPopupOpen)
        {
            _isPopupOpen = false;
            return;
        }

        _viewModel.StartPolling();
        _viewModel.SelectedStall = null;

        if (_isInitialized)
        {
            // Đọc ngôn ngữ/voice hiện tại từ LanguageHelper (được VoicePage ghi trước khi navigate).
            var currentLanguage = LanguageHelper.GetLanguage();
            var currentVoice    = LanguageHelper.GetVoice();

            // Chỉ reload khi ngôn ngữ hoặc voice thực sự thay đổi so với lần trước.
            var languageChanged = currentLanguage is not null && currentLanguage != _lastLanguageCode;
            var voiceChanged    = currentVoice is not null && currentVoice != _lastVoiceId;

            if (languageChanged || voiceChanged)
            {
                _lastLanguageCode = currentLanguage;
                _lastVoiceId      = currentVoice;
                _ = _viewModel.ReloadAsync();
            }

            return;
        }

        _isInitialized = true;
        _ = InitializePageAsync(); // fire-and-forget rõ ràng, exception được bắt bên trong
    }

    /// <summary>
    /// Chuỗi khởi tạo bất đồng bộ khi lần đầu vào trang.
    /// Tách ra khỏi OnAppearing để có thể bắt exception — async void không bắt được exception.
    /// </summary>
    private async Task InitializePageAsync()
    {
        try
        {
            // 1. Xin quyền GPS nếu chưa có
            await EnsureLocationPermissionAsync();

            // 2. Tải danh sách gian hàng từ API
            await _viewModel.InitializeAsync();

            // 3. Di chuyển camera đến vị trí người dùng, fallback về tọa độ trung tâm triển lãm nếu không lấy được GPS
            var located = await MoveToCurrentLocationAsync();
            if (!located)
            {
                var (x, y) = SphericalMercator.FromLonLat(106.710669, 10.777534);
                mapView.Map?.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 0.7, 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khởi tạo MapPage thất bại");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_isPopupOpen)
            return; // popup đang mở — không stop polling

        _viewModel.StopPolling();
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
            var popup = ServiceHelper.GetService<StallPopup>();
            if (popup is null)
            {
                _logger.LogError("[Popup] GetService<StallPopup> trả về NULL");
                return;
            }
            _logger.LogInformation("[Popup] GetService OK, gọi Init...");
            popup.Init(stall);
            _logger.LogInformation("[Popup] Gọi ShowPopupAsync...");
            _isPopupOpen = true;
            await this.ShowPopupAsync(popup);
            _isPopupOpen = false;
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
        _viewModel.StopPolling();
        await Shell.Current.GoToAsync("//MainPage");
    }
}
