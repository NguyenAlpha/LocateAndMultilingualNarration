using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.TtsVoiceProfiles;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/tts-voice-profiles")]
    public class TtsVoiceProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TtsVoiceProfileController> _logger;

        public TtsVoiceProfileController(AppDbContext context, ILogger<TtsVoiceProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách voice profile active theo language (public)
        /// </summary>
        /// <param name="languageId">Id của language</param>
        /// <returns>Danh sách voice profile</returns>
        /// <response code="200">Trả về danh sách voice profile</response>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveByLanguage([FromQuery] Guid? languageId = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách voice profile active - LanguageId: {LanguageId}", languageId);

            if (!languageId.HasValue)
            {
                return this.BadRequestResult("LanguageId là bắt buộc", "LanguageId");
            }

            var profiles = await _context.TtsVoiceProfiles
                .AsNoTracking()
                .Where(v => v.IsActive && v.LanguageId == languageId.Value)
                .OrderBy(v => v.Priority)
                .ThenBy(v => v.DisplayName)
                .ToListAsync();

            var result = profiles.Select(MapListItem).ToList();

            _logger.LogInformation("Lấy danh sách voice profile active thành công - TotalCount: {TotalCount}", result.Count);

            return this.OkResult(result);
        }

        private static TtsVoiceProfileListItemDto MapListItem(Api.Domain.Entities.TtsVoiceProfile profile)
        {
            return new TtsVoiceProfileListItemDto
            {
                Id = profile.Id,
                LanguageId = profile.LanguageId,
                DisplayName = profile.DisplayName,
                Description = profile.Description,
                Style = profile.Style,
                Role = profile.Role,
                IsDefault = profile.IsDefault,
                Priority = profile.Priority
            };
        }
    }
}
