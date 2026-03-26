using Microsoft.Maui.ApplicationModel;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
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

        // Khởi tạo Mapsui với OpenStreetMap. 
        // Lấy tile source từ OpenStreetMap mà không cần API Key như Google Maps
        mapView.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
        
        // Cấu hình sự kiện click pin trên MapView
        mapView.PinClicked += async (sender, args) =>
        {
            if (args.Pin?.Tag is Stall stall)
            {
                args.Handled = true;
                await OnPinClickedAsync(stall);
            }
        };
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
        
        // Mặc định trung tâm bản đồ về TP.HCM nếu không định vị được hoặc chưa có booth focus
        // Resolution 19 tương đương với mức zoom ~13 trong hệ tọa độ của OpenStreetMap
        var defaultCenter = SphericalMercator.FromLonLat(106.660172, 10.762622);
        var centerPoint = new MPoint(defaultCenter.x, defaultCenter.y);
        mapView.Map?.Navigator.CenterOnAndZoomTo(centerPoint, 19, 0);

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
                var userLocation = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                var userPoint = new MPoint(userLocation.x, userLocation.y);
                
                // Mức độ phân giải (resolution) là 5, zoom cận hơn để thấy vị trí user (~Zoom level 15)
                mapView.Map?.Navigator.CenterOnAndZoomTo(userPoint, 5, 500);

                // Thêm pin vị trí hiện tại
                var myPin = new Pin()
                {
                    Label = "Bạn đang ở đây",
                    Position = new Position(location.Latitude, location.Longitude),
                    Color = Microsoft.Maui.Graphics.Colors.Green
                };
                mapView.Pins.Add(myPin);
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
        // Giữ lại pin vị trí hiện tại (màu xanh lá)
        var myLocationPin = mapView.Pins.FirstOrDefault(p => p.Label == "Bạn đang ở đây");
        mapView.Pins.Clear();
        
        if (myLocationPin != null)
        {
            mapView.Pins.Add(myLocationPin);
        }

        foreach (var stall in _viewModel.Stalls)
        {
            var isSelected = _viewModel.SelectedStall?.Id == stall.Id;
            var pin = new Pin()
            {
                Label = stall.Name,
                Address = isSelected ? "Đang chọn" : "Gian hàng",
                Position = new Position(stall.Latitude, stall.Longitude),
                Color = isSelected ? Microsoft.Maui.Graphics.Colors.Red : Microsoft.Maui.Graphics.Colors.Blue,
                Tag = stall // Gắn Stall object vào Tag để nhận diện stall khi pin được click
            };

            mapView.Pins.Add(pin);
        }
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
        // Đổi tọa độ địa lý sang SphericalMercator để tương thích với Mapsui
        var location = SphericalMercator.FromLonLat(stall.Longitude, stall.Latitude);
        var centerPoint = new MPoint(location.x, location.y);
        
        // Resolution = 2, zoom rat sat vao gian hang, thoi gian animation chuyen man hinh la 500ms
        mapView.Map?.Navigator.CenterOnAndZoomTo(centerPoint, 2, 500); 
        RenderPins();
    }
}
