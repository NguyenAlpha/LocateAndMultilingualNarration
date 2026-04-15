using Api.Authorization;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.Users;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : AppControllerBase
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

        private const int MaxPageSize = 100;

        /// <summary>
        /// Lấy danh sách tất cả roles với số user được gán.
        /// Đặt trước GET {id} để tránh route conflict.
        /// </summary>
        [HttpGet("roles")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new RoleListItemDto
                {
                    Id = r.Id,
                    Name = r.Name!,
                    UserCount = r.UserRoles.Count
                })
                .ToListAsync();

            return this.OkResult(roles);
        }

        /// <summary>
        /// Lấy danh sách user có phân trang và filter. Chỉ Admin.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var upper = search.ToUpper();
                query = query.Where(u =>
                    (u.NormalizedEmail != null && u.NormalizedEmail.Contains(upper)) ||
                    (u.NormalizedUserName != null && u.NormalizedUserName.Contains(upper)));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var upper = role.ToUpper();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.NormalizedName == upper));
            }

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var timeZone = GetTimeZone();
            var items = users.Select(u => MapListItem(u, timeZone)).ToList();

            return this.OkResult(new PagedResult<UserListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        /// <summary>
        /// Admin tạo user mới với bất kỳ role nào.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> AdminCreateUser([FromBody] AdminCreateUserDto request)
        {
            if (await _context.Users.EmailExistsAsync(request.Email))
                return this.ConflictResult("Email đã được sử dụng", "Email");

            if (await _context.Users.UserNameExistsAsync(request.UserName))
                return this.ConflictResult("UserName đã được sử dụng", "UserName");

            var validRoles = new[] { "Admin", "BusinessOwner" };
            if (!validRoles.Contains(request.RoleName, StringComparer.OrdinalIgnoreCase))
                return this.BadRequestResult("Role không hợp lệ. Chỉ chấp nhận: Admin, BusinessOwner", "RoleName");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new Api.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                NormalizedUserName = request.UserName.ToUpper(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            var roleName = validRoles.First(r => r.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase));
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpper());
            if (role == null)
            {
                role = new Api.Domain.Entities.Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };
                _context.Roles.Add(role);
            }

            _context.Users.Add(user);
            _context.UserRoles.Add(new Api.Domain.Entities.UserRole { UserId = user.Id, RoleId = role.Id });
            await _context.SaveChangesAsync();

            var timeZone = GetTimeZone();
            return this.OkResult(MapListItem(user, timeZone, [roleName]));
        }

        /// <summary>
        /// Bật/tắt trạng thái IsActive của user. Admin không thể toggle bản thân.
        /// </summary>
        [HttpPut("{id:guid}/toggle-active")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> ToggleUserActive(Guid id)
        {
            if (!TryGetUserId(out var currentUserId))
                return this.UnauthorizedResult("Không xác thực");

            if (currentUserId == id)
                return this.BadRequestResult("Không thể tự thay đổi trạng thái của bản thân", "Id");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return this.NotFoundResult("Không tìm thấy user");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return this.OkResult(new { user.Id, user.IsActive });
        }

        /// <summary>
        /// Đổi role của user. Xoá tất cả role cũ, gán role mới.
        /// </summary>
        [HttpPut("{id:guid}/role")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UserRoleUpdateDto request)
        {
            var validRoles = new[] { "Admin", "BusinessOwner" };
            if (!validRoles.Contains(request.RoleName, StringComparer.OrdinalIgnoreCase))
                return this.BadRequestResult("Role không hợp lệ. Chỉ chấp nhận: Admin, BusinessOwner", "RoleName");

            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return this.NotFoundResult("Không tìm thấy user");

            var roleName = validRoles.First(r => r.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase));
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpper());
            if (role == null)
                return this.NotFoundResult("Không tìm thấy role");

            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.UserRoles.Add(new Api.Domain.Entities.UserRole { UserId = user.Id, RoleId = role.Id });
            await _context.SaveChangesAsync();

            var timeZone = GetTimeZone();
            return this.OkResult(MapListItem(user, timeZone, [roleName]));
        }

        private static UserListItemDto MapListItem(
            Api.Domain.Entities.User user,
            TimeZoneInfo timeZone,
            List<string>? rolesOverride = null)
        {
            var roles = rolesOverride
                ?? user.UserRoles
                    .Select(ur => ur.Role?.Name)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r!)
                    .ToList();

            return new UserListItemDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles,
                IsActive = user.IsActive,
                LastLoginAt = ConvertFromUtc(user.LastLoginAt, timeZone),
                CreatedAt = ConvertFromUtc(user.CreatedAt, timeZone)
            };
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

            if (!TryGetUserId(out var currentUserId))
            {
                _logger.LogWarning("Không xác thực khi lấy chi tiết user - Id: {UserId}", id);
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && currentUserId != id)
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

            var timeZone = GetTimeZone();

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

    }
}
