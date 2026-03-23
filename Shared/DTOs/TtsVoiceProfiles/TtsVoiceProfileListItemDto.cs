namespace Shared.DTOs.TtsVoiceProfiles
{
    public class TtsVoiceProfileListItemDto
    {
        public Guid Id { get; set; }
        public Guid LanguageId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Style { get; set; }
        public string? Role { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
    }
}
