namespace Shared.DTOs.Tours
{
    public class TourListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int? EstimatedMinutes { get; set; }
        public bool IsActive { get; set; }
        public int StopCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
