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
    Task<string> GetOrCreateDeviceIdAsync();

    /// <summary>Lấy thông tin phần cứng của thiết bị hiện tại.</summary>
    DeviceInfoDto GetDeviceInfo();
}

/// <summary>
/// Quản lý định danh thiết bị (DeviceId) và thông tin phần cứng.
///
/// DeviceId là một GUID được tạo ngẫu nhiên lần đầu chạy app,
/// lưu vào SecureStorage (keychain trên iOS, keystore trên Android)
/// để tồn tại xuyên suốt các lần mở app — nhưng sẽ mất nếu người dùng
/// gỡ cài đặt app.
/// </summary>
public class DeviceService : IDeviceService
{
    // Key dùng để đọc/ghi DeviceId trong SecureStorage
    private const string DeviceIdKey = "device_id";

    /// <summary>
    /// Lấy DeviceId từ SecureStorage nếu đã tồn tại.
    /// Nếu chưa có (lần đầu cài app), tạo GUID mới, lưu lại rồi trả về.
    /// SecureStorage đảm bảo giá trị được mã hóa, không bị đọc bởi app khác.
    /// </summary>
    public async Task<string> GetOrCreateDeviceIdAsync()
    {
        // Thử lấy DeviceId đã lưu từ lần trước
        var existing = await SecureStorage.Default.GetAsync(DeviceIdKey);
        if (!string.IsNullOrEmpty(existing))
            return existing;

        // Chưa có → tạo GUID mới làm DeviceId, lưu vào SecureStorage
        var newId = Guid.NewGuid().ToString();
        await SecureStorage.Default.SetAsync(DeviceIdKey, newId);
        return newId;
    }

    /// <summary>
    /// Đọc thông tin phần cứng từ MAUI DeviceInfo API (đồng bộ, không tốn I/O).
    /// DeviceInfo.Current là singleton do framework cung cấp sẵn.
    /// </summary>
    public DeviceInfoDto GetDeviceInfo() => new()
    {
        Platform     = DeviceInfo.Current.Platform.ToString(),
        DeviceModel  = DeviceInfo.Current.Model,
        Manufacturer = DeviceInfo.Current.Manufacturer,
        OsVersion    = DeviceInfo.Current.VersionString
    };
}
