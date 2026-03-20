using System.Security.Claims;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Businesses;
using Shared.DTOs.Common;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý Business cho BusinessOwner/Admin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<BusinessController> _logger;

        /// <summary>
        /// Khởi tạo BusinessController với các dependencies cần thiết
        /// </summary>
        /// <param name="context">Database context để truy vấn dữ liệu</param>
        /// <param name="logger">Logger để ghi log</param>
        public BusinessController(AppDbContext context, ILogger<BusinessController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới business cho BusinessOwner/Admin
        /// </summary>
        /// <param name="request">Thông tin tạo business</param>
        /// <returns>Business vừa tạo</returns>
        /// <response code="200">Tạo thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        [HttpPost]
        public async Task<IActionResult> CreateBusiness([FromBody] BusinessCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo business - Name: {Name}", request.Name);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi tạo business");
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                _logger.LogWarning("Không có quyền tạo business - UserId: {UserId}", userId);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            _logger.LogDebug("Tạo entity business cho UserId: {UserId}", userId);
            var business = new Api.Domain.Entities.Business
            {
                Name = request.Name,
                TaxCode = request.TaxCode,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                OwnerUserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tạo business thành công - BusinessId: {BusinessId}", business.Id);

            var timeZone = GetTimeZone();

            return this.OkResult(MapBusinessDetail(business, timeZone));
        }

        /// <summary>
        /// Cập nhật business theo Id
        /// </summary>
        /// <param name="id">Id của business</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Business sau khi cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy business</response>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateBusiness(Guid id, [FromBody] BusinessUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật business - Id: {BusinessId}", id);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi cập nhật business - Id: {BusinessId}", id);
                return this.UnauthorizedResult("Không xác thực");
            }

            _logger.LogDebug("Truy vấn business - Id: {BusinessId}", id);
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == id);
            if (business == null)
            {
                _logger.LogWarning("Không tìm thấy business - Id: {BusinessId}", id);
                return this.NotFoundResult("Không tìm thấy business");
            }

            if (!IsAdmin() && business.OwnerUserId != userId)
            {
                _logger.LogWarning("Không có quyền cập nhật business - Id: {BusinessId}", id);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            business.Name = request.Name;
            business.TaxCode = request.TaxCode;
            business.ContactEmail = request.ContactEmail;
            business.ContactPhone = request.ContactPhone;
            business.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cập nhật business thành công - Id: {BusinessId}", id);

            var timeZone = GetTimeZone();

            return this.OkResult(MapBusinessDetail(business, timeZone));
        }

        /// <summary>
        /// Lấy chi tiết business theo Id
        /// </summary>
        /// <param name="id">Id của business</param>
        /// <returns>Thông tin business</returns>
        /// <response code="200">Trả về chi tiết business</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy business</response>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBusinessDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết business - Id: {BusinessId}", id);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi lấy chi tiết business - Id: {BusinessId}", id);
                return this.UnauthorizedResult("Không xác thực");
            }

            _logger.LogDebug("Truy vấn business - Id: {BusinessId}", id);
            var business = await _context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (business == null)
            {
                _logger.LogWarning("Không tìm thấy business - Id: {BusinessId}", id);
                return this.NotFoundResult("Không tìm thấy business");
            }

            if (!IsAdmin() && business.OwnerUserId != userId)
            {
                _logger.LogWarning("Không có quyền truy cập business - Id: {BusinessId}", id);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var timeZone = GetTimeZone();

            _logger.LogInformation("Lấy chi tiết business thành công - Id: {BusinessId}", id);
            return this.OkResult(MapBusinessDetail(business, timeZone));
        }

        /// <summary>
        /// Lấy danh sách business có phân trang
        /// </summary>
        /// <param name="page">Trang hiện tại</param>
        /// <param name="pageSize">Số phần tử mỗi trang</param>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách business phân trang</returns>
        /// <response code="200">Trả về danh sách business</response>
        /// <response code="401">Không xác thực</response>
        /// <response code="403">Không có quyền truy cập</response>
        [HttpGet]
        public async Task<IActionResult> GetBusinesses([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách business - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi lấy danh sách business");
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                _logger.LogWarning("Không có quyền truy cập danh sách business - UserId: {UserId}", userId);
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Businesses.AsNoTracking();

            if (!IsAdmin())
            {
                query = query.Where(b => b.OwnerUserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(b => b.Name.Contains(keyword) || (b.TaxCode != null && b.TaxCode.Contains(keyword)));
            }

            _logger.LogDebug("Truy vấn danh sách business - UserId: {UserId}", userId);
            var totalCount = await query.CountAsync();
            var businesses = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var timeZone = GetTimeZone();
            var items = businesses.Select(b => MapBusinessDetail(b, timeZone)).ToList();

            var result = new PagedResult<BusinessDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            _logger.LogInformation("Lấy danh sách business thành công - TotalCount: {TotalCount}", totalCount);
            return this.OkResult(result);
        }

        private static BusinessDetailDto MapBusinessDetail(Api.Domain.Entities.Business business, TimeZoneInfo timeZone)
        {
            return new BusinessDetailDto
            {
                Id = business.Id,
                Name = business.Name,
                TaxCode = business.TaxCode,
                ContactEmail = business.ContactEmail,
                ContactPhone = business.ContactPhone,
                OwnerUserId = business.OwnerUserId,
                CreatedAt = ConvertFromUtc(business.CreatedAt, timeZone),
                IsActive = business.IsActive
            };
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
