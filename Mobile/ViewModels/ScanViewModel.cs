using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Mobile.Pages;
using Mobile.Services;
using SkiaSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
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

        ScanResultCommand = new Command<string>(async text => await ExecuteScanResultAsync(text));
        PickImageFromGalleryCommand = new Command(async () => await PickAndDecodeQrAsync());
    }

    public void ResetScanner()
    {
        IsDetecting = false;
        ErrorMessage = string.Empty;
        IsBusy = false;
    }

    // ====================== COMMAND WRAPPER - TỐI ƯU & AN TOÀN ======================
    private async Task ExecuteScanResultAsync(string? qrText)
    {
        if (string.IsNullOrWhiteSpace(qrText))
        {
            ErrorMessage = "Mã QR không hợp lệ hoặc trống.";
            return;
        }

        // Guard chống lặp
        if (Interlocked.CompareExchange(ref _navigationGuard, 1, 0) == 1)
            return;

        try
        {
            IsBusy = true;
            IsDetecting = false;
            ErrorMessage = string.Empty;

            _logger.LogInformation("[SCAN] Bắt đầu xử lý QR. Độ dài: {Length}", qrText.Length);

            var deviceId = _deviceService.GetOrCreateDeviceId();

            // === TỐI ƯU QUAN TRỌNG: Timeout cứng 5 giây ===
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            _logger.LogInformation("[SCAN] Đang gọi VerifyAsync...");

            var verifyResult = await _qrService.VerifyAsync(qrText, deviceId)
                .WaitAsync(cts.Token);

            _logger.LogInformation("[SCAN] VerifyAsync trả về. IsValid = {IsValid}", verifyResult?.IsValid ?? false);

            if (verifyResult is null)
            {
                ErrorMessage = "Không thể kết nối máy chủ. Vui lòng kiểm tra mạng.";
                return;
            }

            if (!verifyResult.IsValid)
            {
                ErrorMessage = verifyResult.Message ?? "Mã QR không hợp lệ hoặc đã hết hạn.";
                return;
            }

            _logger.LogInformation("[SCAN] QR xác thực thành công → Lưu access");

            _qrService.SaveAccess(verifyResult.ExpiryAt);

            // Navigate
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _logger.LogInformation("[SCAN] Navigate đến LanguagePage");
                await Shell.Current.GoToAsync(nameof(LanguagePage));
            });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("[SCAN] Verify QR timeout sau 5 giây");
            ErrorMessage = "Xác thực quá chậm (5s). Vui lòng kiểm tra mạng và thử lại.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SCAN] Lỗi khi xử lý QR");
            ErrorMessage = "Không thể xử lý mã QR. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _navigationGuard, 0);

            if (HasError)
            {
                await Task.Delay(800);   // Đợi một chút trước khi bật lại camera
                IsDetecting = true;
            }
        }
    }

    // ====================== GALLERY (Giữ nguyên nhưng tối ưu nhẹ) ======================
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

            await ExecuteScanResultAsync(decodedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi giải mã từ gallery");
            ErrorMessage = "Không thể xử lý ảnh QR từ thư viện.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ====================== DECODE ẢNH - TỐI ƯU TỐC ĐỘ ======================
    private async Task<string?> DecodeQrFromImageAsync(Stream imageStream)
    {
        return await Task.Run(() =>
        {
            using var managed = new MemoryStream();
            imageStream.CopyTo(managed);
            managed.Position = 0;

            using var bitmap = SKBitmap.Decode(managed);
            if (bitmap is null) return null;

            var luminance = new RGBLuminanceSource(
                // raw bytes...
                GetRawBytes(bitmap),
                bitmap.Width,
                bitmap.Height,
                RGBLuminanceSource.BitmapFormat.RGBA32);

            var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminance));

            var reader = new MultiFormatReader();
            var hints = new Dictionary<DecodeHintType, object>
            {
                { DecodeHintType.TRY_HARDER, false },           // TẮT để nhanh
                { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } }
            };

            return reader.decode(binaryBitmap, hints)?.Text;
        });
    }

    // Helper nhỏ để decode nhanh hơn
    private byte[] GetRawBytes(SKBitmap bitmap)
    {
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
        return rawBytes;
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}