namespace Api.Domain.Entities
{
    public class TtsVoiceProfile
    {
        public Guid Id { get; set; }                         // DEFAULT NEWSEQUENTIALID()
        public Guid LanguageId { get; set; }                 // FK -> Languages(Id)
        public string DisplayName { get; set; } = null!;      // nvarchar(128) NOT NULL
        public string? Description { get; set; }             // nvarchar(256)
        public string? VoiceName { get; set; }               // nvarchar(128)
        public string? Style { get; set; }                   // nvarchar(64)
        public string? Role { get; set; }                    // nvarchar(64)
        public string? Provider { get; set; }                // nvarchar(64)
        public bool IsDefault { get; set; }                  // bit NOT NULL DEFAULT 0
        public bool IsActive { get; set; }                   // bit NOT NULL DEFAULT 1
        public int Priority { get; set; }                    // int NOT NULL DEFAULT 0

        // Navigation
        public Language Language { get; set; } = null!;
        public ICollection<NarrationAudio> NarrationAudios { get; set; } = new List<NarrationAudio>();
    }
}
