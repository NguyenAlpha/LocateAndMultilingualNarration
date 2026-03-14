using Shared.DTOs.Users;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Khởi tạo UserController với các dependencies cần thiết
        /// </summary>
        /// <param name="context">Database context để truy vấn dữ liệu</param>
        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của user theo Id
        /// </summary>
        /// <param name="id">Id của user cần lấy thông tin</param>
        /// <returns>Thông tin user, roles và profile tương ứng</returns>
        /// <response code="200">Trả về chi tiết user</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy user</response>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết user - Id: {UserId}", id);

            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdValue, out var currentUserId))
            {
                _logger.LogWarning("Không xác thực khi lấy chi tiết user - Id: {UserId}", id);
                return this.UnauthorizedResult("Không xác thực");
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("ADMIN");
            if (!isAdmin && currentUserId != id)
            {
                _logger.LogWarning("Không có quyền truy cập chi tiết user - Id: {UserId}", id);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            _logger.LogDebug("Truy vấn user và profile - Id: {UserId}", id);
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.BusinessOwnerProfile)
                .Include(u => u.VisitorProfile)
                .Include(u => u.EmployeeProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy user - Id: {UserId}", id);
                return this.NotFoundResult("Không tìm thấy user");
            }

            var roles = user.UserRoles
                .Select(ur => ur.Role.Name)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r!)
                .ToList();

            var timeZoneId = HttpContext.Request.Headers["X-TimeZoneId"].ToString();
            var timeZone = string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            _logger.LogDebug("Múi giờ dùng để trả về DTO - TimeZoneId: {TimeZoneId}", string.IsNullOrWhiteSpace(timeZoneId) ? "SE Asia Standard Time" : timeZoneId);

            var response = new UserDetailDto
            {
                Id = user.Id,
                UserName = user.UserName,
                NormalizedUserName = user.NormalizedUserName,
                Email = user.Email,
                NormalizedEmail = user.NormalizedEmail,
                EmailConfirmed = user.EmailConfirmed,
                PasswordHash = user.PasswordHash,
                SecurityStamp = user.SecurityStamp,
                ConcurrencyStamp = user.ConcurrencyStamp,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                DisplayName = user.DisplayName,
                Sex = user.Sex,
                DateOfBirth = ConvertFromUtc(user.DateOfBirth, timeZone),
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = ConvertFromUtc(user.LockoutEnd, timeZone),
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                LastLoginAt = ConvertFromUtc(user.LastLoginAt, timeZone),
                CreatedAt = ConvertFromUtc(user.CreatedAt, timeZone),
                UpdatedAt = ConvertFromUtc(user.UpdatedAt, timeZone),
                DeletedAt = ConvertFromUtc(user.DeletedAt, timeZone),
                IsActive = user.IsActive,
                Roles = roles,
                BusinessOwnerProfile = user.BusinessOwnerProfile == null ? null : new BusinessOwnerProfileDto
                {
                    Id = user.BusinessOwnerProfile.Id,
                    UserId = user.BusinessOwnerProfile.UserId,
                    OwnerName = user.BusinessOwnerProfile.OwnerName,
                    ContactInfo = user.BusinessOwnerProfile.ContactInfo,
                    CreatedAt = ConvertFromUtc(user.BusinessOwnerProfile.CreatedAt, timeZone)
                },
                VisitorProfile = user.VisitorProfile == null ? null : new VisitorProfileDto
                {
                    Id = user.VisitorProfile.Id,
                    UserId = user.VisitorProfile.UserId,
                    LanguageId = user.VisitorProfile.LanguageId,
                    CreatedAt = ConvertFromUtc(user.VisitorProfile.CreatedAt, timeZone)
                },
                EmployeeProfile = user.EmployeeProfile == null ? null : new EmployeeProfileDto
                {
                    Id = user.EmployeeProfile.Id,
                    UserId = user.EmployeeProfile.UserId,
                    Department = user.EmployeeProfile.Department,
                    Position = user.EmployeeProfile.Position,
                    CreatedAt = ConvertFromUtc(user.EmployeeProfile.CreatedAt, timeZone)
                }
            };

            _logger.LogInformation("Lấy chi tiết user thành công - Id: {UserId}", id);
            return this.OkResult(response);
        }

        private static DateTime ConvertFromUtc(DateTime utcDateTime, TimeZoneInfo timeZone)
        {
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }

        private static DateTime? ConvertFromUtc(DateTime? utcDateTime, TimeZoneInfo timeZone)
        {
            if (utcDateTime == null)
            {
                return null;
            }

            return ConvertFromUtc(utcDateTime.Value, timeZone);
        }

        private static DateTimeOffset? ConvertFromUtc(DateTimeOffset? utcDateTime, TimeZoneInfo timeZone)
        {
            if (utcDateTime == null)
            {
                return null;
            }

            return ConvertFromUtc(utcDateTime.Value, timeZone);
        }

        private static DateTimeOffset ConvertFromUtc(DateTimeOffset utcDateTime, TimeZoneInfo timeZone)
        {
            var utc = utcDateTime.UtcDateTime;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var offset = timeZone.GetUtcOffset(utc);
            return new DateTimeOffset(local, offset);
        }
    }
}
