namespace Api.Domain.Entities
{
    public class StallNarrationContent
    {
        public Guid Id { get; set; }                      // DEFAULT NEWSEQUENTIALID()
        public Guid StallId { get; set; }                 // FK -> Stalls(Id)
        public Guid LanguageId { get; set; }              // FK -> Languages(Id)
        public string Title { get; set; } = null!;        // nvarchar(128) NOT NULL
        public string? Description { get; set; }          // nvarchar(256)
        public string ScriptText { get; set; } = null!;   // nvarchar(max) NOT NULL
        public bool IsActive { get; set; }                // bit NOT NULL DEFAULT 1
        public string TtsStatus { get; set; } = TtsJobStatus.None; // nvarchar(32) NOT NULL DEFAULT 'None'
        public string? TtsError { get; set; }             // nvarchar(512)
        public DateTimeOffset? UpdatedAt { get; set; }    // datetimeoffset

        // Navigation
        public Stall Stall { get; set; } = null!;
        public Language Language { get; set; } = null!;
        public ICollection<NarrationAudio> NarrationAudios { get; set; } = new List<NarrationAudio>();

    }
}
