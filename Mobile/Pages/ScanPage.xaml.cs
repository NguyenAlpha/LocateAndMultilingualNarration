using Microsoft.Maui.ApplicationModel;
using Mobile.Helpers;
using Mobile.Services;
using Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    // OLD CODE (kept for reference):
    // private bool _isScanning;
    // private bool _isTorchOn;

    // Biến để chặn scan lặp nhiều lần khi camera detect liên tục.
    private bool isScanning = true;
    private bool _isTorchOn;

    private readonly ScanViewModel _viewModel;
    private readonly SessionService _sessionService;

    public ScanPage()
    {
        InitializeComponent();

        // Giữ tương thích logic cũ: page vẫn bind ScanViewModel để dùng command chọn ảnh.
        _viewModel = ServiceHelper.GetService<ScanViewModel>();
        _sessionService = ServiceHelper.GetService<SessionService>();
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

        // Đưa về MainThread để thao tác UI/navigation an toàn.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await HandleQrSuccess(value);
        });
    }

    // Xử lý khi scan thành công: chuyển sang LanguagePage và truyền stallId.
    private async Task HandleQrSuccess(string qrValue)
    {
        try
        {
            // OLD CODE (kept for reference): kiểm tra định dạng GUID trước khi điều hướng.
            // if (!Guid.TryParse(value, out _))
            // {
            //     await DisplayAlertAsync("QR", "Mã QR không hợp lệ. Vui lòng quét lại.", "OK");
            //     isScanning = true;
            //     cameraView.IsDetecting = true;
            //     return;
            // }

            string stallId = qrValue;

            // QR hợp lệ theo flow hiện tại chứa stallId dạng GUID.
            if (!Guid.TryParse(stallId, out _))
            {
                await DisplayAlertAsync("QR", "Mã QR không hợp lệ. Vui lòng quét lại.", "OK");
                isScanning = true;
                cameraView.IsDetecting = true;
                return;
            }

            // Giữ logic guest mode cũ để tương thích flow hiện tại.
            _sessionService.SetGuestMode(true);

            // OLD CODE (kept for reference): LocalStorageService.SaveStallId(value);
            // Lưu stallId trước khi điều hướng để đảm bảo persistence sau khi restart app.
            await LocalStorageService.SaveStallId(stallId);

            Console.WriteLine($"[DEBUG] Navigating to LanguagePage after scan with StallId: {stallId}");
            await Shell.Current.GoToAsync($"//LanguagePage?stallId={Uri.EscapeDataString(stallId)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            // Nếu lỗi thì bật lại scan để người dùng quét lại.
            isScanning = true;
            cameraView.IsDetecting = true;
        }
    }

    // Nút back ở header.
    private async void OnBackClicked(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Nút flash ở header.
    private void OnFlashClicked(object? sender, TappedEventArgs e)
    {
        _isTorchOn = !_isTorchOn;
        cameraView.IsTorchOn = _isTorchOn;
    }
}
