using Shared.DTOs.Narrations;

namespace Web.Models
{
    public class StallNarrationContentShowViewModel
    {
        public StallNarrationContentDetailDto? Content { get; set; }
        public IReadOnlyList<NarrationAudioDetailDto> Audios { get; set; } = Array.Empty<NarrationAudioDetailDto>();
        public string? StallName { get; set; }
        public string? LanguageName { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
