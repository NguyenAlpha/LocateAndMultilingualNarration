using Microsoft.Maui.ApplicationModel;
using Mobile.Helpers;
using Mobile.Services;
using Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    // OLD CODE (kept for reference): readonly SessionService sessionService = new();
    readonly ScanViewModel _viewModel;
    // OLD CODE (kept for reference): bool isScanning;

    public ScanPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<ScanViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _viewModel.ResetScanner();
            await EnsureCameraPermissionAsync();
        }
        catch (Exception ex)
        {
            // OLD CODE (kept for reference): await DisplayAlert("Lỗi", $"Không thể khởi tạo trang quét QR: {ex.Message}", "OK");
            await DisplayAlertAsync("Lỗi", $"Không thể khởi tạo trang quét QR: {ex.Message}", "OK");
        }
    }

    async Task EnsureCameraPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            // OLD CODE (kept for reference): await DisplayAlert("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            await DisplayAlertAsync("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            return;
        }

        // OLD CODE (kept for reference): isScanning = true;
        // OLD CODE (kept for reference): cameraView.Options = new BarcodeReaderOptions { ... };
        // OLD CODE (kept for reference): cameraView.IsDetecting = true;
    }

    // OLD CODE (kept for reference): async void OnManualSubmitClicked(object? sender, EventArgs e)
    void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        try
        {
            if (!_viewModel.IsDetecting)
            {
                return;
            }

            var rawValue = e.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            // Đưa xử lý vào ViewModel để giữ đúng MVVM.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _viewModel.ScanResultCommand.Execute(rawValue);
            });
        }
        catch
        {
            // Nuốt lỗi callback để tránh crash thread camera; ViewModel sẽ xử lý trạng thái lỗi nếu có.
        }
    }
}
