using Api.Application.Services;
using Api.Domain.Entities;

namespace TestAPI
{
    /// <summary>
    /// Fake TTS service dùng trong test – không gọi Azure, trả về danh sách rỗng.
    /// </summary>
    public class FakeNarrationAudioService : INarrationAudioService
    {
        public Task<NarrationAudio> CreateFromUploadAsync(
            Guid narrationContentId, string? audioUrl, string? blobId,
            string? voice, string? provider, int? durationSeconds, bool isTts)
        {
            return Task.FromResult(new NarrationAudio
            {
                Id = Guid.NewGuid(),
                NarrationContentId = narrationContentId,
                AudioUrl = audioUrl,
                BlobId = blobId,
                Voice = voice,
                Provider = provider,
                DurationSeconds = durationSeconds,
                IsTts = isTts
            });
        }

        public Task<NarrationAudio> UpdateFromUploadAsync(
            NarrationAudio audio, string? audioUrl, string? blobId,
            string? voice, string? provider, int? durationSeconds, bool isTts)
        {
            audio.AudioUrl = audioUrl;
            audio.BlobId = blobId;
            audio.Voice = voice;
            audio.Provider = provider;
            audio.DurationSeconds = durationSeconds;
            audio.IsTts = isTts;
            return Task.FromResult(audio);
        }

        public Task<IReadOnlyList<NarrationAudio>> CreateOrUpdateFromTtsAsync(
            Guid narrationContentId, string scriptText, Guid languageId,
            string? voice, string? provider)
        {
            return Task.FromResult<IReadOnlyList<NarrationAudio>>(new List<NarrationAudio>());
        }
    }
}
