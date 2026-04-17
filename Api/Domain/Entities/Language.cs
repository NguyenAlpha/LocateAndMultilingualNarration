namespace Api.Domain.Entities
{
    public class Language
    {
        public Guid Id { get; set; }                      // DEFAULT NEWSEQUENTIALID()
        public string Name { get; set; } = null!;         // nvarchar(64) NOT NULL
        public string? DisplayName { get; set; }          // nvarchar(64) NULL (tên bản ngữ, e.g. "Tiếng Việt")
        public string Code { get; set; } = null!;         // nvarchar(16) UNIQUE NOT NULL
        public string? FlagCode { get; set; }             // nvarchar(8) NULL (ISO 3166-1 alpha-2, e.g. "vn")
        public bool IsActive { get; set; }                // bit NOT NULL DEFAULT 1

        // Navigation
public ICollection<StallNarrationContent> StallNarrationContents { get; set; } = new List<StallNarrationContent>();
        public ICollection<TtsVoiceProfile> TtsVoiceProfiles { get; set; } = new List<TtsVoiceProfile>();
        public ICollection<DevicePreference> DevicePreferences { get; set; } = new List<DevicePreference>();

    }
}
