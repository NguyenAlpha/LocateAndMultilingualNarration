using System.Security.Claims;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.StallLocations;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/stall-location")]
    [Authorize]
    /// <summary>
    /// API quản lý vị trí gian hàng với phân quyền theo vai trò.
    /// </summary>
    public class StallLocationController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<StallLocationController> _logger;

        public StallLocationController(AppDbContext context, ILogger<StallLocationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới vị trí gian hàng.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StallLocationCreateDto request)
        {
            // Tạo mới vị trí gian hàng, chỉ cho phép Admin hoặc chủ doanh nghiệp.
            _logger.LogInformation("Bắt đầu tạo stall location - StallId: {StallId}", request.StallId);

            // Xác thực người dùng đăng nhập.
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Kiểm tra quyền truy cập theo vai trò.
            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var stall = await _context.Stalls
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Id == request.StallId);

            if (stall == null)
            {
                return this.NotFoundResult("Không tìm thấy stall");
            }

            // Chỉ chủ doanh nghiệp của stall hoặc Admin mới được tạo vị trí.
            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var location = new Api.Domain.Entities.StallLocation
            {
                StallId = request.StallId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                RadiusMeters = request.RadiusMeters,
                Address = request.Address,
                MapProviderPlaceId = request.MapProviderPlaceId,
                IsActive = request.IsActive,
                UpdatedAt = DateTimeOffset.UtcNow,
                Stall = stall
            };

            _context.StallLocations.Add(location);
            await _context.SaveChangesAsync();

            // Trả về DTO sau khi chuyển đổi thời gian UTC theo múi giờ người dùng.
            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(location, timeZone));
        }


        /// <summary>
        /// Cập nhật vị trí gian hàng theo id.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StallLocationUpdateDto request)
        {
            // Cập nhật vị trí gian hàng theo Id, chỉ cho phép Admin hoặc chủ doanh nghiệp.
            _logger.LogInformation("Bắt đầu cập nhật stall location - Id: {LocationId}", id);

            // Xác thực người dùng đăng nhập.
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Kiểm tra quyền truy cập theo vai trò.
            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var location = await _context.StallLocations
                .Include(l => l.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return this.NotFoundResult("Không tìm thấy stall location");
            }

            // Chỉ chủ doanh nghiệp của stall hoặc Admin mới được cập nhật.
            if (!IsAdmin() && location.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            location.Latitude = request.Latitude;
            location.Longitude = request.Longitude;
            location.RadiusMeters = request.RadiusMeters;
            location.Address = request.Address;
            location.MapProviderPlaceId = request.MapProviderPlaceId;
            location.IsActive = request.IsActive;
            location.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            // Trả về DTO sau khi chuyển đổi thời gian UTC theo múi giờ người dùng.
            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(location, timeZone));
        }


        /// <summary>
        /// Lấy chi tiết vị trí gian hàng theo id.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            // Lấy chi tiết vị trí gian hàng theo Id.
            _logger.LogInformation("Bắt đầu lấy chi tiết stall location - Id: {LocationId}", id);

            // Xác thực người dùng đăng nhập.
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var location = await _context.StallLocations
                .Include(l => l.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return this.NotFoundResult("Không tìm thấy stall location");
            }

            // Chỉ chủ doanh nghiệp của stall hoặc Admin mới được xem chi tiết.
            if (!IsAdmin() && location.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            // Trả về DTO sau khi chuyển đổi thời gian UTC theo múi giờ người dùng.
            var timeZone = GetTimeZone();
            return this.OkResult(MapDetail(location, timeZone));
        }


        /// <summary>
        /// Lấy danh sách vị trí gian hàng theo phân trang và bộ lọc.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? stallId = null, [FromQuery] bool? isActive = null, [FromQuery] string? stallName = null)
        {
            // Lấy danh sách vị trí gian hàng theo phân trang và bộ lọc.
            _logger.LogInformation("<<BEGIN>> Bắt đầu lấy danh sách stall location - Page: {Page}, PageSize: {PageSize}<<END>>", page, pageSize);

            // Xác thực người dùng đăng nhập.
            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            // Kiểm tra quyền truy cập theo vai trò.
            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.StallLocations
                .Include(l => l.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .AsQueryable();

            // BusinessOwner chỉ xem được các stall thuộc doanh nghiệp của mình.
            if (!IsAdmin())
            {
                query = query.Where(l => l.Stall.Business.OwnerUserId == userId);
            }

            // Lọc theo stallId nếu có.
            if (stallId.HasValue)
            {
                query = query.Where(l => l.StallId == stallId.Value);
            }

            // Lọc theo tên stall nếu có.
            if (!string.IsNullOrWhiteSpace(stallName))
            {
                query = query.Where(l => l.Stall.Name.Contains(stallName));
            }

            // Lọc theo trạng thái kích hoạt nếu có.
            if (isActive.HasValue)
            {
                query = query.Where(l => l.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var locations = await query
                .OrderByDescending(l => l.UpdatedAt)
                .ThenByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Trả về kết quả phân trang với thời gian đã đổi theo múi giờ người dùng.
            var timeZone = GetTimeZone();
            var items = locations.Select(l => MapDetail(l, timeZone)).ToList();

            var result = new PagedResult<StallLocationDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        private static StallLocationDetailDto MapDetail(Api.Domain.Entities.StallLocation location, TimeZoneInfo timeZone)
        {
            // Ánh xạ entity sang DTO và chuyển đổi thời gian theo múi giờ người dùng.
            return new StallLocationDetailDto
            {
                Id = location.Id,
                StallId = location.StallId,
                StallName = location.Stall?.Name,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                RadiusMeters = location.RadiusMeters,
                Address = location.Address,
                MapProviderPlaceId = location.MapProviderPlaceId,
                UpdatedAt = ConvertFromUtc(location.UpdatedAt, timeZone),
                IsActive = location.IsActive
            };
        }

        private static DateTimeOffset? ConvertFromUtc(DateTimeOffset? utcDateTime, TimeZoneInfo timeZone)
        {
            // Chuyển đổi thời gian UTC sang múi giờ người dùng để hiển thị.
            if (utcDateTime == null)
            {
                return null;
            }

            var utc = utcDateTime.Value.UtcDateTime;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var offset = timeZone.GetUtcOffset(utc);
            return new DateTimeOffset(local, offset);
        }

        private TimeZoneInfo GetTimeZone()
        {
            // Lấy múi giờ từ header, mặc định theo khu vực SE Asia.
            var timeZoneId = HttpContext.Request.Headers["X-TimeZoneId"].ToString();
            return string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        private bool TryGetUserId(out Guid userId)
        {
            // Lấy UserId từ claim để phục vụ kiểm tra quyền.
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(currentUserIdValue, out userId);
        }

        private bool IsAdmin()
        {
            // Kiểm tra vai trò Admin (không phân biệt hoa thường).
            return User.IsInRole("Admin") || User.IsInRole("ADMIN");
        }

        private bool IsBusinessOwner()
        {
            // Kiểm tra vai trò BusinessOwner (không phân biệt hoa thường).
            return User.IsInRole("BusinessOwner") || User.IsInRole("BUSINESSOWNER");
        }
    }
}
