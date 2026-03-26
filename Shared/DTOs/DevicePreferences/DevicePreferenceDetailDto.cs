namespace Shared.DTOs.DevicePreferences
{
    public class DevicePreferenceDetailDto
    {
        public string DeviceId { get; set; } = null!;
        public string LanguageCode { get; set; } = null!;
        public string LanguageName { get; set; } = null!;
        public string? Voice { get; set; }
        public decimal SpeechRate { get; set; }
        public bool AutoPlay { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }
    }
}
