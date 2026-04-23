using Shared.DTOs.Stalls;
using Shared.DTOs.Tours;

namespace Web.Models;

public class TourDesignerViewModel
{
    public bool IsEdit { get; set; }
    public TourDetailDto? Tour { get; set; }
    public IReadOnlyList<TourDesignerStallItem> AvailableStalls { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class TourDesignerStallItem
{
    public Guid StallId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? ThumbnailUrl { get; set; }
}
