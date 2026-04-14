namespace Shared.DTOs.DeviceLocationLogs;

public class LocationPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
