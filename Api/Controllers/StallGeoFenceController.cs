using System.Security.Claims;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.StallGeoFences;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/stall-geo-fence")]
    [Authorize]
    public class StallGeoFenceController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<StallGeoFenceController> _logger;

        public StallGeoFenceController(AppDbContext context, ILogger<StallGeoFenceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StallGeoFenceCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo stall geofence - StallId: {StallId}", request.StallId);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

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

            if (!IsAdmin() && stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var geoFence = new Api.Domain.Entities.StallGeoFence
            {
                StallId = request.StallId,
                PolygonJson = request.PolygonJson,
                MinZoom = request.MinZoom,
                MaxZoom = request.MaxZoom
            };

            _context.StallGeoFences.Add(geoFence);
            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(geoFence));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StallGeoFenceUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật stall geofence - Id: {GeoFenceId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var geoFence = await _context.StallGeoFences
                .Include(g => g.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (geoFence == null)
            {
                return this.NotFoundResult("Không tìm thấy stall geofence");
            }

            if (!IsAdmin() && geoFence.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            geoFence.PolygonJson = request.PolygonJson;
            geoFence.MinZoom = request.MinZoom;
            geoFence.MaxZoom = request.MaxZoom;

            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(geoFence));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết stall geofence - Id: {GeoFenceId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var geoFence = await _context.StallGeoFences
                .Include(g => g.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (geoFence == null)
            {
                return this.NotFoundResult("Không tìm thấy stall geofence");
            }

            if (!IsAdmin() && geoFence.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            return this.OkResult(MapDetail(geoFence));
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? stallId = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách stall geofence - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.StallGeoFences
                .Include(g => g.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .AsQueryable();

            if (!IsAdmin())
            {
                query = query.Where(g => g.Stall.Business.OwnerUserId == userId);
            }

            if (stallId.HasValue)
            {
                query = query.Where(g => g.StallId == stallId.Value);
            }

            var totalCount = await query.CountAsync();
            var geoFences = await query
                .OrderByDescending(g => g.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = geoFences.Select(MapDetail).ToList();

            var result = new PagedResult<StallGeoFenceDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        private static StallGeoFenceDetailDto MapDetail(Api.Domain.Entities.StallGeoFence geoFence)
        {
            return new StallGeoFenceDetailDto
            {
                Id = geoFence.Id,
                StallId = geoFence.StallId,
                PolygonJson = geoFence.PolygonJson,
                MinZoom = geoFence.MinZoom,
                MaxZoom = geoFence.MaxZoom
            };
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
