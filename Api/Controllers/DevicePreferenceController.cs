using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.DevicePreferences;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/device-preference")]
    [AllowAnonymous]
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
        public async Task<IActionResult> GetByDeviceId(string deviceId)
        {
            _logger.LogInformation("Lấy device preference cho deviceId: {DeviceId}", deviceId);

            var preference = await _context.DevicePreferences
                .AsNoTracking()
                .Include(x => x.Language)
                .FirstOrDefaultAsync(x => x.DeviceId == deviceId);

            if (preference is null)
                return this.NotFoundResult("Không tìm thấy device preference");

            return this.OkResult(MapToDetail(preference));
        }

        [HttpPost("/api/device-preferences")]
        public async Task<IActionResult> Save([FromBody] DevicePreferencesRequest request)
        {
            _logger.LogInformation("Lưu device preferences cho deviceId: {DeviceId}", request.DeviceId);

            var language = await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == request.LanguageId && l.IsActive);

            if (language is null)
                return this.BadRequestResult($"Không tìm thấy ngôn ngữ với id '{request.LanguageId}'", "LanguageId");

            var now = DateTimeOffset.UtcNow;

            var existing = await _context.DevicePreferences
                .FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId);

            if (existing != null)
            {
                existing.LanguageId = request.LanguageId;
                existing.Voice = request.Voice;
                existing.SpeechRate = request.SpeechRate;
                existing.AutoPlay = request.AutoPlay;
                existing.Platform = request.Platform;
                existing.DeviceModel = request.DeviceModel;
                existing.Manufacturer = request.Manufacturer;
                existing.OsVersion = request.OsVersion;
                existing.LastSeenAt = now;
            }
            else
            {
                var entity = new Api.Domain.Entities.DevicePreference
                {
                    DeviceId = request.DeviceId,
                    LanguageId = request.LanguageId,
                    Voice = request.Voice,
                    SpeechRate = request.SpeechRate,
                    AutoPlay = request.AutoPlay,
                    Platform = request.Platform,
                    DeviceModel = request.DeviceModel,
                    Manufacturer = request.Manufacturer,
                    OsVersion = request.OsVersion,
                    FirstSeenAt = now,
                    LastSeenAt = now
                };

                _context.DevicePreferences.Add(entity);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] DevicePreferenceUpsertDto request)
        {
            _logger.LogInformation("Upsert device preference cho deviceId: {DeviceId}", request.DeviceId);

            var language = await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Code == request.LanguageCode && l.IsActive);

            if (language is null)
                return this.BadRequestResult($"Không tìm thấy ngôn ngữ với code '{request.LanguageCode}'", "LanguageCode");

            var now = DateTimeOffset.UtcNow;

            var preference = await _context.DevicePreferences
                .Include(x => x.Language)
                .FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId);

            if (preference is null)
            {
                preference = new Api.Domain.Entities.DevicePreference
                {
                    DeviceId     = request.DeviceId,
                    LanguageId   = language.Id,
                    Voice        = request.Voice,
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
                preference.Voice        = request.Voice;
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

        private static DevicePreferenceDetailDto MapToDetail(Api.Domain.Entities.DevicePreference p) => new()
        {
            DeviceId     = p.DeviceId,
            LanguageCode = p.Language.Code,
            LanguageName = p.Language.DisplayName ?? p.Language.Name,
            Voice        = p.Voice,
            SpeechRate   = p.SpeechRate,
            AutoPlay     = p.AutoPlay,
            LastSeenAt   = p.LastSeenAt
        };
    }
}
