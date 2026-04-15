namespace Shared.DTOs.Narrations
{
    public class TtsStatusDto
    {
        public Guid Id { get; set; }
        public string TtsStatus { get; set; } = "None";
        public string? TtsError { get; set; }
        public IReadOnlyList<NarrationAudioDetailDto> Audios { get; set; } = [];
    }
}
