using Api.Application.Services;
using Api.Domain.Entities;
using Api.Domain.Settings;
using Api.Infrastructure.Persistence;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/mobile")]
    public class MobileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageSettings _blobSettings;
        private readonly INarrationAudioService _narrationService;
        private readonly ILogger<MobileController> _logger;

        public MobileController(
            AppDbContext context,
            IOptions<BlobStorageSettings> blobSettings,
            INarrationAudioService narrationService,
            ILogger<MobileController> logger)
        {
            _context = context;
            _blobSettings = blobSettings.Value;
            _narrationService = narrationService;
            _logger = logger;
        }

        // Public endpoint: list active stall locations for mobile app
        [HttpGet("locations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLocations()
        {
            var items = await _context.StallLocations
                .Include(l => l.Stall)
                .Where(l => l.IsActive)
                .Select(l => new
                {
                    StallLocationId = l.Id,
                    StallId = l.StallId,
                    Name = l.Stall.Name,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Address = l.Address
                })
                .ToListAsync();

            return Ok(items);
        }

        // Mobile gọi endpoint này khi user bấm "Nghe thuyết minh" + chọn ngôn ngữ.
        // Trả về JSON nhẹ gồm scriptText + audioUrl đã sinh sẵn ở Blob Storage.
        // Mobile chỉ cần stream / download từ AudioUrl là phát được.
        // Không gọi Azure TTS realtime → nhanh, ổn định, không tốn quota.
        //
        // Query: /api/mobile/narration?stallId={guid}&lang={code}&voiceId={guid?}
        [HttpGet("narration")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNarration(
            [FromQuery] Guid stallId,
            [FromQuery] string? lang,
            [FromQuery] Guid? voiceId,
            CancellationToken cancellationToken = default)
        {
            if (stallId == Guid.Empty)
                return BadRequest(new { message = "stallId is required" });

            if (string.IsNullOrWhiteSpace(lang))
                return BadRequest(new { message = "lang is required" });

            // 1) Resolve Language theo mã (Code). Mobile gửi "vi" / "en" / "fr" / "zh" / "ko".
            var language = await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Code == lang, cancellationToken);

            if (language == null)
            {
                _logger.LogInformation("Narration: không tìm thấy ngôn ngữ {Lang}", lang);
                return NotFound(new { message = "Language not found" });
            }

            // 2) Tìm narration content cho stall + language.
            //    Ưu tiên bản đang Active, sau đó tới bản cập nhật gần nhất.
            var content = await _context.StallNarrationContents
                .AsNoTracking()
                .Include(c => c.NarrationAudios)
                .Where(c => c.StallId == stallId && c.LanguageId == language.Id)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (content == null)
            {
                _logger.LogInformation("Narration: chưa có content cho StallId {StallId}, Lang {Lang}", stallId, lang);
                return NotFound(new { message = "Narration content not found" });
            }

            // 3) Chọn audio theo thứ tự ưu tiên:
            //    - Khớp voiceId người dùng chọn (nếu có)
            //    - Bản TTS (IsTts = true)
            //    - Bất kỳ audio nào có URL hợp lệ
            var audios = content.NarrationAudios
                .Where(a => !string.IsNullOrWhiteSpace(a.AudioUrl))
                .ToList();

            NarrationAudio? selected = null;
            if (voiceId.HasValue)
                selected = audios.FirstOrDefault(a => a.TtsVoiceProfileId == voiceId.Value);
            selected ??= audios.FirstOrDefault(a => a.IsTts);
            selected ??= audios.FirstOrDefault();

            return Ok(new
            {
                NarrationContentId = content.Id,
                StallId = content.StallId,
                LanguageId = content.LanguageId,
                LanguageCode = language.Code,
                Title = content.Title,
                Description = content.Description,
                ScriptText = content.ScriptText,
                AudioUrl = selected?.AudioUrl,
                AudioId = selected?.Id,
                TtsVoiceProfileId = selected?.TtsVoiceProfileId,
                IsTts = selected?.IsTts ?? false,
                UpdatedAt = content.UpdatedAt
            });
        }

        // Public endpoint: return synthesized mp3 for a narration content.
        // Endpoint nặng — chỉ dùng khi audio chưa sinh sẵn ở Blob.
        // Query: /api/mobile/tts?narrationContentId={guid}  hoặc  ?stallId={guid}&lang={code}[&voiceId={guid}]
        [HttpGet("tts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTts(
            [FromQuery] Guid? narrationContentId,
            [FromQuery] Guid? stallId,
            [FromQuery] string? lang,
            [FromQuery] Guid? voiceId,
            CancellationToken cancellationToken = default)
        {
            if (narrationContentId == null && stallId == null)
                return BadRequest(new { message = "narrationContentId or stallId is required" });

            // Bước 1: Resolve narration content
            StallNarrationContent? content = null;
            if (narrationContentId.HasValue)
            {
                content = await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .FirstOrDefaultAsync(s => s.Id == narrationContentId.Value, cancellationToken);
            }
            else if (stallId.HasValue && !string.IsNullOrWhiteSpace(lang))
            {
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Code == lang, cancellationToken);
                if (language == null)
                    return NotFound(new { message = "Language not found" });

                content = await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .Where(s => s.StallId == stallId.Value && s.LanguageId == language.Id)
                    .OrderByDescending(s => s.IsActive)
                    .ThenByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (content == null)
                return NotFound(new { message = "Narration content not found" });

            // Bước 2: Tìm audio TTS đã có (ưu tiên đúng voice nếu được chọn)
            var audioQuery = _context.NarrationAudios
                .Where(a => a.NarrationContentId == content.Id
                            && a.IsTts
                            && !string.IsNullOrEmpty(a.BlobId));

            if (voiceId.HasValue)
                audioQuery = audioQuery.Where(a => a.TtsVoiceProfileId == voiceId.Value);

            var audio = await audioQuery
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Bước 3: Nếu chưa có audio thì sinh TTS rồi lưu
            //         (NarrationAudioService tự xử lý voice profile bên trong, không cần truyền voiceId)
            if (audio == null)
            {
                _logger.LogInformation("TTS: chưa có audio sẵn, sinh mới — ContentId {ContentId}", content.Id);
                var results = await _narrationService.CreateOrUpdateFromTtsAsync(
                    content.Id,
                    content.ScriptText,
                    content.LanguageId,
                    voice: null,
                    provider: null);

                audio = voiceId.HasValue
                    ? results.FirstOrDefault(a => a.TtsVoiceProfileId == voiceId.Value && !string.IsNullOrEmpty(a.BlobId))
                    : null;
                audio ??= results.FirstOrDefault(a => !string.IsNullOrEmpty(a.BlobId));
            }

            if (audio == null || string.IsNullOrWhiteSpace(audio.BlobId))
                return NotFound(new { message = "Audio not available" });

            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
                return StatusCode(500, new { message = "Blob storage not configured" });

            // Bước 4: Stream file mp3 từ Blob về cho client
            var blobClient = new BlobServiceClient(_blobSettings.ConnectionString)
                .GetBlobContainerClient(_blobSettings.ContainerName)
                .GetBlobClient(audio.BlobId);

            if (!await blobClient.ExistsAsync(cancellationToken))
                return NotFound(new { message = "Audio blob not found" });

            var dl = await blobClient.DownloadAsync(cancellationToken);
            return File(dl.Value.Content, dl.Value.Details.ContentType ?? "audio/mpeg");
        }
    }
}
