namespace Mobile.Services;

/// <summary>
/// Quản lý quyền truy cập app qua mã QR.
/// Sau khi Mobile verify QR thành công với API, kết quả được lưu vào Preferences
/// để LoadingPage biết không cần quét lại khi mở app lần sau.
/// Quyền truy cập tự hết hạn theo ExpiryAt của mã QR đó.
/// </summary>
public interface IQrAccessService
{
    /// <summary>Lưu quyền truy cập sau khi verify QR thành công.</summary>
    void SaveAccess(DateTime expiryAt);

    /// <summary>Kiểm tra quyền truy cập còn hợp lệ hay không.</summary>
    bool IsAccessValid();

    /// <summary>Xoá quyền truy cập, buộc user quét QR lại.</summary>
    void ClearAccess();
}

public class QrAccessService : IQrAccessService
{
    // Hai key lưu vào Preferences — tồn tại xuyên suốt các lần tắt/mở app,
    // mất khi user gỡ cài đặt app.
    private const string VerifiedKey = "qr_verified"; // bool   – đã quét QR thành công chưa
    private const string ExpiryKey   = "qr_expiry";   // string – thời điểm QR hết hạn (ISO-8601)

    /// <summary>
    /// Gọi sau khi API verify trả về isValid=true.
    /// Lưu cờ đã xác nhận và thời hạn của mã QR vừa quét.
    /// </summary>
    public void SaveAccess(DateTime expiryAt)
    {
        Preferences.Set(VerifiedKey, true);
        Preferences.Set(ExpiryKey, expiryAt.ToString("O")); // "O" = ISO-8601 round-trip format
    }

    /// <summary>
    /// Trả về true nếu thiết bị đã quét QR hợp lệ VÀ QR đó chưa hết hạn.
    /// Dùng trong LoadingPage để quyết định có cần quét lại không.
    /// </summary>
    public bool IsAccessValid()
    {
        // Chưa từng quét QR thành công
        if (!Preferences.Get(VerifiedKey, false)) return false;

        // Đọc và parse thời hạn — nếu lỗi parse thì coi như hết hạn
        var raw = Preferences.Get(ExpiryKey, string.Empty);
        return DateTime.TryParse(raw, out var expiry) && expiry > DateTime.UtcNow;
    }

    /// <summary>
    /// Xoá quyền truy cập, buộc user phải quét QR lại lần sau mở app.
    /// </summary>
    public void ClearAccess()
    {
        Preferences.Remove(VerifiedKey);
        Preferences.Remove(ExpiryKey);
    }
}
