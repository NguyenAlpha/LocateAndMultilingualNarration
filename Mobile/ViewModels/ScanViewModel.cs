using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private readonly SessionService _sessionService;
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

    public ScanViewModel(SessionService sessionService, ILogger<ScanViewModel> logger)
    {
        _sessionService = sessionService;
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

            // Giữ nguyên hành vi guest mode của logic cũ.
            _sessionService.SetGuestMode(true);

            var stallId = ExtractStallId(result);
            var route = BuildLanguageRoute(stallId, result);

            await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current.GoToAsync(route));
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

    private static string BuildLanguageRoute(string? stallId, string rawToken)
    {
        var route = nameof(LanguagePage);

        if (!string.IsNullOrWhiteSpace(stallId))
        {
            route += $"?stallId={Uri.EscapeDataString(stallId)}";
        }
        else
        {
            route += $"?token={Uri.EscapeDataString(rawToken)}";
        }

        return route;
    }

    private static string? ExtractStallId(string result)
    {
        // OLD CODE (kept for reference): logic tách boothId đã từng nằm trong ScanPage.xaml.cs.
        if (Guid.TryParse(result, out var guid))
            return guid.ToString();

        if (int.TryParse(result, out _))
            return result;

        var queryMatch = Regex.Match(result, @"(boothId|stallId)=(?<id>[^&\s]+)", RegexOptions.IgnoreCase);
        if (queryMatch.Success)
            return queryMatch.Groups["id"].Value;

        if (result.StartsWith("stall:", StringComparison.OrdinalIgnoreCase))
            return result.Split(':', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        return null;
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
