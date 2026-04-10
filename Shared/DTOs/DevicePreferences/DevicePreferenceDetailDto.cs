namespace Shared.DTOs.DevicePreferences
{
    public class DevicePreferenceDetailDto
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = null!;
        public Guid LanguageId { get; set; }
        public Guid? VoiceId { get; set; }
        public decimal SpeechRate { get; set; }
        public bool AutoPlay { get; set; }
        public string? Platform { get; set; }
        public string? DeviceModel { get; set; }
        public string? Manufacturer { get; set; }
        public string? OsVersion { get; set; }
        public DateTimeOffset FirstSeenAt { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }
        public string LanguageName { get; set; } = null!;
        public string? LanguageDisplayName { get; set; }
        public string LanguageCode { get; set; } = null!;
        public string? LanguageFlagCode { get; set; }
    }
}
