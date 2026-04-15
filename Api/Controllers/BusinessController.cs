using Api.Authorization;
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
    public class BusinessController : AppControllerBase
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
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> CreateBusiness([FromBody] BusinessCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo business - Name: {Name}", request.Name);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi tạo business");
                return this.UnauthorizedResult("Không xác thực");
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
                IsActive = false
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
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
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
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
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
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> GetBusinesses([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách business - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Không xác thực khi lấy danh sách business");
                return this.UnauthorizedResult("Không xác thực");
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

            var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            IOrderedQueryable<Api.Domain.Entities.Business> orderedQuery;
            if (string.Equals(sortBy, "plan", StringComparison.OrdinalIgnoreCase))
            {
                // Sort by plan rank: Free=1, Basic=2, Pro=3
                orderedQuery = descending
                    ? query.OrderByDescending(b => b.Plan == "Pro" ? 3 : b.Plan == "Basic" ? 2 : 1)
                           .ThenByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.Plan == "Pro" ? 3 : b.Plan == "Basic" ? 2 : 1)
                           .ThenByDescending(b => b.CreatedAt);
            }
            else
            {
                orderedQuery = descending
                    ? query.OrderByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.CreatedAt);
            }

            var businesses = await orderedQuery
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
                IsActive = business.IsActive,
                Plan = business.Plan,
                PlanExpiresAt = business.PlanExpiresAt
            };
        }

        /// <summary>
        /// Cập nhật gói subscription của business (chỉ Admin)
        /// </summary>
        /// <param name="id">Id của business</param>
        /// <param name="request">Gói mới và ngày hết hạn (tuỳ chọn)</param>
        /// <returns>Business sau khi cập nhật plan</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Gói không hợp lệ</response>
        /// <response code="404">Không tìm thấy business</response>
        [HttpPut("{id:guid}/subscription")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] SubscriptionUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật subscription - BusinessId: {BusinessId}, Plan: {Plan}", id, request.Plan);

            var validPlans = new[] { Api.Domain.SubscriptionPlan.Free, Api.Domain.SubscriptionPlan.Basic, Api.Domain.SubscriptionPlan.Pro };
            if (!validPlans.Contains(request.Plan))
            {
                _logger.LogWarning("Gói subscription không hợp lệ - Plan: {Plan}", request.Plan);
                return this.BadRequestResult("Gói không hợp lệ. Chỉ chấp nhận: Free, Basic, Pro", "Plan");
            }

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == id);
            if (business == null)
            {
                _logger.LogWarning("Không tìm thấy business - Id: {BusinessId}", id);
                return this.NotFoundResult("Không tìm thấy business");
            }

            business.Plan = request.Plan;
            business.PlanExpiresAt = request.PlanExpiresAt;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cập nhật subscription thành công - BusinessId: {BusinessId}, Plan: {Plan}", id, request.Plan);

            var timeZone = GetTimeZone();
            return this.OkResult(MapBusinessDetail(business, timeZone));
        }

    }
}
