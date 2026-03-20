namespace Api.Domain.Entities
{
    public class VisitorPreference
    {
        public Guid Id { get; set; }                    // DEFAULT NEWSEQUENTIALID()
        public Guid UserId { get; set; }                // UNIQUE FK -> Users(Id)
        public Guid LanguageId { get; set; }            // FK -> Languages(Id)
        public string? Voice { get; set; }              // nvarchar(64)
        public decimal SpeechRate { get; set; }         // decimal(4,2)
        public bool AutoPlay { get; set; }              // bit NOT NULL DEFAULT 0
        public DateTimeOffset? UpdatedAt { get; set; }  // datetimeoffset

        // Navigation
        public User User { get; set; } = null!;
        public Language Language { get; set; } = null!;

    }
}
