namespace Api.Domain.Entities
{
    public class DevicePreference
    {
        public Guid Id { get; set; }                    // DEFAULT NEWSEQUENTIALID()
        public string DeviceId { get; set; } = null!;   // nvarchar(128) UNIQUE NOT NULL
        public Guid LanguageId { get; set; }             // FK -> Languages(Id)
        public Guid? VoiceId { get; set; }               // FK -> TtsVoiceProfiles(Id)
        public decimal SpeechRate { get; set; } = 1.0m; // decimal(4,2)
        public bool AutoPlay { get; set; } = true;
        public string? Platform { get; set; }            // nvarchar(32)
        public string? DeviceModel { get; set; }         // nvarchar(128)
        public string? Manufacturer { get; set; }        // nvarchar(128)
        public string? OsVersion { get; set; }           // nvarchar(64)
        public DateTimeOffset FirstSeenAt { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }

        // Navigation
        public Language Language { get; set; } = null!;
        public TtsVoiceProfile? VoiceProfile { get; set; }
    }
}
