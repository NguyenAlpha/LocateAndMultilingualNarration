using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.VisitorPreferences;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/visitor-preference")]
    [Authorize]
    public class VisitorPreferenceController : AppControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VisitorPreferenceController> _logger;

        public VisitorPreferenceController(AppDbContext context, ILogger<VisitorPreferenceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetDetail()
        {
            _logger.LogInformation("Bắt đầu lấy visitor preference");

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var preference = await _context.VisitorPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preference == null)
            {
                return this.NotFoundResult("Không tìm thấy visitor preference");
            }

            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(preference, timeZone));
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] VisitorPreferenceUpsertDto request)
        {
            _logger.LogInformation("Bắt đầu upsert visitor preference");

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var languageExists = await _context.Languages
                .AsNoTracking()
                .AnyAsync(l => l.Id == request.LanguageId && l.IsActive);

            if (!languageExists)
            {
                return this.NotFoundResult("Không tìm thấy language", "LanguageId");
            }

            var preference = await _context.VisitorPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preference == null)
            {
                preference = new Api.Domain.Entities.VisitorPreference
                {
                    UserId = userId,
                    LanguageId = request.LanguageId,
                    Voice = request.Voice,
                    SpeechRate = request.SpeechRate,
                    AutoPlay = request.AutoPlay,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.VisitorPreferences.Add(preference);
            }
            else
            {
                preference.LanguageId = request.LanguageId;
                preference.Voice = request.Voice;
                preference.SpeechRate = request.SpeechRate;
                preference.AutoPlay = request.AutoPlay;
                preference.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();

            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(preference, timeZone));
        }

        private static VisitorPreferenceDetailDto MapDetail(Api.Domain.Entities.VisitorPreference preference, TimeZoneInfo timeZone)
        {
            return new VisitorPreferenceDetailDto
            {
                Id = preference.Id,
                UserId = preference.UserId,
                LanguageId = preference.LanguageId,
                Voice = preference.Voice,
                SpeechRate = preference.SpeechRate,
                AutoPlay = preference.AutoPlay,
                UpdatedAt = ConvertFromUtc(preference.UpdatedAt, timeZone)
            };
        }

    }
}
