using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    private bool _isTorchOn;
    private readonly ScanViewModel _viewModel;
    private readonly ILogger<ScanPage> _logger;
    private bool _hasShownDetectAlertForDebug;

    // OLD CODE (kept for reference): public ScanPage(ScanViewModel viewModel)
    public ScanPage(ScanViewModel viewModel, ILogger<ScanPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = _viewModel;

        // Ghi log khởi tạo sớm để xác nhận trang được tạo đúng bằng DI trên thiết bị thật.
        _logger.LogInformation("[ScanPage] Constructor chạy. ThreadId={ThreadId}", Environment.CurrentManagedThreadId);
        Debug.WriteLine($"[ScanPage] Constructor chạy. ThreadId={Environment.CurrentManagedThreadId}");

        cameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            _logger.LogInformation("[ScanPage] OnAppearing bắt đầu");
            Debug.WriteLine("[ScanPage] OnAppearing bắt đầu");

            _viewModel.ResetScanner();
            await EnsureCameraPermissionAsync();

            _logger.LogInformation("[ScanPage] OnAppearing kết thúc. IsDetecting={IsDetecting}, IsBusy={IsBusy}", _viewModel.IsDetecting, _viewModel.IsBusy);
            Debug.WriteLine($"[ScanPage] OnAppearing kết thúc. IsDetecting={_viewModel.IsDetecting}, IsBusy={_viewModel.IsBusy}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScanPage] Lỗi nghiêm trọng trong OnAppearing");
            Debug.WriteLine($"[ScanPage] OnAppearing error: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _logger.LogInformation("[ScanPage] OnDisappearing. Tắt detect để tránh callback trễ.");
        Debug.WriteLine("[ScanPage] OnDisappearing. Tắt detect để tránh callback trễ.");
        _viewModel.IsDetecting = false;
    }

    private async Task EnsureCameraPermissionAsync()
    {
        _logger.LogInformation("[ScanPage] Yêu cầu quyền Camera...");
        Debug.WriteLine("[ScanPage] Yêu cầu quyền Camera...");

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        _logger.LogInformation("[ScanPage] Kết quả quyền Camera: {Status}", status);
        Debug.WriteLine($"[ScanPage] Kết quả quyền Camera: {status}");

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            return;
        }

        _viewModel.IsDetecting = true;
    }

    // OLD CODE (kept for reference): private void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    private void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Tách luồng xử lý async để có thể log/catch đầy đủ, tránh async void nuốt exception.
        _ = ProcessQrDetectedAsync(e);
    }

    private async Task ProcessQrDetectedAsync(BarcodeDetectionEventArgs e)
    {
        try
        {
            var resultsCount = e.Results?.Count() ?? 0;
            _logger.LogInformation("[ScanPage] OnQrDetected fired. IsDetecting={IsDetecting}, IsBusy={IsBusy}, ResultsCount={Count}", _viewModel.IsDetecting, _viewModel.IsBusy, resultsCount);
            Debug.WriteLine($"[ScanPage] OnQrDetected fired. IsDetecting={_viewModel.IsDetecting}, IsBusy={_viewModel.IsBusy}, ResultsCount={resultsCount}");

            if (!_viewModel.IsDetecting)
            {
                _logger.LogWarning("[ScanPage] Bỏ qua QR vì IsDetecting=false");
                Debug.WriteLine("[ScanPage] Bỏ qua QR vì IsDetecting=false");
                return;
            }

            var result = e.Results.FirstOrDefault();
            if (result == null || string.IsNullOrWhiteSpace(result.Value))
            {
                _logger.LogWarning("[ScanPage] Không có QR value hợp lệ trong callback");
                Debug.WriteLine("[ScanPage] Không có QR value hợp lệ trong callback");
                return;
            }

            _logger.LogInformation("[ScanPage] Đã detect QR: {Preview}", result.Value.Length > 60 ? result.Value[..60] + "..." : result.Value);
            Debug.WriteLine($"[ScanPage] Đã detect QR: {(result.Value.Length > 60 ? result.Value[..60] + "..." : result.Value)}");

            _viewModel.IsDetecting = false;
            // OLD CODE (kept for reference): _viewModel.IsBusy = true;
            // Không set IsBusy ở đây vì HandleQrResultAsync sẽ tự set; set sớm làm command bị skip ngay từ đầu.

#if DEBUG
            // Cơ chế test nhanh: xác nhận callback detect đã chạy trên thiết bị thật.
            if (!_hasShownDetectAlertForDebug)
            {
                _hasShownDetectAlertForDebug = true;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("DEBUG", "Đã detect QR và chuẩn bị chạy ScanResultCommand.", "OK");
                });
            }
#endif

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _logger.LogInformation("[ScanPage] Chuẩn bị execute ScanResultCommand. CanExecute={CanExecute}", _viewModel.ScanResultCommand.CanExecute(result.Value));
                Debug.WriteLine($"[ScanPage] Chuẩn bị execute ScanResultCommand. CanExecute={_viewModel.ScanResultCommand.CanExecute(result.Value)}");

                if (!_viewModel.ScanResultCommand.CanExecute(result.Value))
                {
                    _logger.LogWarning("[ScanPage] ScanResultCommand.CanExecute=false");
                    return;
                }

                _viewModel.ScanResultCommand.Execute(result.Value);
                _logger.LogInformation("[ScanPage] Đã Execute ScanResultCommand");
                Debug.WriteLine("[ScanPage] Đã Execute ScanResultCommand");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScanPage] Lỗi trong ProcessQrDetectedAsync");
            Debug.WriteLine($"[ScanPage] ProcessQrDetectedAsync error: {ex}");
            _viewModel.ErrorMessage = $"Lỗi detect QR trên thiết bị. [debug:scanpage-detect:{ex.GetType().Name}]";
            _viewModel.IsBusy = false;
            _viewModel.IsDetecting = true;
        }
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