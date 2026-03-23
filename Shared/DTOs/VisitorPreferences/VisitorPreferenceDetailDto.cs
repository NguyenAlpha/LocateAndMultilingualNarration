namespace Shared.DTOs.VisitorPreferences
{
    public class VisitorPreferenceDetailDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LanguageId { get; set; }
        public string? Voice { get; set; }
        public decimal SpeechRate { get; set; }
        public bool AutoPlay { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
