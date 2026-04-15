using Microsoft.Maui.ApplicationModel;
using Mobile.Helpers;
using Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    // Biến để chặn scan lặp nhiều lần khi camera detect liên tục.
    private bool isScanning = true;
    private bool _isTorchOn;

    private readonly ScanViewModel _viewModel;

    public ScanPage()
    {
        InitializeComponent();

        _viewModel = ServiceHelper.GetService<ScanViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // OLD CODE (kept for reference): auto skip ScanPage nếu đã có stallId local.
        // var hasStall = await LocalStorageService.HasStall();
        // if (hasStall)
        // {
        //     var savedStallId = await LocalStorageService.GetStallId();
        //     if (!string.IsNullOrWhiteSpace(savedStallId))
        //     {
        //         Console.WriteLine($"[DEBUG] Navigating to LanguagePage with StallId: {savedStallId}");
        //         await Shell.Current.GoToAsync($"//LanguagePage?stallId={Uri.EscapeDataString(savedStallId)}");
        //         return;
        //     }
        // }

        // Reset trạng thái scan mỗi lần vào trang để user có thể quét lại.
        isScanning = true;
        _viewModel.ResetScanner();
        await EnsureCameraPermissionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Dừng camera detect khi rời trang để tránh callback thừa.
        cameraView.IsDetecting = false;
    }

    private async Task EnsureCameraPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            return;
        }

        // OLD CODE (kept for reference): cấu hình scanner cũ.
        cameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };

        cameraView.IsDetecting = true;
    }

    // Xử lý khi camera phát hiện QR.
    private void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (!isScanning)
        {
            return;
        }

        var result = e.Results.FirstOrDefault();
        if (result == null)
        {
            return;
        }

        var value = result.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        isScanning = false;
        cameraView.IsDetecting = false;

        // Route qua ViewModel để verify QR với API trước khi điều hướng.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _viewModel.ScanResultCommand.Execute(value);
        });
    }

    private async void OnBackClicked(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnFlashClicked(object? sender, TappedEventArgs e)
    {
        _isTorchOn = !_isTorchOn;
        cameraView.IsTorchOn = _isTorchOn;
    }
}
