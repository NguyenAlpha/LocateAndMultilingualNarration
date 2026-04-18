namespace Shared.DTOs.Geo
{
    public class ActiveDeviceItemDto
    {
        public string DeviceId { get; set; } = null!;
        public string? Platform { get; set; }
        public string? DeviceModel { get; set; }
        public string? Manufacturer { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }
    }
}
