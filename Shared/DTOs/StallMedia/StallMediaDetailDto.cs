namespace Shared.DTOs.StallMedia
{
    public class StallMediaDetailDto
    {
        public Guid Id { get; set; }
        public Guid StallId { get; set; }
        public string MediaUrl { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
