using ZXing.Net.Maui;
using Mobile.ViewModels;
using Microsoft.Maui.ApplicationModel;
namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    bool isScanning = true;

    public ScanPage()
    {
        InitializeComponent();
        RequestCamera();
    }
    async void RequestCamera()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Error", "Camera permission denied", "OK");
        }
    }
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!isScanning) return;

        isScanning = false;

        var result = e.Results.FirstOrDefault()?.Value;

        if (result != null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 👉 Lưu guest login
                Preferences.Set("guest_mode", true);

                await DisplayAlert("QR", $"Scanned: {result}", "OK");

                // 👉 Chuyển sang MainPage
                await Shell.Current.GoToAsync("//MainPage");
            });
        }
    }
}