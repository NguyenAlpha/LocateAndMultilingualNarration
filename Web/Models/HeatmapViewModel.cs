using Shared.DTOs.DeviceLocationLogs;
using Shared.DTOs.Geo;

namespace Web.Models;

public class HeatmapViewModel
{
    public List<HeatmapPointDto> Points { get; set; } = [];
    public List<GeoStallDto> Stalls { get; set; } = [];
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public string? DeviceId { get; set; }
    public int TotalPoints => Points.Count;
    public int TotalWeight => Points.Sum(p => p.Weight);
    public string? ErrorMessage { get; set; }
}
