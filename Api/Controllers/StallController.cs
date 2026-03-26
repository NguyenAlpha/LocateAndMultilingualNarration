using System.Security.Claims;
using System.Text.RegularExpressions;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.Stalls;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý Stall cho BusinessOwner/Admin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StallController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<StallController> _logger;

        /// <summary>
        /// Khởi tạo StallController với các dependencies cần thiết
        /// </summary>
        /// <param name="context">Database context để truy vấn dữ liệu</param>
        /// <param name="logger">Logger để ghi log</param>
        public StallController(AppDbContext context, ILogger<StallController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới stall cho business
        /// </summary>
        /// <param name="request">Thông tin tạo stall</param>
        /// <returns>Stall vừa tạo</returns>
        /// <response code="200">Tạo thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy business</response>
        [HttpPost]
        public async Task<IActionResult> CreateStall([FromBody] StallCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo stall - Name: {Name}", request.Name);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi tạo stall");
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                _logger.LogWarning("Không có quyền tạo stall - UserId: {UserId}", userId);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            _logger.LogDebug("Truy vấn business - BusinessId: {BusinessId}", request.BusinessId);
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == request.BusinessId);
            if (business == null)
            {
                _logger.LogWarning("Không tìm thấy business - BusinessId: {BusinessId}", request.BusinessId);
                return this.NotFoundResult("Không tìm thấy business");
            }

            if (!IsAdmin() && business.OwnerUserId != userId)
            {
                _logger.LogWarning("Không có quyền tạo stall cho business - BusinessId: {BusinessId}", request.BusinessId);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var slug = NormalizeSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Slug không hợp lệ khi tạo stall - Name: {Name}", request.Name);
                return this.BadRequestResult("Slug không hợp lệ", "Slug");
            }

            var slugExists = await _context.Stalls.AnyAsync(s => s.Slug == slug);
            if (slugExists)
            {
                _logger.LogWarning("Slug đã tồn tại khi tạo stall - Slug: {Slug}", slug);
                return this.ConflictResult("Slug đã tồn tại", "Slug");
            }

            var stall = new Api.Domain.Entities.Stall
            {
                BusinessId = request.BusinessId,
                Name = request.Name,
                Description = request.Description,
                Slug = slug,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Stalls.Add(stall);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tạo stall thành công - StallId: {StallId}", stall.Id);

            var timeZone = GetTimeZone();

            return this.OkResult(MapStallDetail(stall, timeZone));
        }

        /// <summary>
        /// Cập nhật stall theo Id
        /// </summary>
        /// <param name="id">Id của stall</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Stall sau khi cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy stall</response>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateStall(Guid id, [FromBody] StallUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật stall - Id: {StallId}", id);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi cập nhật stall - Id: {StallId}", id);
                return this.UnauthorizedResult("Không xác thực");
            }

            _logger.LogDebug("Truy vấn stall - Id: {StallId}", id);
            var stall = await _context.Stalls
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stall == null)
            {
                _logger.LogWarning("Không tìm thấy stall - Id: {StallId}", id);
                return this.NotFoundResult("Không tìm thấy stall");
            }

            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
            {
                _logger.LogWarning("Không có quyền cập nhật stall - Id: {StallId}", id);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var normalizedSlug = NormalizeSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
            if (string.IsNullOrWhiteSpace(normalizedSlug))
            {
                _logger.LogWarning("Slug không hợp lệ khi cập nhật stall - Id: {StallId}", id);
                return this.BadRequestResult("Slug không hợp lệ", "Slug");
            }

            if (!string.Equals(stall.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase))
            {
                var slugExists = await _context.Stalls.AnyAsync(s => s.Slug == normalizedSlug && s.Id != id);
                if (slugExists)
                {
                    _logger.LogWarning("Slug đã tồn tại khi cập nhật stall - Slug: {Slug}", normalizedSlug);
                    return this.ConflictResult("Slug đã tồn tại", "Slug");
                }

                stall.Slug = normalizedSlug;
            }

            stall.Name = request.Name;
            stall.Description = request.Description;
            stall.ContactEmail = request.ContactEmail;
            stall.ContactPhone = request.ContactPhone;
            stall.IsActive = request.IsActive;
            stall.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cập nhật stall thành công - Id: {StallId}", id);

            var timeZone = GetTimeZone();

            return this.OkResult(MapStallDetail(stall, timeZone));
        }

        /// <summary>
        /// Lấy chi tiết stall theo Id
        /// </summary>
        /// <param name="id">Id của stall</param>
        /// <returns>Thông tin stall</returns>
        /// <response code="200">Trả về chi tiết stall</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy stall</response>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetStallDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết stall - Id: {StallId}", id);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi lấy chi tiết stall - Id: {StallId}", id);
                return this.UnauthorizedResult("Không xác thực");
            }

            _logger.LogDebug("Truy vấn stall - Id: {StallId}", id);
            var stall = await _context.Stalls
                .Include(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stall == null)
            {
                _logger.LogWarning("Không tìm thấy stall - Id: {StallId}", id);
                return this.NotFoundResult("Không tìm thấy stall");
            }

            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
            {
                _logger.LogWarning("Không có quyền truy cập stall - Id: {StallId}", id);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var timeZone = GetTimeZone();

            _logger.LogInformation("Lấy chi tiết stall thành công - Id: {StallId}", id);
            return this.OkResult(MapStallDetail(stall, timeZone));
        }

        /// <summary>
        /// Lấy danh sách stall có phân trang
        /// </summary>
        /// <param name="page">Trang hiện tại</param>
        /// <param name="pageSize">Số phần tử mỗi trang</param>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="businessId">Lọc theo business</param>
        /// <returns>Danh sách stall phân trang</returns>
        /// <response code="200">Trả về danh sách stall</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        [HttpGet]
        public async Task<IActionResult> GetStalls([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] Guid? businessId = null)
        {
            _logger.LogInformation("<<BEGIN>>Bắt đầu lấy danh sách stall - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            // Lấy userId từ claim, nếu không có -> trả về 401
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi lấy danh sách stall");
                return this.UnauthorizedResult("Không xác thực");
            }

            // Kiểm tra quyền: chỉ Admin hoặc BusinessOwner mới được truy cập
            if (!IsAdmin() && !IsBusinessOwner())
            {
                _logger.LogWarning("Không có quyền truy cập danh sách stall - UserId: {UserId}", userId);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Chuẩn hóa tham số phân trang
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            // Xây dựng truy vấn cơ bản (join Business để kiểm tra owner khi cần)
            var query = _context.Stalls
                .Include(s => s.Business)
                .AsNoTracking()
                .AsQueryable();

            // Nếu không phải Admin thì chỉ lấy các stall thuộc business của user
            if (!IsAdmin())
            {
                query = query.Where(s => s.Business.OwnerUserId == userId);
            }

            // Lọc theo businessId nếu có
            if (businessId.HasValue)
            {
                query = query.Where(s => s.BusinessId == businessId.Value);
            }

            // Tìm kiếm theo tên hoặc slug nếu có từ khóa
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(s => s.Name.Contains(keyword) || s.Slug.Contains(keyword));
            }

            // Đếm tổng số và lấy trang dữ liệu
            _logger.LogDebug("Truy vấn danh sách stall - UserId: {UserId}", userId);
            var totalCount = await query.CountAsync();
            var stalls = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Chuyển thời gian từ UTC sang timezone của client và map sang DTO
            var timeZone = GetTimeZone();
            var items = stalls.Select(s => MapStallDetail(s, timeZone)).ToList();

            // Gói kết quả theo mô hình phân trang và trả về
            var result = new PagedResult<StallDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            _logger.LogInformation("Lấy danh sách stall thành công - TotalCount: {TotalCount}<<END>>", totalCount);
            return this.OkResult(result);
        }

        private static StallDetailDto MapStallDetail(Api.Domain.Entities.Stall stall, TimeZoneInfo timeZone)
        {
            return new StallDetailDto
            {
                Id = stall.Id,
                BusinessId = stall.BusinessId,
                Name = stall.Name,
                Description = stall.Description,
                Slug = stall.Slug,
                ContactEmail = stall.ContactEmail,
                ContactPhone = stall.ContactPhone,
                IsActive = stall.IsActive,
                CreatedAt = ConvertFromUtc(stall.CreatedAt, timeZone),
                UpdatedAt = stall.UpdatedAt == null ? null : ConvertFromUtc(stall.UpdatedAt.Value, timeZone)
            };
        }

        private static string NormalizeSlug(string input)
        {
            var slug = input.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, "\\s+", "-");
            slug = Regex.Replace(slug, "[^a-z0-9-]", string.Empty);
            slug = Regex.Replace(slug, "-+", "-");
            return slug.Trim('-');
        }

        private static DateTimeOffset ConvertFromUtc(DateTimeOffset utcDateTime, TimeZoneInfo timeZone)
        {
            var utc = utcDateTime.UtcDateTime;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var offset = timeZone.GetUtcOffset(utc);
            return new DateTimeOffset(local, offset);
        }

        private TimeZoneInfo GetTimeZone()
        {
            var timeZoneId = HttpContext.Request.Headers["X-TimeZoneId"].ToString();
            return string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        private bool TryGetUserId(out Guid userId)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(currentUserIdValue, out userId);
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("ADMIN");
        }

        private bool IsBusinessOwner()
        {
            return User.IsInRole("BusinessOwner") || User.IsInRole("BUSINESSOWNER");
        }
    }
}
