namespace Api.Domain.Entities
{
    public class BusinessOwnerProfile
    {
        public Guid Id { get; set; }                         // DEFAULT NEWSEQUENTIALID()
        public Guid UserId { get; set; }                     // UNIQUE FK -> Users(Id)
        public string? OwnerName { get; set; }               // nvarchar(256)
        public string? ContactInfo { get; set; }             // nvarchar(256)
        public DateTime CreatedAt { get; set; }        // datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()

        // Navigation
        public User User { get; set; } = null!;

    }
}
