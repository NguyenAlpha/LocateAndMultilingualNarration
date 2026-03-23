namespace Shared.DTOs.Geo
{
    public class GeoNearestStallDto
    {
        public Guid StallId { get; set; }
        public string StallName { get; set; } = string.Empty;
        public decimal DistanceMeters { get; set; }
        public string? ContentText { get; set; }
        public string? AudioUrl { get; set; }
    }
}
