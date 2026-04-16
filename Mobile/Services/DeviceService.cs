namespace Mobile.Services;

/// <summary>
/// DTO chứa thông tin phần cứng và hệ điều hành của thiết bị.
/// Dùng để gửi kèm request lên backend nhận dạng loại thiết bị.
/// </summary>
public class DeviceInfoDto
{
    public string Platform { get; set; } = null!;     // "Android", "iOS", "WinUI"...
    public string DeviceModel { get; set; } = null!;  // Tên model máy, vd: "Pixel 7"
    public string Manufacturer { get; set; } = null!; // Nhà sản xuất, vd: "Google", "Samsung"
    public string OsVersion { get; set; } = null!;    // Phiên bản OS, vd: "13.0"
}

/// <summary>
/// Contract cho DeviceService.
/// </summary>
public interface IDeviceService
{
    /// <summary>Lấy DeviceId đã lưu, hoặc tạo mới nếu chưa có.</summary>
    string GetOrCreateDeviceId();

    /// <summary>Lấy thông tin phần cứng của thiết bị hiện tại.</summary>
    DeviceInfoDto GetDeviceInfo();

#if DEBUG
    /// <summary>Xóa DeviceId khỏi Preferences. Lần gọi GetOrCreateDeviceId tiếp theo sẽ tạo GUID mới.</summary>
    void ResetDeviceId();
#endif
}

/// <summary>
/// Quản lý định danh thiết bị (DeviceId) và thông tin phần cứng.
/// DeviceId là một GUID được tạo ngẫu nhiên lần đầu chạy app,
/// lưu vào Preferences để tồn tại xuyên suốt các lần mở app —
/// sẽ mất nếu người dùng gỡ cài đặt app.
/// </summary>
public class DeviceService : IDeviceService
{
    private const string DeviceIdKey = "device_id";

    /// <summary>
    /// Lấy DeviceId từ Preferences nếu đã tồn tại.
    /// Nếu chưa có (lần đầu cài app), tạo GUID mới, lưu lại rồi trả về.
    /// </summary>
    public string GetOrCreateDeviceId()
    {
        var existing = Preferences.Get(DeviceIdKey, null);
        if (!string.IsNullOrEmpty(existing))
            return existing;

        var newId = Guid.NewGuid().ToString();
        Preferences.Set(DeviceIdKey, newId);
        return newId;
    }

    /// <summary>
    /// Đọc thông tin phần cứng từ MAUI DeviceInfo API (đồng bộ, không tốn I/O).
    /// </summary>
    public DeviceInfoDto GetDeviceInfo() => new()
    {
        Platform     = DeviceInfo.Current.Platform.ToString(),
        DeviceModel  = DeviceInfo.Current.Model,
        Manufacturer = DeviceInfo.Current.Manufacturer,
        OsVersion    = DeviceInfo.Current.VersionString
    };

#if DEBUG
    public void ResetDeviceId() => Preferences.Remove(DeviceIdKey);
#endif
}
