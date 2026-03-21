namespace Shared.DTOs.StallGeoFences
{
    public class StallGeoFenceDetailDto
    {
        public Guid Id { get; set; }
        public Guid StallId { get; set; }
        public string PolygonJson { get; set; } = string.Empty;
        public int? MinZoom { get; set; }
        public int? MaxZoom { get; set; }
    }
}
