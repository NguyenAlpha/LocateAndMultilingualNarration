using Api.Authorization;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.DevicePreferences;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/device-preference")]
    public class DevicePreferenceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DevicePreferenceController> _logger;

        public DevicePreferenceController(AppDbContext context, ILogger<DevicePreferenceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{deviceId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDeviceId(string deviceId)
        {
            _logger.LogInformation("Lấy device preference cho deviceId: {DeviceId}", deviceId);

            var preference = await _context.DevicePreferences
                .AsNoTracking()
                .Include(x => x.Language)
                .Include(x => x.VoiceProfile)
                .FirstOrDefaultAsync(x => x.DeviceId == deviceId);

            if (preference is null)
                return this.NotFoundResult("Không tìm thấy device preference");

            return this.OkResult(MapToDetail(preference));
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Upsert([FromBody] DevicePreferenceUpsertDto request)
        {
            _logger.LogInformation("Upsert device preference cho deviceId: {DeviceId}", request.DeviceId);

            var language = await _context.Languages.GetActiveByIdAsync(request.LanguageId);

            if (language is null)
                return this.BadRequestResult($"Không tìm thấy ngôn ngữ với id '{request.LanguageId}'", "LanguageId");

            if (request.VoiceId.HasValue)
            {
                var voiceExists = await _context.TtsVoiceProfiles.IsActiveByIdAsync(request.VoiceId.Value);

                if (!voiceExists)
                    return this.BadRequestResult($"Không tìm thấy giọng đọc với id '{request.VoiceId}'", "VoiceId");
            }

            var now = DateTimeOffset.UtcNow;

            var preference = await _context.DevicePreferences
                .Include(x => x.Language)
                .Include(x => x.VoiceProfile)
                .FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId);

            if (preference is null)
            {
                preference = new Api.Domain.Entities.DevicePreference
                {
                    DeviceId     = request.DeviceId,
                    LanguageId   = language.Id,
                    VoiceId      = request.VoiceId,
                    SpeechRate   = request.SpeechRate,
                    AutoPlay     = request.AutoPlay,
                    Platform     = request.Platform,
                    DeviceModel  = request.DeviceModel,
                    Manufacturer = request.Manufacturer,
                    OsVersion    = request.OsVersion,
                    FirstSeenAt  = now,
                    LastSeenAt   = now
                };
                _context.DevicePreferences.Add(preference);
            }
            else
            {
                preference.LanguageId   = language.Id;
                preference.VoiceId      = request.VoiceId;
                preference.SpeechRate   = request.SpeechRate;
                preference.AutoPlay     = request.AutoPlay;
                preference.Platform     = request.Platform;
                preference.DeviceModel  = request.DeviceModel;
                preference.Manufacturer = request.Manufacturer;
                preference.OsVersion    = request.OsVersion;
                preference.LastSeenAt   = now;
            }

            await _context.SaveChangesAsync();

            // reload language nếu vừa tạo mới
            preference.Language = language;

            return this.OkResult(MapToDetail(preference));
        }

        [HttpGet("reset-flag")]
        [AllowAnonymous]
        public async Task<IActionResult> GetResetFlag([FromQuery] string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return this.BadRequestResult("deviceId là bắt buộc", "deviceId");

            var preference = await _context.DevicePreferences
                .FirstOrDefaultAsync(x => x.DeviceId == deviceId);

            if (preference is null)
                return this.OkResult(false);

            var needsReset = preference.NeedsReset;
            if (needsReset)
            {
                await _context.DevicePreferences
                    .Where(x => x.DeviceId == deviceId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.NeedsReset, false));
            }

            return this.OkResult(needsReset);
        }

        // Set LastSeenAt về MinValue để admin dashboard loại thiết bị này khỏi danh sách active ngay lập tức,
        // thay vì chờ hết cửa sổ 30 giây.
        [HttpPost("{deviceId}/offline")]
        [AllowAnonymous]
        public async Task<IActionResult> NotifyOffline(string deviceId)
        {
            await _context.DevicePreferences
                .Where(x => x.DeviceId == deviceId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, DateTimeOffset.MinValue));

            return this.OkResult(true);
        }

        [HttpPost("{deviceId}/reset")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> RequestReset(string deviceId)
        {
            var updated = await _context.DevicePreferences
                .Where(x => x.DeviceId == deviceId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.NeedsReset, true));

            if (updated == 0)
                return this.NotFoundResult("Không tìm thấy thiết bị");

            _logger.LogInformation("Admin yêu cầu reset thiết bị: {DeviceId}", deviceId);
            return this.OkResult(true);
        }

        private static DevicePreferenceDetailDto MapToDetail(Api.Domain.Entities.DevicePreference p) => new()
        {
            Id                  = p.Id,
            DeviceId            = p.DeviceId,
            Platform            = p.Platform,
            DeviceModel         = p.DeviceModel,
            Manufacturer        = p.Manufacturer,
            OsVersion           = p.OsVersion,
            LanguageId          = p.LanguageId,
            LanguageCode        = p.Language.Code,
            LanguageName        = p.Language.Name,
            LanguageDisplayName = p.Language.DisplayName,
            LanguageFlagCode    = p.Language.FlagCode,
            VoiceId             = p.VoiceId,
            VoiceDisplayName    = p.VoiceProfile?.DisplayName,
            SpeechRate          = p.SpeechRate,
            AutoPlay            = p.AutoPlay,
            FirstSeenAt         = p.FirstSeenAt,
            LastSeenAt          = p.LastSeenAt
        };
    }
}
