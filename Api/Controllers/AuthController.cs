using Shared.DTOs.Auth;
using Api.Application.Services;
using Api.Domain.Entities;
using Api.Domain.Settings;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng xác thực và đăng ký người dùng
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : AppControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtSettings _jwtSettings;
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

        /// <summary>
        /// Khởi tạo AuthController với các dependencies cần thiết
        /// </summary>
        /// <param name="jwtService">Service để tạo và xác thực JWT token</param>
        /// <param name="context">Database context để truy vấn dữ liệu</param>
        /// <param name="logger">Logger để ghi log</param>
        public AuthController(IJwtService jwtService, AppDbContext context, ILogger<AuthController> logger, IOptions<JwtSettings> jwtSettings)
        {
            _jwtService = jwtService;
            _context = context;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// Đăng ký tài khoản mới cho Business Owner
        /// </summary>
        /// <param name="request">Thông tin đăng ký bao gồm username, email, password, phone number</param>
        /// <returns>Thông tin user đã đăng ký thành công hoặc lỗi nếu email/username đã tồn tại</returns>
        /// <response code="200">Đăng ký thành công</response>
        /// <response code="400">Email hoặc username đã được sử dụng</response>
        [HttpPost("register/business-owner")]
        public async Task<IActionResult> RegisterBusinessOwner([FromBody] RegisterBusinessOwnerDto request)
        {
            _logger.LogInformation("Bắt đầu đăng ký Business Owner - Email: {Email}, UserName: {UserName}", 
                request.Email, request.UserName);

            try
            {
                // 1. Validate Email đã tồn tại chưa
                _logger.LogDebug("Kiểm tra email đã tồn tại: {Email}", request.Email);
                var existingEmail = await _context.Users.EmailExistsAsync(request.Email);

                if (existingEmail)
                {
                    _logger.LogWarning("Email đã được sử dụng: {Email}", request.Email);
                    return this.ConflictResult("Email đã được sử dụng", "Email");
                }

                // 2. Validate UserName đã tồn tại chưa
                _logger.LogDebug("Kiểm tra username đã tồn tại: {UserName}", request.UserName);
                var existingUserName = await _context.Users.UserNameExistsAsync(request.UserName);

                if (existingUserName)
                {
                    _logger.LogWarning("UserName đã được sử dụng: {UserName}", request.UserName);
                    return this.ConflictResult("UserName đã được sử dụng", "UserName");
                }

                // 3. Hash password
                _logger.LogDebug("Hash password cho user: {UserName}", request.UserName);
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 4. Tạo User mới
                _logger.LogDebug("Tạo User entity mới");
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = request.UserName,
                    NormalizedUserName = request.UserName.ToUpper(),
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpper(),
                    PasswordHash = passwordHash,
                    PhoneNumber = request.PhoneNumber,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 5. Tạo BusinessOwnerProfile
                _logger.LogDebug("Tạo BusinessOwnerProfile cho UserId: {UserId}", user.Id);
                var businessOwnerProfile = new BusinessOwnerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // 6. Gán Role BusinessOwner
                // 6.1. Tìm role BusinessOwner trong database
                _logger.LogDebug("Tìm role BusinessOwner trong database");
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.NormalizedName == "BUSINESSOWNER");

                if (role == null)
                {
                    // 6.2. Tạo role nếu chưa có (seed data)
                    _logger.LogInformation("Role BusinessOwner chưa tồn tại, tạo mới");
                    role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "BusinessOwner",
                        NormalizedName = "BUSINESSOWNER",
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã tạo role BusinessOwner thành công");
                }

                // 6.3. Tạo quan hệ User-Role
                _logger.LogDebug("Tạo quan hệ User-Role");
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };

                // 7. Lưu vào database
                _logger.LogDebug("Bắt đầu lưu dữ liệu vào database");
                _context.Users.Add(user);
                _context.BusinessOwnerProfiles.Add(businessOwnerProfile);
                _context.UserRoles.Add(userRole);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Đăng ký Business Owner thành công - UserId: {UserId}, Email: {Email}", 
                    user.Id, user.Email);

                // 8. Trả về response
                return this.OkResult(new RegisterResponseDto
                {
                    UserId = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    Message = "Đăng ký thành công! Bạn có thể đăng nhập để tạo doanh nghiệp và gian hàng."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi database khi đăng ký Business Owner - Email: {Email}", request.Email);
                return this.ServerErrorResult("Lỗi khi lưu dữ liệu. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi đăng ký Business Owner - Email: {Email}", request.Email);
                return this.ServerErrorResult("Có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        /// <param name="request">Thông tin đăng nhập bao gồm email và password</param>
        /// <returns>JWT token và thông tin user nếu đăng nhập thành công</returns>
        /// <response code="200">Đăng nhập thành công, trả về token và thông tin user</response>
        /// <response code="401">Email/password không đúng hoặc tài khoản bị vô hiệu hóa</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Bắt đầu đăng nhập cho user: {Email}", request.Email);
            
            try
            {
                // 1. Tìm user theo email
                _logger.LogDebug("Tìm user theo email: {Email}", request.Email);
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpper()
                                          || u.NormalizedUserName == request.Email.ToUpper());

                if (user == null)
                {
                    _logger.LogWarning("Đăng nhập thất bại - Không tìm thấy user cho email: {Email}", request.Email);
                    return this.UnauthorizedResult("Email hoặc mật khẩu không đúng");
                }

                // 2. Verify password bằng BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Đăng nhập thất bại - Sai mật khẩu cho email: {Email}", request.Email);
                    return this.UnauthorizedResult("Email hoặc mật khẩu không đúng");
                }

                // 3. Kiểm tra account status
                if (!user.IsActive)
                {
                    _logger.LogWarning("Đăng nhập thất bại - Tài khoản bị vô hiệu hóa: {Email}", request.Email);
                    return this.UnauthorizedResult("Tài khoản đã bị vô hiệu hóa");
                }

                // 4. Lấy danh sách roles của user từ navigation property
                var roles = user.UserRoles.Select(ur => ur.Role.Name).OfType<string>().ToList();

                // 5. Generate JWT token với thông tin user và roles
                var token = _jwtService.GenerateToken(user, roles);
                var (refreshToken, refreshTokenHash) = _jwtService.GenerateRefreshToken();
                var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

                // 6. Cập nhật thời gian đăng nhập gần nhất
                user.LastLoginAt = DateTime.UtcNow;

                var refreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = refreshTokenHash,
                    ExpiresAtUtc = refreshTokenExpiresAtUtc,
                    CreatedAtUtc = DateTime.UtcNow,
                    DeviceId = HttpContext.Request.Headers["X-DeviceId"].ToString(),
                    CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var timeZone = GetTimeZone();

                var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);
                var expiresAtLocal = ConvertFromUtc(expiresAtUtc, timeZone);
                var refreshTokenExpiresAtLocal = ConvertFromUtc(refreshTokenExpiresAtUtc, timeZone);

                _logger.LogInformation("Đăng nhập thành công - UserId: {UserId}, Email: {Email}", user.Id, user.Email);

                // 7. Trả về response với token và thông tin user
                return this.OkResult(new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAtLocal,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshTokenExpiresAtLocal,
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Đăng nhập thất bại cho email: {Email}", request.Email);
                return this.UnauthorizedResult("Email hoặc mật khẩu không đúng");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi đăng nhập: {Email}", request.Email);
                return this.ServerErrorResult("Có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            _logger.LogInformation("Bắt đầu refresh token");

            try
            {
                var tokenHash = _jwtService.HashRefreshToken(request.RefreshToken);
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token không hợp lệ");
                    return this.UnauthorizedResult("Refresh token không hợp lệ");
                }

                if (refreshToken.RevokedAtUtc != null || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token đã hết hạn hoặc bị thu hồi - TokenId: {TokenId}", refreshToken.Id);
                    return this.UnauthorizedResult("Refresh token đã hết hạn hoặc bị thu hồi");
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);

                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Refresh token thất bại - user không tồn tại hoặc bị vô hiệu hóa");
                    return this.UnauthorizedResult("Tài khoản đã bị vô hiệu hóa");
                }

                var roles = user.UserRoles.Select(ur => ur.Role.Name).OfType<string>().ToList();
                var token = _jwtService.GenerateToken(user, roles);
                var (newRefreshToken, newRefreshTokenHash) = _jwtService.GenerateRefreshToken();
                var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

                refreshToken.RevokedAtUtc = DateTime.UtcNow;

                var newRefreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = newRefreshTokenHash,
                    ExpiresAtUtc = refreshTokenExpiresAtUtc,
                    CreatedAtUtc = DateTime.UtcNow,
                    DeviceId = request.DeviceId,
                    CreatedByIp = string.IsNullOrWhiteSpace(request.ClientIp)
                        ? HttpContext.Connection.RemoteIpAddress?.ToString()
                        : request.ClientIp,
                    ReplacedByTokenId = refreshToken.Id
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                var timeZone = GetTimeZone();
                var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);
                var expiresAtLocal = ConvertFromUtc(expiresAtUtc, timeZone);
                var refreshTokenExpiresAtLocal = ConvertFromUtc(refreshTokenExpiresAtUtc, timeZone);

                _logger.LogInformation("Refresh token thành công - UserId: {UserId}", user.Id);

                return this.OkResult(new RefreshResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAtLocal,
                    RefreshToken = newRefreshToken,
                    RefreshTokenExpiresAt = refreshTokenExpiresAtLocal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi refresh token");
                return this.ServerErrorResult("Có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            _logger.LogInformation("Bắt đầu logout");

            try
            {
                var tokenHash = _jwtService.HashRefreshToken(request.RefreshToken);
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Logout - Refresh token không tồn tại, bỏ qua");
                    return this.OkResult(new LogoutResponseDto { Success = true, RevokedAt = null });
                }

                if (refreshToken.RevokedAtUtc == null)
                {
                    refreshToken.RevokedAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var timeZone = GetTimeZone();
                var revokedAtLocal = ConvertFromUtc(refreshToken.RevokedAtUtc!.Value, timeZone);

                _logger.LogInformation("Logout thành công - TokenId: {TokenId}", refreshToken.Id);

                return this.OkResult(new LogoutResponseDto
                {
                    Success = true,
                    RevokedAt = revokedAtLocal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi logout");
                return this.ServerErrorResult("Có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

    }
}