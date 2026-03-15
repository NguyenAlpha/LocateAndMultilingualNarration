namespace Shared.DTOs.Stalls
{
    public class StallDetailDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Slug { get; set; } = null!;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
