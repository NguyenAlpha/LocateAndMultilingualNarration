namespace Api.Domain.Entities
{
    public class VisitorProfile
    {
        public Guid Id { get; set; }            // DEFAULT NEWSEQUENTIALID()
        public Guid UserId { get; set; }        // UNIQUE FK -> Users(Id)
        public Guid LanguageId { get; set; }    // FK -> Languages(Id)
        public DateTime CreatedAt { get; set; } // datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()

        // Navigation
        public User User { get; set; } = null!;
        public Language Language { get; set; } = null!;

    }
}
