namespace Shared.DTOs.DeviceLocationLogs;

public class DeviceLocationLogBatchDto
{
    public string DeviceId { get; set; } = "";
    public List<LocationPointDto> Points { get; set; } = [];
}
