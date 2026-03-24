namespace Shared.DTOs.Narrations
{
    public class StallNarrationContentWithAudiosDto
    {
        public StallNarrationContentDetailDto Content { get; set; } = new();
        public IReadOnlyList<NarrationAudioDetailDto> Audios { get; set; } = Array.Empty<NarrationAudioDetailDto>();
    }
}
