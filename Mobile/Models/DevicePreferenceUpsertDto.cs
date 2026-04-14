namespace Mobile.Models;

public class DevicePreferenceUpsertDto
{
    internal string LanguageCode;

    public Guid? LanguageId { get; set; }
    public Guid? VoiceId { get; set; }        // Giữ Guid? như API
    public decimal? SpeechRate { get; set; } = 1.0m;
    public bool? AutoPlay { get; set; } = true;

    // Các trường bổ sung nếu cần gửi thêm thông tin thiết bị
    public string? Platform { get; set; }
    public string? DeviceModel { get; set; }
    public string? Manufacturer { get; set; }
    public string? OsVersion { get; set; }
}