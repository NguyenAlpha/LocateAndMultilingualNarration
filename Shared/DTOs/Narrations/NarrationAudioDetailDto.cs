namespace Shared.DTOs.Narrations
{
    public class NarrationAudioDetailDto
    {
        public Guid Id { get; set; }
        public Guid NarrationContentId { get; set; }
        public Guid? TtsVoiceProfileId { get; set; }
        public string? TtsVoiceProfileDisplayName { get; set; }
        public string? TtsVoiceProfileDescription { get; set; }
        public string? TtsVoiceProfileLanguageName { get; set; }
        public string? AudioUrl { get; set; }
        public string? BlobId { get; set; }
        public string? Voice { get; set; }
        public string? Provider { get; set; }
        public int? DurationSeconds { get; set; }
        public bool IsTts { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
