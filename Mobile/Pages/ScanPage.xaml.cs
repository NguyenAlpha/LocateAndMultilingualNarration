using Microsoft.Maui.ApplicationModel;
using Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    private bool _isTorchOn;
    private readonly ScanViewModel _viewModel;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        cameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ResetScanner();
        await EnsureCameraPermissionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.IsDetecting = false;
    }

    private async Task EnsureCameraPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            return;
        }

        _viewModel.IsDetecting = true;
    }

    private void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (!_viewModel.IsDetecting) return;

        var result = e.Results.FirstOrDefault();
        if (result == null || string.IsNullOrWhiteSpace(result.Value)) return;

        _viewModel.IsDetecting = false;
        _viewModel.IsBusy = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _viewModel.ScanResultCommand.Execute(result.Value);
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