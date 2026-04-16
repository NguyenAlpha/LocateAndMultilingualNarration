using Microsoft.Maui.ApplicationModel;
using Mobile.Helpers;
using Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Mobile.Pages;

public partial class ScanPage : ContentPage
{
    // Trạng thái torch lưu ở View vì camera API của ZXing không expose getter.
    private bool _isTorchOn;

    private readonly ScanViewModel _viewModel;

    public ScanPage()
    {
        InitializeComponent();

        // Lấy ViewModel từ DI thay vì new() để đảm bảo các service được inject đúng.
        _viewModel = ServiceHelper.GetService<ScanViewModel>();
        BindingContext = _viewModel;

        // Set options một lần duy nhất — không thay đổi trong suốt vòng đời trang.
        // Chỉ nhận mã 2D (QR, DataMatrix…) để giảm tải CPU.
        // AutoRotate = true giúp nhận QR khi điện thoại nằm ngang.
        // Multiple = false dừng sau khi decode được 1 mã, tránh fire nhiều lần.
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

        // Reset ViewModel state mỗi lần trang hiện lên.
        // Cần thiết khi user quay lại từ LanguagePage (back navigation).
        _viewModel.ResetScanner();

        // Hỏi quyền camera tại đây (không phải trong constructor) vì
        // dialog permission cần UI đang hiển thị mới hoạt động.
        await EnsureCameraPermissionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Tắt camera qua ViewModel — binding đồng bộ xuống cameraView,
        // tránh callback OnQrDetected fire sau khi trang đã bị pop khỏi stack.
        _viewModel.IsDetecting = false;
    }

    private async Task EnsureCameraPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            // Không có quyền → không khởi động camera, hiển thị lỗi và dừng.
            await DisplayAlertAsync("Lỗi", "Bạn cần cấp quyền camera để quét QR.", "OK");
            return;
        }

        // Bật camera qua ViewModel — binding sẽ đồng bộ sang cameraView.IsDetecting.
        _viewModel.IsDetecting = true;
    }

    // Callback từ ZXing khi camera nhận diện được barcode.
    // Chạy trên background thread của ZXing → phải dùng MainThread khi tương tác UI.
    private void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Dùng ViewModel.IsDetecting làm guard — ViewModel set false ngay khi bắt đầu
        // xử lý, ngăn các frame tiếp theo qua được.
        // _navigationGuard trong ViewModel bảo vệ thêm lần nữa nếu nhiều frame
        // cùng pass guard trước khi main thread kịp xử lý dispatch đầu tiên.
        if (!_viewModel.IsDetecting) return;

        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        var value = result.Value;
        if (string.IsNullOrWhiteSpace(value)) return;

        // Chuyển sang UI thread — GoToAsync và các thao tác
        // navigation bắt buộc phải chạy trên main thread.
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
        // Toggle torch — ZXing không có getter trạng thái nên tự theo dõi bằng _isTorchOn.
        _isTorchOn = !_isTorchOn;
        cameraView.IsTorchOn = _isTorchOn;
    }
}
