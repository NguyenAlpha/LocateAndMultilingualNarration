using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Mobile.Pages;
using Mobile.Services;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Mobile.ViewModels;

public class ScanViewModel : INotifyPropertyChanged
{
    private readonly IQrService _qrService;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<ScanViewModel> _logger;

    // Guard chặn nhiều luồng quét đồng thời cùng gọi GoToAsync —
    // camera có thể detect liên tiếp nhiều frame trong 1 giây,
    // nếu không chặn sẽ push nhiều trang lên navigation stack và gây crash.
    private int _navigationGuard;

    public event PropertyChangedEventHandler? PropertyChanged;

    // true khi đang gọi API hoặc xử lý ảnh — dùng để disable nút và hiện spinner.
    bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    // Bật/tắt camera detect — tắt ngay khi bắt đầu xử lý QR để camera không
    // tiếp tục fire sự kiện OnQrDetected trong lúc đang navigate.
    bool _isDetecting = true;
    public bool IsDetecting
    {
        get => _isDetecting;
        set
        {
            if (_isDetecting == value) return;
            _isDetecting = value;
            OnPropertyChanged();
        }
    }

    // Thông báo lỗi hiển thị trực tiếp trên UI — rỗng nghĩa là không có lỗi.
    string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError)); // HasError phụ thuộc vào ErrorMessage
        }
    }

    // Computed property — XAML dùng để ẩn/hiện khung lỗi mà không cần converter.
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    // Lệnh nhận kết quả từ ZXing camera — ScanPage gọi khi camera detect được QR.
    public ICommand ScanResultCommand { get; }

    // Lệnh mở thư viện ảnh, giải mã QR từ ảnh rồi đưa vào flow verify như camera.
    public ICommand PickImageFromGalleryCommand { get; }

    public ScanViewModel(IQrService qrService, IDeviceService deviceService, ILogger<ScanViewModel> logger)
    {
        _qrService     = qrService;
        _deviceService = deviceService;
        _logger        = logger;

        ScanResultCommand           = new Command<string>(async text => await HandleQrResultAsync(text));
        PickImageFromGalleryCommand = new Command(async () => await PickAndDecodeQrAsync());
    }

    /// <summary>
    /// Gọi khi user quay lại ScanPage — reset camera và xóa lỗi cũ
    /// để tránh thấy trạng thái lỗi của lần quét trước.
    /// </summary>
    public void ResetScanner()
    {
        IsDetecting  = false; // camera tắt trong lúc EnsureCameraPermissionAsync chạy
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Mở MediaPicker để chọn ảnh, giải mã QR bằng ZXing, rồi chuyển sang flow verify.
    /// Dùng khi camera không tiếp cận được mã QR (in trên giấy, màn hình xa…).
    /// </summary>
    private async Task PickAndDecodeQrAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy        = true;
            ErrorMessage  = string.Empty;

            var fileResult = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Chọn ảnh chứa mã QR"
            });

            // User hủy picker — không làm gì thêm.
            if (fileResult is null) return;

            await using var stream = await fileResult.OpenReadAsync();
            var decodedText = await DecodeQrFromImageAsync(stream);

            if (string.IsNullOrWhiteSpace(decodedText))
            {
                ErrorMessage = "Không tìm thấy mã QR trong ảnh.";
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Shell.Current.DisplayAlertAsync("Thông báo", "Không tìm thấy mã QR trong ảnh đã chọn.", "OK"));
                return;
            }

            // Reset IsBusy trước khi gọi HandleQrResultAsync vì method đó có guard `if (IsBusy) return`.
            IsBusy = false;
            await HandleQrResultAsync(decodedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi giải mã QR từ thư viện");
            ErrorMessage = "Không thể xử lý ảnh QR từ thư viện.";
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Shell.Current.DisplayAlertAsync("Lỗi", "Ảnh không hợp lệ hoặc không thể giải mã QR.", "OK"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Giải mã QR từ stream ảnh bằng SkiaSharp + ZXing.
    /// Chạy trên background thread (Task.Run) vì decode ảnh lớn tốn CPU,
    /// không được chặn UI thread.
    /// </summary>
    private async Task<string?> DecodeQrFromImageAsync(Stream imageStream)
    {
        return await Task.Run(() =>
        {
            // Copy stream vào MemoryStream để đọc lại nhiều lần (stream gốc không seekable).
            using var managed = new MemoryStream();
            imageStream.CopyTo(managed);
            managed.Position = 0;

            using var bitmap = SKBitmap.Decode(managed);
            if (bitmap is null) return null;

            // Giới hạn ảnh 128 MB (width × height × 4 bytes/pixel) để tránh OOM
            // khi user chọn ảnh RAW hoặc ảnh panorama siêu lớn.
            var estimatedBytes = (long)bitmap.Width * bitmap.Height * 4;
            if (estimatedBytes > 128L * 1024L * 1024L)
                throw new InvalidOperationException("Ảnh quá lớn để giải mã QR an toàn.");

            // Chuyển pixel SKColor → mảng byte RGBA thô mà ZXing RGBLuminanceSource cần.
            var rawBytes = new byte[bitmap.Width * bitmap.Height * 4];
            var colors   = bitmap.Pixels;
            for (var i = 0; i < colors.Length; i++)
            {
                var offset = i * 4;
                rawBytes[offset]     = colors[i].Red;
                rawBytes[offset + 1] = colors[i].Green;
                rawBytes[offset + 2] = colors[i].Blue;
                rawBytes[offset + 3] = colors[i].Alpha;
            }

            // ZXing pipeline: raw bytes → luminance → binary → decode
            var luminance    = new RGBLuminanceSource(rawBytes, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminance));

            var reader = new MultiFormatReader();
            var hints  = new Dictionary<DecodeHintType, object>
            {
                { DecodeHintType.TRY_HARDER, true }, // thử nhiều góc xoay, scale hơn — chậm hơn nhưng chính xác hơn
                { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } }
            };

            return reader.decode(binaryBitmap, hints)?.Text;
        });
    }

    /// <summary>
    /// Điểm xử lý trung tâm — nhận chuỗi QR từ bất kỳ nguồn nào (camera, nhập tay, ảnh),
    /// gọi QrService verify, lưu quyền truy cập nếu hợp lệ, rồi điều hướng.
    /// </summary>
    private async Task HandleQrResultAsync(string? result)
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(result))
        {
            ErrorMessage = "Mã QR không hợp lệ hoặc trống.";
            return;
        }

        try
        {
            // Dùng Interlocked thay lock vì method này async — lock thông thường không hoạt động đúng với await.
            // So sánh-và-đổi: nếu _navigationGuard đang là 0 thì đổi thành 1 và tiếp tục,
            // ngược lại (đã là 1) nghĩa là luồng khác đang xử lý → thoát ngay.
            if (Interlocked.CompareExchange(ref _navigationGuard, 1, 0) == 1) return;

            IsBusy       = true;
            ErrorMessage = string.Empty;
            IsDetecting  = false; // dừng camera ngay để không fire thêm sự kiện trong lúc đang xử lý

            var deviceId     = _deviceService.GetOrCreateDeviceId();
            var verifyResult = await _qrService.VerifyAsync(result, deviceId);

            // null = không kết nối được server (timeout, network lỗi…)
            if (verifyResult is null)
            {
                ErrorMessage = "Không thể kết nối máy chủ. Vui lòng thử lại.";
                IsDetecting  = true;
                return;
            }

            // IsValid = false = QR đã dùng, hết hạn, hoặc không tồn tại
            if (!verifyResult.IsValid)
            {
                ErrorMessage = verifyResult.Message;
                IsDetecting  = true;
                return;
            }

            // Lưu vào Preferences — giúp LoadingPage skip ScanPage khi mở lại app
            // miễn là QR chưa hết hạn (kiểm tra bằng expiryAt > UtcNow).
            _qrService.SaveAccess(verifyResult.ExpiryAt);

            // OLD CODE (kept for reference):
            // await MainThread.InvokeOnMainThreadAsync(async () =>
            //     await Shell.Current.GoToAsync(nameof(LanguagePage)));
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Shell.Current.GoToAsync("LanguagePage"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xử lý kết quả QR thất bại");
            ErrorMessage = "Không thể xử lý mã QR. Vui lòng thử lại.";
            IsDetecting  = true;
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _navigationGuard, 0); // mở khóa để lần quét tiếp theo có thể vào
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
