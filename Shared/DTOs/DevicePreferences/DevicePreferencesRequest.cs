namespace Shared.DTOs.DevicePreferences
{
    public class DevicePreferencesRequest
    {
        public string DeviceId { get; set; } = null!;
        public Guid LanguageId { get; set; }
        public Guid? VoiceId { get; set; }
        public decimal SpeechRate { get; set; } = 1.0m;
        public bool AutoPlay { get; set; } = true;
        public string? Platform { get; set; }
        public string? DeviceModel { get; set; }
        public string? Manufacturer { get; set; }
        public string? OsVersion { get; set; }
    }
}
