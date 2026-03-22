namespace Api.Domain.Entities
{
    public class NarrationAudio
    {
        public Guid Id { get; set; }                         // DEFAULT NEWSEQUENTIALID()
        public Guid NarrationContentId { get; set; }         // FK -> StallNarrationContents(Id)
        public Guid? TtsVoiceProfileId { get; set; }         // FK -> TtsVoiceProfiles(Id)
        public string? AudioUrl { get; set; }                // nvarchar(512)
        public string? BlobId { get; set; }                  // nvarchar(128)
        public string? Voice { get; set; }                   // nvarchar(64)
        public string? Provider { get; set; }                // nvarchar(64)
        public int? DurationSeconds { get; set; }            // int
        public bool IsTts { get; set; }                      // bit NOT NULL DEFAULT 0
        public DateTimeOffset? UpdatedAt { get; set; }       // datetimeoffset

        // Navigation
        public StallNarrationContent NarrationContent { get; set; } = null!;
        public TtsVoiceProfile? TtsVoiceProfile { get; set; }

    }
}
