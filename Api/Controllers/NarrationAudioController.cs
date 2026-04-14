using Api.Authorization;
using Api.Domain.Entities;
using Api.Domain.Settings;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.DTOs.Common;
using Shared.DTOs.Narrations;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/narration-audio")]
    [Authorize]
    public class NarrationAudioController : AppControllerBase
    {
        private static readonly HashSet<string> AllowedAudioContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "audio/mpeg", "audio/mp3", "audio/wav", "audio/wave", "audio/x-wav",
            "audio/ogg", "audio/aac", "audio/flac", "audio/webm"
        };

        private readonly AppDbContext _context;
        private readonly BlobStorageSettings _blobSettings;
        private readonly ILogger<NarrationAudioController> _logger;

        public NarrationAudioController(
            AppDbContext context,
            IOptions<BlobStorageSettings> blobSettings,
            ILogger<NarrationAudioController> logger)
        {
            _context = context;
            _blobSettings = blobSettings.Value;
            _logger = logger;
        }

        [HttpPut("{id:guid}/upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> UploadHumanAudio(Guid id, IFormFile audioFile, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bắt đầu upload audio giọng người cho NarrationAudioId: {Id}", id);

            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            var audio = await _context.NarrationAudios
                .Include(a => a.NarrationContent)
                    .ThenInclude(nc => nc.Stall)
                        .ThenInclude(s => s.Business)
                .Include(a => a.TtsVoiceProfile)
                    .ThenInclude(p => p!.Language)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (audio == null)
                return this.NotFoundResult("Không tìm thấy narration audio");

            if (!IsAdmin() && audio.NarrationContent.Stall.Business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            if (audioFile == null || audioFile.Length == 0)
                return BadRequest(ApiResult<NarrationAudioDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.Validation,
                    Message = "File audio là bắt buộc"
                }));

            if (!AllowedAudioContentTypes.Contains(audioFile.ContentType))
                return BadRequest(ApiResult<NarrationAudioDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.Validation,
                    Message = $"Định dạng file không hợp lệ: {audioFile.ContentType}. Chỉ chấp nhận file audio (mp3, wav, ogg, aac, flac)."
                }));

            string audioUrl;
            string blobId;

            try
            {
                (audioUrl, blobId) = await UploadAudioToBlobAsync(audioFile, audio.NarrationContentId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<NarrationAudioDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.ServerError,
                    Message = ex.Message
                }));
            }

            audio.AudioUrl = audioUrl;
            audio.BlobId = blobId;
            audio.IsTts = false;
            audio.Voice = null;
            audio.Provider = "Human";
            audio.DurationSeconds = null;
            audio.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cập nhật audio giọng người thành công - NarrationAudioId: {Id}", id);

            var timeZone = GetTimeZone();
            return this.OkResult(MapAudioDetail(audio, timeZone));
        }

        private async Task<(string audioUrl, string blobId)> UploadAudioToBlobAsync(IFormFile audioFile, Guid narrationContentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
                throw new InvalidOperationException("Thiếu cấu hình Blob Storage (ConnectionString/ContainerName).");

            var ext = Path.GetExtension(audioFile.FileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".mp3";

            var blobName = $"narration-audio/{narrationContentId}/{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";

            var containerClient = new BlobServiceClient(_blobSettings.ConnectionString)
                .GetBlobContainerClient(_blobSettings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);
            await using var stream = audioFile.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = audioFile.ContentType }, cancellationToken: cancellationToken);

            _logger.LogInformation("Upload audio thành công - BlobName: {BlobName}", blobName);

            return (blobClient.Uri.ToString(), blobName);
        }

        private static NarrationAudioDetailDto MapAudioDetail(NarrationAudio audio, TimeZoneInfo timeZone)
        {
            return new NarrationAudioDetailDto
            {
                Id = audio.Id,
                NarrationContentId = audio.NarrationContentId,
                TtsVoiceProfileId = audio.TtsVoiceProfileId,
                TtsVoiceProfileDisplayName = audio.TtsVoiceProfile?.DisplayName,
                TtsVoiceProfileDescription = audio.TtsVoiceProfile?.Description,
                TtsVoiceProfileLanguageName = audio.TtsVoiceProfile?.Language?.Name,
                AudioUrl = audio.AudioUrl,
                BlobId = audio.BlobId,
                Voice = audio.Voice,
                Provider = audio.Provider,
                DurationSeconds = audio.DurationSeconds,
                IsTts = audio.IsTts,
                UpdatedAt = ConvertFromUtc(audio.UpdatedAt, timeZone)
            };
        }
    }
}
