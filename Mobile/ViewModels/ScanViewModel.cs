using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Mobile.Services;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Mobile.ViewModels;

public class ScanViewModel : INotifyPropertyChanged
{
    private readonly IQrSessionService _qrSessionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<ScanViewModel> _logger;
    private int _navigationGuard;
    private const string ApiBaseUrl = "http://10.0.2.2:5299";

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

    string _manualQrInput = string.Empty;
    public string ManualQrInput
    {
        get => _manualQrInput;
        set
        {
            if (_manualQrInput == value) return;
            _manualQrInput = value;
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
    public ICommand SubmitManualQrCommand { get; }
    public ICommand PickImageFromGalleryCommand { get; }

    public ScanViewModel(
        IQrSessionService qrSessionService,
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        ILogger<ScanViewModel> logger)
    {
        _qrSessionService = qrSessionService;
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _logger = logger;

        // Lệnh xử lý kết quả quét từ camera.
        ScanResultCommand = new Command<string>(async text => await HandleQrResultAsync(text));

        // Lệnh xử lý nhập tay (giữ lại flow cũ để không mất logic hiện tại).
        SubmitManualQrCommand = new Command(async () => await HandleQrResultAsync(ManualQrInput));

        // Lệnh mở thư viện và giải mã QR từ ảnh.
        PickImageFromGalleryCommand = new Command(async () => await PickAndDecodeQrAsync());
    }

    public void ResetScanner()
    {
        // Cho phép camera tiếp tục detect khi quay lại trang.
        IsDetecting = true;
        ErrorMessage = string.Empty;
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

            if (fileResult is null)
            {
                return;
            }

            await using var stream = await fileResult.OpenReadAsync();
            var decodedText = await DecodeQrFromImageAsync(stream);

            if (string.IsNullOrWhiteSpace(decodedText))
            {
                ErrorMessage = "Không tìm thấy mã QR trong ảnh.";
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Shell.Current.DisplayAlertAsync("Thông báo", "Không tìm thấy mã QR trong ảnh đã chọn.", "OK"));
                return;
            }

            // Kết thúc trạng thái xử lý bước decode trước khi chuyển sang flow điều hướng QR.
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

    private async Task<string?> DecodeQrFromImageAsync(Stream imageStream)
    {
        return await Task.Run(() =>
        {
            using var managed = new MemoryStream();
            imageStream.CopyTo(managed);
            managed.Position = 0;

            using var bitmap = SKBitmap.Decode(managed);
            if (bitmap is null)
            {
                return null;
            }

            // Profile dữ liệu lớn có thể kéo theo ảnh QR rất lớn; bọc kiểm tra để tránh cấp phát quá mức.
            var estimatedBytes = (long)bitmap.Width * bitmap.Height * 4;
            if (estimatedBytes > 128L * 1024L * 1024L)
            {
                throw new InvalidOperationException("Ảnh quá lớn để giải mã QR an toàn.");
            }

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

            var result = reader.decode(binaryBitmap, hints);
            return result?.Text;
        });
    }

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
            // Chặn nhiều luồng quét cùng điều hướng, nguyên nhân phổ biến gây crash loop khi camera detect liên tiếp.
            if (Interlocked.CompareExchange(ref _navigationGuard, 1, 0) == 1)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;
            IsDetecting = false;

            // Verify mã QR với API trước khi cho phép vào app.
            var verifyResult = await VerifyQrCodeAsync(result);
            if (verifyResult is null)
            {
                ErrorMessage = "Không thể kết nối máy chủ. Vui lòng thử lại.";
                IsDetecting = true;
                return;
            }

            if (!verifyResult.Value.isValid)
            {
                ErrorMessage = verifyResult.Value.message;
                IsDetecting = true;
                return;
            }

            _qrSessionService.SaveSession(verifyResult.Value.expiryAt);

            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Shell.Current.GoToAsync(nameof(LanguagePage)));
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

    private async Task<(bool isValid, string message, DateTime expiryAt)?> VerifyQrCodeAsync(string code)
    {
        try
        {
            var deviceId = _deviceService.GetOrCreateDeviceId();
            var client = _httpClientFactory.CreateClient("ApiHttp");
            var request = new QrCodeVerifyRequestDto { Code = code, DeviceId = deviceId };
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/qrcodes/verify", request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.GetProperty("success").GetBoolean()) return null;

            var data = root.GetProperty("data");
            var isValid = data.GetProperty("isValid").GetBoolean();
            var message = data.GetProperty("message").GetString() ?? string.Empty;
            var expiryAt = isValid && data.TryGetProperty("expiryAt", out var expiryProp)
                ? expiryProp.GetDateTime()
                : DateTime.MinValue;

            return (isValid, message, expiryAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi verify mã QR");
            return null;
        }
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
