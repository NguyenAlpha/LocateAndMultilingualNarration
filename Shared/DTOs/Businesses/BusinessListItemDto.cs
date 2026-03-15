namespace Shared.DTOs.Businesses
{
    public class BusinessListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }
    }
}
