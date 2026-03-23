using Api.Domain.Entities;
using Api.Domain.Settings;
using Api.Infrastructure.Persistence;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Application.Services
{
    /// <summary>
    /// Service nghiệp vụ xử lý tạo/cập nhật audio narration từ upload hoặc TTS.
    /// </summary>
    public interface INarrationAudioService
    {
        /// <summary>
        /// Tạo mới bản ghi audio từ file do client upload (không TTS).
        /// </summary>
        Task<NarrationAudio> CreateFromUploadAsync(Guid narrationContentId, string? audioUrl, string? blobId, string? voice, string? provider, int? durationSeconds, bool isTts);
        /// <summary>
        /// Cập nhật bản ghi audio từ file upload.
        /// </summary>
        Task<NarrationAudio> UpdateFromUploadAsync(NarrationAudio audio, string? audioUrl, string? blobId, string? voice, string? provider, int? durationSeconds, bool isTts);
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
        private readonly ILogger<NarrationAudioService> _logger;

        /// <summary>
        /// Khởi tạo service với DbContext, cấu hình Speech/Blob và logger.
        /// </summary>
        public NarrationAudioService(
            AppDbContext context,
            IOptions<AzureSpeechSettings> speechSettings,
            IOptions<BlobStorageSettings> blobSettings,
            ITranslationService translationService,
            ILogger<NarrationAudioService> logger)
        {
            // Inject DbContext + cấu hình + logger
            _context = context;
            _speechSettings = speechSettings.Value;
            _blobSettings = blobSettings.Value;
            _translationService = translationService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới bản ghi audio từ dữ liệu upload của client.
        /// </summary>
        public async Task<NarrationAudio> CreateFromUploadAsync(Guid narrationContentId, string? audioUrl, string? blobId, string? voice, string? provider, int? durationSeconds, bool isTts)
        {
            // Tạo bản ghi audio từ dữ liệu do client cung cấp
            var audio = new NarrationAudio
            {
                NarrationContentId = narrationContentId,
                AudioUrl = audioUrl,
                BlobId = blobId,
                Voice = voice,
                Provider = provider,
                DurationSeconds = durationSeconds,
                IsTts = isTts,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // Lưu DB
            _context.NarrationAudios.Add(audio);
            await _context.SaveChangesAsync();

            return audio;
        }

        /// <summary>
        /// Cập nhật bản ghi audio từ dữ liệu upload của client.
        /// </summary>
        public async Task<NarrationAudio> UpdateFromUploadAsync(NarrationAudio audio, string? audioUrl, string? blobId, string? voice, string? provider, int? durationSeconds, bool isTts)
        {
            // Cập nhật các trường từ dữ liệu client gửi lên
            audio.AudioUrl = audioUrl;
            audio.BlobId = blobId;
            audio.Voice = voice;
            audio.Provider = provider;
            audio.DurationSeconds = durationSeconds;
            audio.IsTts = isTts;
            audio.UpdatedAt = DateTimeOffset.UtcNow;

            // Lưu thay đổi
            await _context.SaveChangesAsync();

            return audio;
        }

        /// <summary>
        /// Tạo hoặc cập nhật audio bằng TTS dựa trên ScriptText.
        /// </summary>
        public async Task<IReadOnlyList<NarrationAudio>> CreateOrUpdateFromTtsAsync(Guid narrationContentId, string scriptText, Guid languageId, string? voice, string? provider)
        {
            _logger.LogInformation("Bắt đầu tạo/cập nhật TTS cho NarrationContentId: {NarrationContentId}, LanguageId: {LanguageId}", narrationContentId, languageId);

            // Bước 1: Kiểm tra dữ liệu đầu vào.
            // TTS chỉ có ý nghĩa khi có nội dung văn bản hợp lệ để chuyển thành giọng nói.
            // Nếu ScriptText rỗng hoặc chỉ chứa khoảng trắng thì dừng ngay để tránh gọi các bước xử lý phía sau.
            if (string.IsNullOrWhiteSpace(scriptText))
            {
                _logger.LogWarning("ScriptText rỗng khi tạo TTS cho NarrationContentId: {NarrationContentId}", narrationContentId);
                throw new InvalidOperationException("ScriptText không được rỗng khi tạo TTS.");
            }

            // Bước 2: Lấy mã ngôn ngữ gốc của nội dung narration.
            // languageId là khóa ngoại trỏ tới bảng Languages; từ đó lấy Code (ví dụ: vi-VN, en-US)
            // để cấu hình cho Azure Speech và phục vụ việc dịch sang ngôn ngữ đích nếu cần.
            var languageCode = await _context.Languages
                .AsNoTracking()
                .Where(l => l.Id == languageId)
                .Select(l => l.Code)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                _logger.LogWarning("Không tìm thấy ngôn ngữ cho LanguageId: {LanguageId}, NarrationContentId: {NarrationContentId}", languageId, narrationContentId);
                throw new InvalidOperationException("Không tìm thấy ngôn ngữ để tạo TTS.");
            }

            // Bước 3: Lấy danh sách voice profile TTS đang hoạt động.
            // Chỉ những profile có IsActive = true mới được phép tham gia luồng tạo audio.
            // Việc sắp xếp theo Priority giúp xử lý profile quan trọng trước.
            // ThenBy(Id) được dùng để đảm bảo thứ tự ổn định khi nhiều profile có cùng Priority.
            // Lấy danh sách voice profile TTS đang được bật.
            // Chỉ các profile có IsActive = true mới được dùng để tạo audio.
            // Sắp xếp theo Priority trước để xử lý profile ưu tiên cao hơn trước,
            // nếu Priority trùng nhau thì sắp xếp tiếp theo Id để kết quả ổn định.
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
            var targetLanguageCodes = await _context.Languages
                .AsNoTracking()
                .Where(l => targetLanguageIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Code);

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
        private async Task<NarrationAudio> CreateOrUpdateSingleAsync(
            Guid narrationContentId,
            string scriptText,
            string languageCode,
            string? voice,
            string? provider,
            Guid? voiceProfileId,
            string? profileProvider)
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
            // Kiểm tra cấu hình Azure Speech trước khi gọi dịch vụ bên ngoài.
            // Nếu thiếu endpoint hoặc key, không nên synthesize vì sẽ lỗi ngay từ tầng SDK.
            if (string.IsNullOrWhiteSpace(_speechSettings.Endpoint) || string.IsNullOrWhiteSpace(_speechSettings.Key))
            {
                throw new InvalidOperationException("Thiếu cấu hình Azure Speech (Endpoint/Key).");
            }

            // Kiểm tra cấu hình Blob Storage trước khi upload file audio.
            // Blob Storage là nơi lưu file nhị phân audio thay vì lưu trực tiếp trong database.
            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
            {
                throw new InvalidOperationException("Thiếu cấu hình Blob Storage (ConnectionString/ContainerName).");
            }

            // Ưu tiên voice được truyền vào từ caller.
            // Nếu caller không truyền, dùng voice mặc định để đảm bảo vẫn tạo được audio.
            var voiceName = !string.IsNullOrWhiteSpace(voice) ? voice : _speechSettings.DefaultVoice;
            if (string.IsNullOrWhiteSpace(voiceName))
            {
                throw new InvalidOperationException("Thiếu cấu hình DefaultVoice cho Azure Speech.");
            }

            // Tạo cấu hình cho Azure Speech SDK.
            // Endpoint + key là credential để gọi dịch vụ TTS của Azure.
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(_speechSettings.Endpoint), _speechSettings.Key);
            speechConfig.SpeechSynthesisVoiceName = voiceName;
            speechConfig.SpeechSynthesisLanguage = languageCode;

            // Chọn format đầu ra là MP3 mono 16Khz để file nhẹ và phù hợp cho phát lại trên web/mobile.
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

            _logger.LogInformation("Bắt đầu synthesize TTS cho NarrationContentId: {NarrationContentId}", narrationContentId);

            // Gọi Azure Speech để sinh audio từ script text.
            // Nếu dịch vụ trả về không thành công thì sẽ ném lỗi để tầng trên xử lý nghiệp vụ.
            using var synthesizer = new SpeechSynthesizer(speechConfig);
            var result = await synthesizer.SpeakTextAsync(scriptText);

            // Nếu synthesize thất bại hoặc bị hủy, lấy chi tiết lỗi từ Azure để dễ debug.
            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                var details = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new InvalidOperationException($"TTS thất bại: {details.ErrorDetails}");
            }

            // Upload file mp3 đã synthesize lên Blob Storage.
            // Mỗi narrationContentId sẽ có một nhánh blob riêng để dễ quản lý và ghi đè logic sau này.
            var blobServiceClient = new BlobServiceClient(_blobSettings.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blobSettings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Tên blob dùng timestamp để tránh trùng file khi tạo nhiều lần liên tiếp.
            var blobName = $"narration-audio/{narrationContentId}/{DateTime.UtcNow:yyyyMMddHHmmssfff}.mp3";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload audio bytes với content-type chuẩn cho file mp3.
            await using (var stream = new MemoryStream(result.AudioData))
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = "audio/mpeg"
                });
            }

            // Lấy duration nếu Azure trả về.
            // Thông tin này hữu ích để hiển thị thời lượng audio trong UI hoặc để kiểm tra nghiệp vụ.
            var durationSeconds = result.AudioDuration > TimeSpan.Zero
                ? (int?)Math.Round(result.AudioDuration.TotalSeconds)
                : null;

            _logger.LogInformation("Upload audio thành công - BlobName: {BlobName}", blobName);

            // Trả về URL public của blob + blobName (BlobId) + duration + voice thực tế đã dùng.
            return (blobClient.Uri.ToString(), blobName, durationSeconds, voiceName);
        }
    }
}
