using System.Text.RegularExpressions;
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
            await DisplayAlertAsync("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
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

            var boothId = ExtractBoothId(result);
            if (!string.IsNullOrWhiteSpace(boothId))
            {
                await Shell.Current.GoToAsync($"{nameof(MapPage)}?boothId={Uri.EscapeDataString(boothId)}");
                return;
            }

            await DisplayAlertAsync("QR", $"Scanned: {result}", "OK");
            await Shell.Current.GoToAsync(nameof(MapPage));
        });
    }

    static string? ExtractBoothId(string result)
    {
        if (int.TryParse(result, out _))
        {
            return result;
        }

        var q = Regex.Match(result, @"boothId=(?<id>[^&\s]+)", RegexOptions.IgnoreCase);
        if (q.Success)
        {
            return q.Groups["id"].Value;
        }

        if (result.StartsWith("stall:", StringComparison.OrdinalIgnoreCase))
        {
            return result.Split(':', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        return null;
    }
}
