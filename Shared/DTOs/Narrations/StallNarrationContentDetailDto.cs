namespace Shared.DTOs.Narrations
{
    public class StallNarrationContentDetailDto
    {
        public Guid Id { get; set; }
        public Guid StallId { get; set; }
        public Guid LanguageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ScriptText { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string TtsStatus { get; set; } = "None";
        public string? TtsError { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
