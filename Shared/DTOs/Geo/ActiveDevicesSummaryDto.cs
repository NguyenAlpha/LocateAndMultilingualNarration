namespace Shared.DTOs.Geo
{
    public class ActiveDevicesSummaryDto
    {
        public int ActiveCount { get; set; }
        public int WithinSeconds { get; set; }
        public DateTimeOffset AsOf { get; set; }
        public List<ActiveDeviceItemDto> Devices { get; set; } = [];
    }
}
