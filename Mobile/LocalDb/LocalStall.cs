using SQLite;

namespace Mobile.LocalDb;

[Table("Stalls")]
public class LocalStall
{
    [PrimaryKey]
    public string StallId { get; set; } = null!;   // Guid.ToString()
    public string StallName { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; }
    public string? AudioUrl { get; set; }           // URL remote từ API
    public string? LocalAudioPath { get; set; }     // Path file local sau khi download
    public string LanguageCode { get; set; } = string.Empty;
    public string VoiceId { get; set; } = string.Empty;  // TtsVoiceProfileId
    public DateTimeOffset LastUpdated { get; set; }

    // Narration content (từ StallNarrationContent IsActive = true)
    public string? NarrationContentId { get; set; }
    public string? NarrationTitle { get; set; }
    public string? NarrationDescription { get; set; }
    public string? NarrationScriptText { get; set; }
}
