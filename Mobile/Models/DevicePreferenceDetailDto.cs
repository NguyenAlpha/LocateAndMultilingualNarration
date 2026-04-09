namespace Mobile.Models;

/// <summary>
/// DTO cấu hình thiết bị trả về cho UI ProfilePage.
/// </summary>
public class DevicePreferenceDetailDto
{
    public string DeviceId { get; set; } = string.Empty;
    public Guid? LanguageId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public Guid? VoiceId { get; set; }
    public decimal SpeechRate { get; set; }
    public bool AutoPlay { get; set; }
    public string? Platform { get; set; }
    public string? DeviceModel { get; set; }
    public string? Manufacturer { get; set; }
    public string? OsVersion { get; set; }
    public DateTimeOffset? FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
