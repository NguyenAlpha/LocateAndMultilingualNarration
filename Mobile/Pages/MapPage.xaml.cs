// Quyền vị trí và Geolocation API của MAUI
using Microsoft.Maui.ApplicationModel;
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
/// [QueryProperty] cho phép nhận tham số điều hướng từ Shell:
///   await Shell.Current.GoToAsync($"MapPage?boothId=B01")
///   → property BoothId sẽ được gán = "B01" trước khi OnAppearing chạy
/// </summary>
[QueryProperty(nameof(BoothId), "boothId")]
public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;

    // Cờ tránh chạy logic khởi tạo nhiều lần khi quay lại trang (OnAppearing gọi lại nhiều lần)
    private bool _isInitialized;

    // Layer riêng để vẽ vòng tròn geofence (bán kính phủ sóng) của từng gian hàng
    // Style = null để mỗi feature tự mang style riêng (màu khác nhau khi selected/unselected)
    private WritableLayer _circlesLayer = new WritableLayer { Name = "StallCircles", Style = null };

    // Nhận boothId từ query string khi điều hướng đến trang này (có thể null)
    public string? BoothId { get; set; }

    /// <summary>
    /// Constructor: khởi tạo UI, lấy ViewModel từ DI, đăng ký event, cấu hình bản đồ.
    /// </summary>
    public MapPage()
    {
        InitializeComponent(); // Nạp MapPage.xaml

        // Lấy ViewModel từ DI container thay vì new trực tiếp (để inject đúng service)
        _viewModel = ServiceHelper.GetService<MapViewModel>();
        BindingContext = _viewModel; // Kết nối binding XAML với ViewModel

        // Lắng nghe event từ ViewModel để thực hiện thao tác trên MapView
        // (ViewModel không được giữ reference đến View, nên dùng event)
        _viewModel.FocusStallRequested += OnFocusStallRequested; // Di chuyển camera bản đồ
        _viewModel.PinsRefreshRequested += RenderPins;           // Vẽ lại toàn bộ pin

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
    /// Dùng _isInitialized để chỉ chạy logic khởi tạo nặng một lần duy nhất.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Bắt đầu (hoặc khởi động lại) polling GPS mỗi khi trang hiện ra
        _viewModel.StartPolling();

        if (_isInitialized)
        {
            return; // Đã khởi tạo rồi, không làm gì thêm
        }

        _isInitialized = true;

        // 1. Xin quyền GPS nếu chưa có
        await EnsureLocationPermissionAsync();

        // 2. Di chuyển camera về vị trí mặc định (tọa độ trung tâm triển lãm)
        //    SphericalMercator.FromLonLat: chuyển GPS (lon, lat) → tọa độ bản đồ Mercator
        //    Resolution = 19: mức zoom gần (số nhỏ = zoom gần hơn trong Mapsui)
        //    Duration = 0: không có animation khi mới vào trang
        var defaultCenter = SphericalMercator.FromLonLat(106.710669, 10.777534);
        mapView.Map?.Navigator.CenterOnAndZoomTo(new MPoint(defaultCenter.x, defaultCenter.y), 19, 0);

        // 3. Lấy vị trí GPS thực tế của người dùng và đặt pin xanh "Bạn đang ở đây"
        await MoveToCurrentLocationAsync();

        // 4. Tải danh sách gian hàng từ API (và tự động chọn booth nếu có BoothId)
        await _viewModel.InitializeAsync(BoothId);

        // 5. Vẽ pin cho tất cả gian hàng lên bản đồ
        RenderPins();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopPolling();
    }

    /// <summary>
    /// Kiểm tra và xin quyền truy cập vị trí khi đang dùng app.
    /// Nếu từ chối, bản đồ vẫn hoạt động nhưng không hiển thị vị trí người dùng.
    /// </summary>
    private async Task EnsureLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
        {
            return; // Đã có quyền rồi
        }

        // Hiện dialog xin quyền của hệ điều hành
        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("GPS", "Bạn chưa cấp quyền vị trí. Bản đồ vẫn chạy nhưng không thể định vị bạn.", "OK");
        }
    }

    /// <summary>
    /// Lấy vị trí GPS hiện tại của người dùng, di chuyển camera đến đó
    /// và thêm pin xanh đánh dấu vị trí người dùng trên bản đồ.
    /// </summary>
    private async Task MoveToCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                return; // Không có quyền, bỏ qua
            }

            // Yêu cầu GPS với độ chính xác Medium, timeout 8 giây
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                // Chuyển tọa độ GPS sang tọa độ Mercator để dùng với Mapsui
                var userLocation = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);

                // Di chuyển camera đến vị trí người dùng
                // Resolution = 5: zoom gần hơn so với mặc định ban đầu
                // Duration = 500ms: animation mượt mà
                mapView.Map?.Navigator.CenterOnAndZoomTo(new MPoint(userLocation.x, userLocation.y), 5, 500);

                // Thêm pin xanh đánh dấu vị trí người dùng
                var myPin = new Pin()
                {
                    Label = "Bạn đang ở đây",
                    Position = new MauiPosition(location.Latitude, location.Longitude),
                    Color = Microsoft.Maui.Graphics.Colors.Green
                };
                mapView.Pins.Add(myPin);
            }
        }
        catch (FeatureNotEnabledException)
        {
            // GPS bị tắt trong cài đặt thiết bị
            await DisplayAlert("GPS", "Vui lòng bật GPS để xem vị trí hiện tại.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("GPS", $"Không thể lấy vị trí: {ex.Message}", "OK");
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
        var myLocationPin = mapView.Pins.FirstOrDefault(p => p.Label == "Bạn đang ở đây");
        mapView.Pins.Clear();

        // Khôi phục pin vị trí người dùng
        if (myLocationPin != null)
        {
            mapView.Pins.Add(myLocationPin);
        }

        // Thêm pin cho từng gian hàng
        foreach (var stall in _viewModel.Stalls)
        {
            var isSelected = _viewModel.SelectedStall?.StallId == stall.StallId;
            mapView.Pins.Add(new Pin()
            {
                Label = stall.StallName,
                Address = isSelected ? "Đang chọn" : "Gian hàng", // Hiển thị dưới label khi tap
                Position = new MauiPosition(stall.Latitude, stall.Longitude),
                Color = isSelected
                    ? Microsoft.Maui.Graphics.Colors.Red   // Gian hàng đang chọn → đỏ
                    : Microsoft.Maui.Graphics.Colors.Blue, // Gian hàng khác → xanh dương
                Tag = stall // Lưu object Stall vào Tag để dùng lại khi PinClicked
            });
        }

        // Vẽ lại vòng tròn geofence sau khi vẽ xong pin
        RenderCircles();
    }

    /// <summary>
    /// Vẽ vòng tròn geofence cho từng gian hàng có RadiusMeters > 0.
    /// Gian hàng đang chọn: vòng tròn đỏ đậm, alpha cao hơn.
    /// Gian hàng khác: vòng tròn xanh nhạt.
    /// </summary>
    private void RenderCircles()
    {
        _circlesLayer.Clear(); // Xóa vòng tròn cũ trước khi vẽ lại

        foreach (var stall in _viewModel.Stalls)
        {
            if (stall.RadiusMeters <= 0) continue; // Bỏ qua gian hàng không có geofence

            var isSelected = _viewModel.SelectedStall?.StallId == stall.StallId;

            // Tạo polygon hình tròn xấp xỉ bằng 64 đoạn thẳng
            var polygon = BuildCirclePolygon(stall.Latitude, stall.Longitude, stall.RadiusMeters);
            var feature = new GeometryFeature { Geometry = polygon };

            // Màu sắc khác nhau tùy trạng thái: đỏ nếu đang chọn, xanh nếu không
            var fillAlpha   = isSelected ? 70  : 40;  // Độ trong suốt của fill (0-255)
            var outlineWidth = isSelected ? 3.0 : 1.5; // Độ dày viền
            var r = isSelected ? 220 : 33;
            var g = isSelected ? 50  : 150;
            var b = isSelected ? 50  : 243;

            feature.Styles.Add(new VectorStyle
            {
                Fill    = new MapsuiBrush(new Mapsui.Styles.Color(r, g, b, fillAlpha)),
                Outline = new Pen(new Mapsui.Styles.Color(r, g, b, 200), outlineWidth)
            });

            _circlesLayer.Add(feature);
        }

        // Thông báo cho Mapsui biết layer đã thay đổi để re-render
        _circlesLayer.DataHasChanged();
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
            // Góc của điểm thứ i trên đường tròn (0 → 2π)
            var angle = 2 * Math.PI * i / segments;

            // Tọa độ GPS của điểm trên đường tròn
            var ptLat = lat + dLat * Math.Sin(angle);
            var ptLon = lon + dLon * Math.Cos(angle);

            // Chuyển từ GPS sang tọa độ Mercator để vẽ trên Mapsui
            var projected = SphericalMercator.FromLonLat(ptLon, ptLat);
            coords[i] = new Coordinate(projected.x, projected.y);
        }

        // Điểm cuối = điểm đầu để đóng polygon
        coords[segments] = coords[0];

        var factory = new GeometryFactory();
        return factory.CreatePolygon(factory.CreateLinearRing(coords));
    }

    /// <summary>
    /// Xử lý khi người dùng tap vào một pin gian hàng trên bản đồ.
    /// Chọn gian hàng đó trong ViewModel, rồi hiện action sheet với 3 tuỳ chọn.
    /// </summary>
    private async Task OnPinClickedAsync(GeoStallDto stall)
    {
        // Thông báo ViewModel gian hàng nào đang được chọn
        _viewModel.SelectStall(stall);

        // Hiện menu tuỳ chọn dạng bottom sheet
        var action = await DisplayActionSheet(
            stall.StallName,   // Tiêu đề
            "Đóng",       // Nút huỷ
            null,         // Nút destructive (không dùng)
            "Phát audio",
            "Xem chi tiết",
            "Dừng audio");

        if (action == "Phát audio")
        {
            _viewModel.PlayAudioCommand.Execute(null);
            return;
        }

        if (action == "Dừng audio")
        {
            _viewModel.StopAudioCommand.Execute(null);
            return;
        }

        if (action == "Xem chi tiết")
        {
            // Hiện thông tin chi tiết của gian hàng trong dialog
            await DisplayAlert("Chi tiết gian hàng",
                $"Tên: {stall.StallName}\nID: {stall.StallId}\nBán kính: {stall.RadiusMeters}m\nAudio: {stall.AudioUrl}",
                "OK");
        }
    }

    /// <summary>
    /// Handler cho event FocusStallRequested từ ViewModel.
    /// Di chuyển camera bản đồ đến vị trí gian hàng được chọn với animation 500ms.
    /// Resolution = 2: zoom rất gần để nhìn rõ gian hàng.
    /// </summary>
    private void OnFocusStallRequested(GeoStallDto stall)
    {
        var location = SphericalMercator.FromLonLat(stall.Longitude, stall.Latitude);
        mapView.Map?.Navigator.CenterOnAndZoomTo(new MPoint(location.x, location.y), 2, 500);

        // Vẽ lại pin để cập nhật màu pin đang chọn (đỏ) vs các pin còn lại (xanh)
        RenderPins();
    }
}
