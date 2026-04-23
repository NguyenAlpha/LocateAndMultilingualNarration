namespace Shared.DTOs.Tours
{
    public class TourStopDetailDto
    {
        public Guid Id { get; set; }
        public Guid StallId { get; set; }
        public string StallName { get; set; } = null!;
        public string StallSlug { get; set; } = null!;
        public int Order { get; set; }
        public string? Note { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
