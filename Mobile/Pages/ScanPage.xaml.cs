using Microsoft.Maui.ApplicationModel;
using Mobile.Services;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    readonly SessionService sessionService = new();
    bool isScanning;

    public ScanPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EnsureCameraPermissionAsync();
    }

    async Task EnsureCameraPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            return;
        }

        isScanning = true;
        cameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
        cameraView.IsDetecting = true;
    }

    void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!isScanning)
        {
            return;
        }

        var result = e.Results.FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        isScanning = false;
        cameraView.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            sessionService.SetGuestMode(true);
            await DisplayAlert("QR", $"Scanned: {result}", "OK");
            await Shell.Current.GoToAsync("//MainPage");
        });
    }
}
