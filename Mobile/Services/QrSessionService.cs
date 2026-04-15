namespace Mobile.Services;

public interface IQrSessionService
{
    /// <summary>Lưu kết quả verify QR thành công vào Preferences.</summary>
    void SaveSession(DateTime expiryAt);

    /// <summary>Kiểm tra QR đã verify và còn hạn hay chưa.</summary>
    bool IsSessionValid();

    /// <summary>Xoá session QR (dùng khi cần force re-scan).</summary>
    void ClearSession();
}

public class QrSessionService : IQrSessionService
{
    private const string VerifiedKey = "qr_verified";
    private const string ExpiryKey = "qr_expiry";

    public void SaveSession(DateTime expiryAt)
    {
        Preferences.Set(VerifiedKey, true);
        Preferences.Set(ExpiryKey, expiryAt.ToString("O"));
    }

    public bool IsSessionValid()
    {
        if (!Preferences.Get(VerifiedKey, false)) return false;
        var raw = Preferences.Get(ExpiryKey, string.Empty);
        return DateTime.TryParse(raw, out var expiry) && expiry > DateTime.UtcNow;
    }

    public void ClearSession()
    {
        Preferences.Remove(VerifiedKey);
        Preferences.Remove(ExpiryKey);
    }
}
