namespace Shared.DTOs.Geo
{
    public class GeoStallNarrationContentDto
    {
        public Guid Id { get; set; }
        public Guid LanguageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ScriptText { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? AudioUrl { get; set; }
    }
}
