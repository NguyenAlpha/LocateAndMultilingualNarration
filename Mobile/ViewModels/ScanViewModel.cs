using Microsoft.Extensions.Logging;
using Mobile.Services;
using SkiaSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ZXing;
using ZXing.Common;


namespace Mobile.ViewModels;

public class ScanViewModel : INotifyPropertyChanged
{
    private readonly IQrService _qrService;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<ScanViewModel> _logger;

    private int _navigationGuard;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ICommand ScanResultCommand { get; }
    public ICommand PickImageFromGalleryCommand { get; }

    public ScanViewModel(IQrService qrService, IDeviceService deviceService, ILogger<ScanViewModel> logger)
    {
        _qrService = qrService;
        _deviceService = deviceService;
        _logger = logger;

        ScanResultCommand = new Command<string>(async text => await HandleQrResultAsync(text));
        PickImageFromGalleryCommand = new Command(async () => await PickAndDecodeQrAsync());
    }

    public void ResetScanner()
    {
        IsDetecting = false;
        ErrorMessage = string.Empty;
        // OLD CODE: IsBusy = false; (không reset ở đây để tránh flash loading)
    }

    private async Task PickAndDecodeQrAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var fileResult = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Chọn ảnh chứa mã QR"
            });

            if (fileResult is null) return;

            await using var stream = await fileResult.OpenReadAsync();
            var decodedText = await DecodeQrFromImageAsync(stream);

            if (string.IsNullOrWhiteSpace(decodedText))
            {
                ErrorMessage = "Không tìm thấy mã QR trong ảnh.";
                await Shell.Current.DisplayAlertAsync("Thông báo", "Không tìm thấy mã QR trong ảnh đã chọn.", "OK");
                return;
            }

            IsBusy = false;
            await HandleQrResultAsync(decodedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi giải mã QR từ thư viện");
            ErrorMessage = "Không thể xử lý ảnh QR từ thư viện.";
            await Shell.Current.DisplayAlertAsync("Lỗi", "Ảnh không hợp lệ hoặc không thể giải mã QR.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<string?> DecodeQrFromImageAsync(Stream imageStream)
    {
        return await Task.Run(() =>
        {
            using var managed = new MemoryStream();
            imageStream.CopyTo(managed);
            managed.Position = 0;

            using var bitmap = SKBitmap.Decode(managed);
            if (bitmap is null) return null;

            var estimatedBytes = (long)bitmap.Width * bitmap.Height * 4;
            if (estimatedBytes > 128L * 1024L * 1024L)
                throw new InvalidOperationException("Ảnh quá lớn để giải mã QR an toàn.");

            var rawBytes = new byte[bitmap.Width * bitmap.Height * 4];
            var colors = bitmap.Pixels;
            for (var i = 0; i < colors.Length; i++)
            {
                var offset = i * 4;
                rawBytes[offset] = colors[i].Red;
                rawBytes[offset + 1] = colors[i].Green;
                rawBytes[offset + 2] = colors[i].Blue;
                rawBytes[offset + 3] = colors[i].Alpha;
            }

            var luminance = new RGBLuminanceSource(rawBytes, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminance));

            var reader = new MultiFormatReader();
            var hints = new Dictionary<DecodeHintType, object>
            {
                { DecodeHintType.TRY_HARDER, true },
                { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } }
            };

            return reader.decode(binaryBitmap, hints)?.Text;
        });
    }

    // ================== HÀM CHÍNH ĐÃ ĐƯỢC TỐI ƯU ==================
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
            if (Interlocked.CompareExchange(ref _navigationGuard, 1, 0) == 1) return;

            // FIX: Set IsBusy và tắt camera NGAY TỪ ĐẦU
            // OLD CODE: Chỉ set sau khi bắt đầu try
            IsBusy = true;
            IsDetecting = false;
            ErrorMessage = string.Empty;

            var deviceId = _deviceService.GetOrCreateDeviceId();

            // Gọi API verify
            var verifyResult = await _qrService.VerifyAsync(result, deviceId);

            if (verifyResult is null)
            {
                ErrorMessage = "Không thể kết nối máy chủ. Vui lòng thử lại.";
                IsDetecting = true;   // bật lại camera nếu lỗi
                return;
            }

            if (!verifyResult.IsValid)
            {
                ErrorMessage = verifyResult.Message ?? "Mã QR không hợp lệ hoặc đã hết hạn.";
                IsDetecting = true;
                return;
            }

            // Lưu quyền truy cập
            _qrService.SaveAccess(verifyResult.ExpiryAt);

            // Navigation
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // OLD CODE:
                // await Shell.Current.GoToAsync(nameof(LanguagePage));
                await Shell.Current.GoToAsync("LanguagePage");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xử lý kết quả QR thất bại");
            ErrorMessage = "Không thể xử lý mã QR. Vui lòng thử lại.";
            IsDetecting = true;
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _navigationGuard, 0);
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
