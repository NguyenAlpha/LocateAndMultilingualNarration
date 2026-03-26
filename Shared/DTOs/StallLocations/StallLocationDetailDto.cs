namespace Shared.DTOs.StallLocations
{
    public class StallLocationDetailDto
    {
        public Guid Id { get; set; }
        public Guid StallId { get; set; }
        public string? StallName { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal RadiusMeters { get; set; }
        public string? Address { get; set; }
        public string? MapProviderPlaceId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
