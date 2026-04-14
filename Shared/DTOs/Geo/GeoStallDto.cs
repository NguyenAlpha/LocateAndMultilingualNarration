namespace Shared.DTOs.Geo
{
    public class GeoStallDto
    {
        public Guid StallId { get; set; }
        public string StallName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusMeters { get; set; }
        public GeoStallNarrationContentDto? NarrationContent { get; set; }
        public List<GeoStallMediaDto> MediaImages { get; set; } = [];
    }
}
