using System.Security;
using System.Text;
using Api.Domain.Entities;
using Api.Domain.Settings;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Extensions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Application.Services
{
    /// <summary>
    /// Service nghiệp vụ xử lý tạo/cập nhật audio narration từ upload hoặc TTS.
    /// </summary>`
    public interface INarrationAudioService
    {
        /// <summary>
        /// Tạo hoặc cập nhật audio bằng TTS (Azure Speech).
        /// </summary>
        Task<IReadOnlyList<NarrationAudio>> CreateOrUpdateFromTtsAsync(Guid narrationContentId, string scriptText, Guid languageId, string? voice, string? provider);
    }

    /// <summary>
    /// Triển khai dịch vụ audio narration: upload thủ công và TTS qua Azure Speech.
    /// </summary>
    public class NarrationAudioService : INarrationAudioService
    {
        private readonly AppDbContext _context;
        private readonly AzureSpeechSettings _speechSettings;
        private readonly BlobStorageSettings _blobSettings;
        private readonly ITranslationService _translationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NarrationAudioService> _logger;

        public NarrationAudioService(AppDbContext context, IOptions<AzureSpeechSettings> speechSettings, IOptions<BlobStorageSettings> blobSettings, ITranslationService translationService, IHttpClientFactory httpClientFactory, ILogger<NarrationAudioService> logger)
        {
            _context = context;
            _speechSettings = speechSettings.Value;
            _blobSettings = blobSettings.Value;
            _translationService = translationService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IReadOnlyList<NarrationAudio>> CreateOrUpdateFromTtsAsync(Guid narrationContentId, string scriptText, Guid languageId, string? voice, string? provider)
        {
            _logger.LogInformation("Bắt đầu tạo/cập nhật TTS cho NarrationContentId: {NarrationContentId}, LanguageId: {LanguageId}", narrationContentId, languageId);

            // Bước 1: Kiểm tra dữ liệu đầu vào không được rỗng.
            if (string.IsNullOrWhiteSpace(scriptText))
            {
                _logger.LogWarning("ScriptText rỗng khi tạo TTS cho NarrationContentId: {NarrationContentId}", narrationContentId);
                throw new InvalidOperationException("ScriptText không được rỗng khi tạo TTS.");
            }

            // Bước 2: Lấy mã ngôn ngữ gốc của nội dung narration.
            var languageCode = await _context.Languages.GetCodeByIdAsync(languageId);

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                _logger.LogWarning("Không tìm thấy ngôn ngữ cho LanguageId: {LanguageId}, NarrationContentId: {NarrationContentId}", languageId, narrationContentId);
                throw new InvalidOperationException("Không tìm thấy ngôn ngữ để tạo TTS.");
            }

            // Bước 3: Lấy danh sách voice profile TTS đang hoạt động.
            var voiceProfiles = await _context.TtsVoiceProfiles
                .Where(v => v.IsActive)
                .OrderBy(v => v.Priority)
                .ThenBy(v => v.Id)
                .ToListAsync();

            _logger.LogInformation("Tìm thấy {Count} voice profile đang hoạt động cho NarrationContentId: {NarrationContentId}", voiceProfiles.Count, narrationContentId);

            // Bước 4: Nếu không có voice profile nào thì dùng nhánh fallback.
            // Trường hợp này hệ thống vẫn tạo một bản audio duy nhất bằng voice/provider được truyền vào
            // thay vì dịch và sinh nhiều bản theo từng profile.
            if (voiceProfiles.Count == 0)
            {
                _logger.LogInformation("Không có voice profile nào, dùng fallback voice cho NarrationContentId: {NarrationContentId}", narrationContentId);
                var fallbackAudio = await CreateOrUpdateSingleAsync(narrationContentId, scriptText, languageCode, voice, provider, null, null);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã tạo/cập nhật fallback audio cho NarrationContentId: {NarrationContentId}", narrationContentId);
                return new List<NarrationAudio> { fallbackAudio };
            }

            // Bước 5: Gom các LanguageId cần tra cứu để tránh query lặp lại trong vòng foreach.
            // Mỗi voice profile sẽ trỏ tới một ngôn ngữ đích; ta chỉ truy vấn danh sách ngôn ngữ duy nhất một lần.
            var targetLanguageIds = voiceProfiles
                .Select(v => v.LanguageId)
                .Distinct()
                .ToList();

            // Bước 6: Tải trước mã ngôn ngữ cho các profile đích.
            // Kết quả là một dictionary cho phép tra cứu nhanh LanguageId -> Code khi duyệt từng profile.
            var targetLanguageCodes = await _context.Languages.GetCodeDictionaryAsync(targetLanguageIds);

            // Bước 7: Duyệt từng profile để tạo/cập nhật audio tương ứng.
            // Với mỗi profile:
            // - xác định ngôn ngữ đích,
            // - dịch nội dung nếu ngôn ngữ đích khác ngôn ngữ gốc,
            // - chọn voice ưu tiên của profile (nếu có),
            // - gọi hàm synthesize/upload để tạo file audio và lưu metadata.
            var results = new List<NarrationAudio>(voiceProfiles.Count);
            foreach (var profile in voiceProfiles)
            {
                if (!targetLanguageCodes.TryGetValue(profile.LanguageId, out var targetLanguageCode) || string.IsNullOrWhiteSpace(targetLanguageCode))
                {
                    _logger.LogWarning("Không tìm thấy mã ngôn ngữ cho voice profile {VoiceProfileId}, LanguageId: {LanguageId}", profile.Id, profile.LanguageId);
                    throw new InvalidOperationException("Không tìm thấy ngôn ngữ để dịch/đọc cho voice profile.");
                }

                // Nếu ngôn ngữ đích trùng với ngôn ngữ gốc thì dùng nguyên văn bản.
                // Nếu khác nhau thì dịch sang ngôn ngữ đích trước khi đọc bằng TTS.
                var textToSpeak = string.Equals(languageCode, targetLanguageCode, StringComparison.OrdinalIgnoreCase)
                    ? scriptText
                    : await _translationService.TranslateAsync(scriptText, languageCode, targetLanguageCode);

                _logger.LogInformation(
                    "Đang xử lý voice profile {VoiceProfileId} với ngôn ngữ đích {TargetLanguageCode} cho NarrationContentId: {NarrationContentId}",
                    profile.Id,
                    targetLanguageCode,
                    narrationContentId);

                // Ưu tiên voice được cấu hình riêng cho profile.
                // Nếu profile không chỉ định voice thì fallback về voice do caller truyền vào.
                var profileVoice = !string.IsNullOrWhiteSpace(profile.VoiceName) ? profile.VoiceName : voice;

                // Tạo hoặc cập nhật một bản ghi audio cho profile hiện tại.
                // profile.Id và profile.Provider được lưu kèm để truy vết nguồn cấu hình TTS đã dùng.
                var audio = await CreateOrUpdateSingleAsync(narrationContentId, textToSpeak, targetLanguageCode, profileVoice, provider, profile.Id, profile.Provider);
                results.Add(audio);

                _logger.LogInformation("Hoàn tất voice profile {VoiceProfileId} cho NarrationContentId: {NarrationContentId}", profile.Id, narrationContentId);
            }

            // Bước 8: Lưu toàn bộ thay đổi vào database sau khi xử lý xong tất cả profile.
            // Việc SaveChangesAsync chỉ gọi một lần giúp giảm số lần round-trip tới DB.
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã lưu {Count} audio TTS cho NarrationContentId: {NarrationContentId}", results.Count, narrationContentId);

            return results;
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật một bản ghi audio TTS cho một voice profile cụ thể.
        /// </summary>
        /// <param name="narrationContentId">Id của nội dung narration đang xử lý.</param>
        /// <param name="scriptText">Nội dung văn bản dùng để sinh audio.</param>
        /// <param name="languageCode">Mã ngôn ngữ của văn bản đầu vào.</param>
        /// <param name="voice">Voice được sử dụng để synthesize.</param>
        /// <param name="provider">Nhà cung cấp mặc định do luồng gọi truyền vào.</param>
        /// <param name="voiceProfileId">Id voice profile nếu audio thuộc về profile cụ thể; null khi dùng fallback.</param>
        /// <param name="profileProvider">Nhà cung cấp ưu tiên lấy từ voice profile.</param>
        /// <returns>Bản ghi <see cref="NarrationAudio"/> đã được tạo hoặc cập nhật.</returns>
        private async Task<NarrationAudio> CreateOrUpdateSingleAsync(Guid narrationContentId, string scriptText, string languageCode, string? voice, string? provider, Guid? voiceProfileId, string? profileProvider)
        {
            // Sinh audio và upload lên Blob Storage trước, sau đó mới cập nhật metadata trong DB.
            var (audioUrl, blobId, durationSeconds, voiceName) = await SynthesizeAndUploadAsync(narrationContentId, scriptText, languageCode, voice);

            // Tìm bản ghi hiện có theo NarrationContentId + VoiceProfileId để tránh tạo trùng dữ liệu.
            var audio = await _context.NarrationAudios
                .FirstOrDefaultAsync(a => a.NarrationContentId == narrationContentId && a.TtsVoiceProfileId == voiceProfileId);

            // Nếu chưa có bản ghi thì tạo mới để lưu kết quả TTS vừa sinh ra.
            if (audio == null)
            {
                audio = new NarrationAudio
                {
                    NarrationContentId = narrationContentId,
                    TtsVoiceProfileId = voiceProfileId
                };
                _context.NarrationAudios.Add(audio);
            }

            // Cập nhật metadata cho audio theo kết quả synthesize mới nhất.
            audio.AudioUrl = audioUrl;
            audio.BlobId = blobId;
            audio.Voice = voiceName;

            // Ưu tiên provider lấy từ profile; nếu không có thì dùng provider đầu vào,
            // và cuối cùng fallback về provider cấu hình mặc định của hệ thống.
            audio.Provider = !string.IsNullOrWhiteSpace(profileProvider)
                ? profileProvider
                : string.IsNullOrWhiteSpace(provider)
                    ? _speechSettings.Provider
                    : provider;

            // Ghi nhận thời lượng, đánh dấu đây là audio TTS và cập nhật thời điểm sửa gần nhất.
            audio.DurationSeconds = durationSeconds;
            audio.IsTts = true;
            audio.UpdatedAt = DateTimeOffset.UtcNow;

            return audio;
        }

        /// <summary>
        /// Thực hiện toàn bộ luồng tạo audio TTS:
        /// 1) kiểm tra cấu hình Azure Speech và Blob Storage,
        /// 2) chọn voice phù hợp,
        /// 3) gọi Azure Speech để sinh file mp3,
        /// 4) upload file lên Blob Storage,
        /// 5) trả về thông tin cần lưu vào bảng <see cref="NarrationAudio"/>.
        /// </summary>
        /// <param name="narrationContentId">Id của nội dung narration đang được xử lý.</param>
        /// <param name="scriptText">Nội dung văn bản sẽ được đọc thành audio.</param>
        /// <param name="languageCode">Mã ngôn ngữ dùng để cấu hình TTS, ví dụ <c>vi-VN</c>.</param>
        /// <param name="voice">Voice được yêu cầu từ bên ngoài; nếu rỗng sẽ dùng voice mặc định trong cấu hình.</param>
        /// <returns>
        /// Bộ giá trị gồm:
        /// <list type="bullet">
        /// <item><description><c>audioUrl</c>: URL truy cập file audio sau khi upload.</description></item>
        /// <item><description><c>blobId</c>: tên blob để lưu tham chiếu trong DB.</description></item>
        /// <item><description><c>durationSeconds</c>: thời lượng audio (nếu Azure trả về).</description></item>
        /// <item><description><c>voiceName</c>: voice thực tế đã dùng để synthesize.</description></item>
        /// </list>
        /// </returns>
        private async Task<(string audioUrl, string blobId, int? durationSeconds, string voiceName)> SynthesizeAndUploadAsync(Guid narrationContentId, string scriptText, string languageCode, string? voice)
        {
            if (string.IsNullOrWhiteSpace(_speechSettings.Endpoint) || string.IsNullOrWhiteSpace(_speechSettings.Key))
                throw new InvalidOperationException("Thiếu cấu hình Azure Speech (Endpoint/Key).");

            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
                throw new InvalidOperationException("Thiếu cấu hình Blob Storage (ConnectionString/ContainerName).");

            var voiceName = !string.IsNullOrWhiteSpace(voice) ? voice : _speechSettings.DefaultVoice;
            if (string.IsNullOrWhiteSpace(voiceName))
                throw new InvalidOperationException("Thiếu cấu hình DefaultVoice cho Azure Speech.");

            // Derive TTS REST endpoint từ cognitive services endpoint.
            // Ví dụ: https://eastasia.api.cognitive.microsoft.com/ → https://eastasia.tts.speech.microsoft.com/cognitiveservices/v1
            var cogUri = new Uri(_speechSettings.Endpoint);
            var region = cogUri.Host.Split('.')[0];
            var ttsEndpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";

            var ssml = $"<speak version='1.0' xml:lang='{languageCode}'>" +
                       $"<voice xml:lang='{languageCode}' name='{voiceName}'>" +
                       SecurityElement.Escape(scriptText) +
                       "</voice></speak>";

            _logger.LogInformation("Bắt đầu synthesize TTS (REST) cho NarrationContentId: {NarrationContentId}, Endpoint: {Endpoint}", narrationContentId, ttsEndpoint);

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, ttsEndpoint);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _speechSettings.Key);
            request.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-32kbitrate-mono-mp3");
            request.Headers.Add("User-Agent", "LocateAndMultilingualNarration");
            request.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"TTS REST API thất bại: {(int)response.StatusCode} {response.StatusCode} - {errorBody}");
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();

            var blobServiceClient = new BlobServiceClient(_blobSettings.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blobSettings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobName = $"narration-audio/{narrationContentId}/{DateTime.UtcNow:yyyyMMddHHmmssfff}.mp3";
            var blobClient = containerClient.GetBlobClient(blobName);

            await using (var stream = new MemoryStream(audioBytes))
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "audio/mpeg" });
            }

            _logger.LogInformation("Upload audio thành công - BlobName: {BlobName}", blobName);
            return (blobClient.Uri.ToString(), blobName, null, voiceName);
        }
    }
}
