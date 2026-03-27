namespace Mobile.Services;

public class DeviceInfoDto
{
    public string Platform { get; set; } = null!;
    public string DeviceModel { get; set; } = null!;
    public string Manufacturer { get; set; } = null!;
    public string OsVersion { get; set; } = null!;
}

public interface IDeviceService
{
    Task<string> GetOrCreateDeviceIdAsync();
    DeviceInfoDto GetDeviceInfo();
}

public class DeviceService : IDeviceService
{
    private const string DeviceIdKey = "device_id";

    public async Task<string> GetOrCreateDeviceIdAsync()
    {
        var existing = await SecureStorage.Default.GetAsync(DeviceIdKey);
        if (!string.IsNullOrEmpty(existing))
            return existing;

        var newId = Guid.NewGuid().ToString();
        await SecureStorage.Default.SetAsync(DeviceIdKey, newId);
        return newId;
    }

    public DeviceInfoDto GetDeviceInfo() => new()
    {
        Platform     = DeviceInfo.Current.Platform.ToString(),
        DeviceModel  = DeviceInfo.Current.Model,
        Manufacturer = DeviceInfo.Current.Manufacturer,
        OsVersion    = DeviceInfo.Current.VersionString
    };
}
