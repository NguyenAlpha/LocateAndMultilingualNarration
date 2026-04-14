namespace Api.Domain.Entities;

public class DeviceLocationLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// DeviceId từ SecureStorage của Mobile — không FK, đây là log table analytics.
    /// </summary>
    public string DeviceId { get; set; } = "";

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? AccuracyMeters { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
}
