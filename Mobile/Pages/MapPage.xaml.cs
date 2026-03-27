using Microsoft.Maui.ApplicationModel;
// OLD CODE (kept for reference): using Mapsui;
// OLD CODE (kept for reference): using Mapsui.Extensions;
// OLD CODE (kept for reference): using Mapsui.Projections;
// OLD CODE (kept for reference): using Mapsui.Tiling;
// OLD CODE (kept for reference): using Mapsui.UI.Maui;
using Mobile.Helpers;
using Mobile.Models;
using Mobile.ViewModels;

namespace Mobile.Pages;

[QueryProperty(nameof(BoothId), "boothId")]
public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private bool _isInitialized;

    public string? BoothId { get; set; }

    public MapPage()
    {
        InitializeComponent();

        _viewModel = ServiceHelper.GetService<MapViewModel>();
        BindingContext = _viewModel;

        _viewModel.FocusStallRequested += OnFocusStallRequested;
        _viewModel.PinsRefreshRequested += RenderPins;

        // OLD CODE (kept for reference): Khởi tạo Mapsui + sự kiện pin click.
        // Bản net8 tạm ẩn MapView để đảm bảo project build/run ổn định trên Android.
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        await EnsureLocationPermissionAsync();
        
        // OLD CODE (kept for reference): set default center bằng Mapsui Navigator

        await MoveToCurrentLocationAsync();
        await _viewModel.InitializeAsync(BoothId);
        RenderPins();
    }

    private async Task EnsureLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
        {
            return;
        }

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("GPS", "Bạn chưa cấp quyền vị trí. Bản đồ vẫn chạy nhưng không thể định vị bạn.", "OK");
        }
    }

    private async Task MoveToCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                // OLD CODE (kept for reference): cập nhật Navigator/Pin trên Mapsui mapView.
                // Ghi chú: ở bản fallback net8, chỉ lấy vị trí để đảm bảo luồng xin quyền + GPS vẫn hoạt động.
            }
        }
        catch (FeatureNotEnabledException)
        {
            await DisplayAlert("GPS", "Vui lòng bật GPS để xem vị trí hiện tại.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("GPS", $"Không thể lấy vị trí: {ex.Message}", "OK");
        }
    }

    private void RenderPins()
    {
        // OLD CODE (kept for reference): render pin lên mapView.Pins bằng Mapsui.
        // Ở chế độ net8 fallback, danh sách gian hàng vẫn hiển thị qua CollectionView (Binding Stalls).
    }

    private async Task OnPinClickedAsync(Stall stall)
    {
        _viewModel.SelectStall(stall);

        // Hiển thị Popup khi nhấn vào pin
        var action = await DisplayActionSheet(
            stall.Name,
            "Đóng",
            null,
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
            await DisplayAlert("Chi tiết gian hàng", $"Tên: {stall.Name}\nID: {stall.Id}\nAudio: {stall.AudioUrl}", "OK");
        }
    }

    private void OnFocusStallRequested(Stall stall)
    {
        // OLD CODE (kept for reference): focus stall bằng Navigator.CenterOnAndZoomTo của Mapsui.
        // Bản fallback net8: vẫn refresh danh sách để người dùng thấy gian hàng đang chọn.
        RenderPins();
    }
}
