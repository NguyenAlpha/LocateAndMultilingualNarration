using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.DTOs.QrCodes;

namespace Mobile.Services;

/// <summary>
/// Quản lý toàn bộ vòng đời QR access của app theo 4 bước:
///   1. VerifyAsync   — gọi API kiểm tra mã QR có hợp lệ không
///   2. SaveAccess    — lưu kết quả vào Preferences sau khi verify thành công
///   3. IsAccessValid — kiểm tra Preferences còn hạn không (dùng ở LoadingPage)
///   4. ClearAccess   — xoá Preferences, buộc user quét QR lại
///
/// Lý do gộp HTTP + Preferences vào cùng một service:
/// cả bốn bước đều thuộc một domain — "quản lý quyền vào app bằng QR",
/// gộp lại giúp ScanViewModel không cần biết về HTTP hay storage.
/// </summary>
public interface IQrService
{
    /// <summary>Gọi API verify mã QR. Trả về null nếu không kết nối được server.</summary>
    Task<QrVerifyResult?> VerifyAsync(string code, string deviceId);

    /// <summary>Lưu quyền truy cập vào Preferences sau khi verify thành công.</summary>
    void SaveAccess(DateTime expiryAt);

    /// <summary>Kiểm tra quyền truy cập còn hợp lệ hay không.</summary>
    bool IsAccessValid();

    /// <summary>Xoá quyền truy cập, buộc user quét QR lại.</summary>
    void ClearAccess();
}

/// <summary>
/// Kết quả trả về từ API verify QR.
/// Dùng record thay tuple để caller truy cập bằng tên (IsValid, Message, ExpiryAt)
/// thay vì vị trí (.Item1, .Item2...).
/// </summary>
public record QrVerifyResult(bool IsValid, string Message, DateTime ExpiryAt);

public class QrService : IQrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QrService> _logger;

    // Hai key lưu vào Preferences — tồn tại qua các lần tắt/mở app,
    // bị xoá khi user gỡ cài đặt app.
    private const string VerifiedKey = "qr_verified"; // bool   – đã quét QR thành công chưa
    private const string ExpiryKey   = "qr_expiry";   // string – thời điểm QR hết hạn (ISO-8601)

    public QrService(IHttpClientFactory httpClientFactory, ILogger<QrService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    /// <summary>
    /// Gửi mã QR và deviceId lên API để xác thực.
    /// DeviceId được ghi vào DB khi QR được dùng lần đầu — phục vụ thống kê.
    /// Trả về null nếu mạng lỗi hoặc response không parse được (caller hiển thị lỗi kết nối).
    /// Trả về QrVerifyResult với IsValid=false nếu QR không hợp lệ (caller hiển thị message từ API).
    /// </summary>
    public async Task<QrVerifyResult?> VerifyAsync(string code, string deviceId)
    {
        try
        {
            var client  = _httpClientFactory.CreateClient();
            var request = new QrCodeVerifyRequestDto { Code = code, DeviceId = deviceId };

            var response = await client.PostAsJsonAsync("api/qrcodes/verify", request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[QrService] Verify thất bại. StatusCode={Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Cấu trúc response chuẩn: { success: bool, data: { isValid, message, expiryAt? } }
            // success=false nghĩa là lỗi server-side (không phải QR invalid) → trả null.
            if (!root.GetProperty("success").GetBoolean()) return null;

            var data    = root.GetProperty("data");
            var isValid = data.GetProperty("isValid").GetBoolean();
            var message = data.GetProperty("message").GetString() ?? string.Empty;

            // expiryAt chỉ xuất hiện trong response khi isValid=true —
            // khi QR không hợp lệ, API không trả expiryAt, dùng MinValue làm sentinel.
            var expiryAt = isValid && data.TryGetProperty("expiryAt", out var expiryProp)
                ? expiryProp.GetDateTime()
                : DateTime.MinValue;

            _logger.LogInformation("[QrService] Verify kết quả: isValid={IsValid}, expiryAt={ExpiryAt:O}", isValid, expiryAt);
            return new QrVerifyResult(isValid, message, expiryAt);
        }
        catch (Exception ex)
        {
            // Bắt mọi exception (timeout, JSON parse lỗi, network…) → trả null
            // để caller phân biệt: null = lỗi kết nối, IsValid=false = QR không hợp lệ.
            _logger.LogError(ex, "[QrService] Lỗi khi verify mã QR");
            return null;
        }
    }

    /// <summary>
    /// Lưu cờ đã xác nhận và thời hạn QR vào Preferences.
    /// Gọi ngay sau khi VerifyAsync trả về IsValid=true.
    /// LoadingPage đọc hai key này mỗi lần khởi động để quyết định có cần quét lại không.
    /// </summary>
    public void SaveAccess(DateTime expiryAt)
    {
        Preferences.Set(VerifiedKey, true);
        Preferences.Set(ExpiryKey, expiryAt.ToString("O")); // "O" = ISO-8601 round-trip, giữ timezone chính xác
        _logger.LogInformation("[QrService] Đã lưu quyền truy cập. ExpiryAt={ExpiryAt:O}", expiryAt);
    }

    /// <summary>
    /// Kiểm tra hai điều kiện:
    ///   1. Đã từng verify QR thành công (VerifiedKey = true)
    ///   2. QR đó chưa hết hạn (ExpiryKey > UtcNow)
    /// Nếu một trong hai sai → user phải quét lại.
    /// </summary>
    public bool IsAccessValid()
    {
        // Chưa từng verify QR — lần đầu cài app hoặc sau khi ClearAccess().
        if (!Preferences.Get(VerifiedKey, false))
        {
            _logger.LogInformation("[QrService] Chưa có QR được verify.");
            return false;
        }

        // Parse thời hạn — nếu lỗi parse (data bị corrupt) thì coi như hết hạn để an toàn.
        var raw = Preferences.Get(ExpiryKey, string.Empty);
        if (!DateTime.TryParse(raw, out var expiry))
        {
            _logger.LogWarning("[QrService] Không parse được ExpiryAt='{Raw}' → coi như hết hạn.", raw);
            return false;
        }

        var valid = expiry > DateTime.UtcNow;
        if (valid)
            _logger.LogInformation("[QrService] Quyền truy cập còn hiệu lực. ExpiryAt={ExpiryAt:O}", expiry);
        else
            _logger.LogInformation("[QrService] QR đã hết hạn. ExpiryAt={ExpiryAt:O}, Now={Now:O}", expiry, DateTime.UtcNow);

        return valid;
    }

    /// <summary>
    /// Xoá cả hai key khỏi Preferences.
    /// Gọi khi user "đăng xuất" — lần mở app tiếp theo LoadingPage sẽ redirect về ScanPage.
    /// </summary>
    public void ClearAccess()
    {
        Preferences.Remove(VerifiedKey);
        Preferences.Remove(ExpiryKey);
        _logger.LogInformation("[QrService] Đã xoá quyền truy cập.");
    }
}
