namespace Api.Domain.Entities
{
    public class Business
    {
        public Guid Id { get; set; }                              // DEFAULT NEWSEQUENTIALID()
        public string Name { get; set; } = null!;                 // nvarchar(256) NOT NULL
        public string? TaxCode { get; set; }                      // nvarchar(32)
        public string? ContactEmail { get; set; }                 // nvarchar(256)
        public string? ContactPhone { get; set; }                 // nvarchar(32)
        public Guid? OwnerUserId { get; set; }                    // FK -> Users(Id)
        public DateTimeOffset CreatedAt { get; set; }             // datetimeoffset(3) NOT NULL DEFAULT SYSUTCDATETIME()
        public bool IsActive { get; set; }                        // bit NOT NULL DEFAULT 1

        // Navigation
        public User? OwnerUser { get; set; }
        public ICollection<Stall> Stalls { get; set; } = new List<Stall>();

    }
}
