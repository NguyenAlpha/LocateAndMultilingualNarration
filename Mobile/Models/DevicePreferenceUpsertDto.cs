namespace Mobile.Models;

/// <summary>
/// DTO upsert cấu hình thiết bị từ màn hình hồ sơ.
/// </summary>
public class DevicePreferenceUpsertDto
{
    public Guid? LanguageId { get; set; }
    public string? LanguageCode { get; set; }
    public Guid? VoiceId { get; set; }
    public decimal? SpeechRate { get; set; } = 1.0m;
    public bool? AutoPlay { get; set; } = true;
    public string? Platform { get; set; }
    public string? DeviceModel { get; set; }
    public string? Manufacturer { get; set; }
    public string? OsVersion { get; set; }
}
