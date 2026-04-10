using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Users;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý VisitorProfile cho user đăng nhập
    /// </summary>
    [ApiController]
    [Route("api/visitor-profile")]
    [Authorize]
    public class VisitorProfileController : AppControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VisitorProfileController> _logger;

        /// <summary>
        /// Khởi tạo VisitorProfileController với các dependencies cần thiết
        /// </summary>
        /// <param name="context">Database context để truy vấn dữ liệu</param>
        /// <param name="logger">Logger để ghi log</param>
        public VisitorProfileController(AppDbContext context, ILogger<VisitorProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo visitor profile nếu chưa có
        /// </summary>
        /// <param name="request">Thông tin cập nhật language</param>
        /// <returns>VisitorProfile vừa tạo</returns>
        /// <response code="200">Tạo thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="404">Không tìm thấy language</response>
        [HttpPost]
        public async Task<IActionResult> CreateVisitorProfile([FromBody] VisitorProfileUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo visitor profile");

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi tạo visitor profile");
                return this.UnauthorizedResult("Không xác thực");
            }

            var existingProfile = await _context.VisitorProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (existingProfile != null)
            {
                _logger.LogWarning("Visitor profile đã tồn tại - UserId: {UserId}", userId);
                return this.ConflictResult("Visitor profile đã tồn tại");
            }

            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Id == request.LanguageId && l.IsActive);
            if (language == null)
            {
                _logger.LogWarning("Language không hợp lệ khi tạo visitor profile - LanguageId: {LanguageId}", request.LanguageId);
                return this.NotFoundResult("Không tìm thấy language");
            }

            var profile = new Api.Domain.Entities.VisitorProfile
            {
                UserId = userId,
                LanguageId = request.LanguageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.VisitorProfiles.Add(profile);
            await _context.SaveChangesAsync();

            var timeZone = GetTimeZone();

            _logger.LogInformation("Tạo visitor profile thành công - ProfileId: {ProfileId}", profile.Id);

            return this.OkResult(MapVisitorProfile(profile, timeZone));
        }

        /// <summary>
        /// Cập nhật language cho visitor profile
        /// </summary>
        /// <param name="request">Thông tin cập nhật language</param>
        /// <returns>VisitorProfile sau khi cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="404">Không tìm thấy language hoặc profile</response>
        [HttpPut]
        public async Task<IActionResult> UpdateVisitorProfile([FromBody] VisitorProfileUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật visitor profile");

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi cập nhật visitor profile");
                return this.UnauthorizedResult("Không xác thực");
            }

            var profile = await _context.VisitorProfiles.FirstOrDefaultAsync(v => v.UserId == userId);
            if (profile == null)
            {
                _logger.LogWarning("Không tìm thấy visitor profile - UserId: {UserId}", userId);
                return this.NotFoundResult("Không tìm thấy visitor profile");
            }

            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Id == request.LanguageId && l.IsActive);
            if (language == null)
            {
                _logger.LogWarning("Language không hợp lệ khi cập nhật visitor profile - LanguageId: {LanguageId}", request.LanguageId);
                return this.NotFoundResult("Không tìm thấy language");
            }

            profile.LanguageId = request.LanguageId;
            await _context.SaveChangesAsync();

            var timeZone = GetTimeZone();

            _logger.LogInformation("Cập nhật visitor profile thành công - ProfileId: {ProfileId}", profile.Id);

            return this.OkResult(MapVisitorProfile(profile, timeZone));
        }

        private static VisitorProfileDto MapVisitorProfile(Api.Domain.Entities.VisitorProfile profile, TimeZoneInfo timeZone)
        {
            return new VisitorProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                LanguageId = profile.LanguageId,
                CreatedAt = ConvertFromUtc(profile.CreatedAt, timeZone)
            };
        }

    }
}
