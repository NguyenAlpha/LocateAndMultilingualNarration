namespace Shared.DTOs.Businesses
{
    public class BusinessDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? TaxCode { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public Guid? OwnerUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
