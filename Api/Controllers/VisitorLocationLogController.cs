using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.VisitorLocationLogs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/visitor-location-log")]
    [Authorize]
    public class VisitorLocationLogController : AppControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<VisitorLocationLogController> _logger;

        public VisitorLocationLogController(AppDbContext context, ILogger<VisitorLocationLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VisitorLocationLogCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo visitor location log");

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var log = new Api.Domain.Entities.VisitorLocationLog
            {
                UserId = userId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AccuracyMeters = request.AccuracyMeters,
                CapturedAtUtc = DateTime.UtcNow
            };

            _context.VisitorLocationLogs.Add(log);
            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(log));
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? userId = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách visitor location log - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (!TryGetUserId(out var currentUserId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (userId.HasValue && !IsAdmin())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var targetUserId = userId ?? currentUserId;

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.VisitorLocationLogs
                .AsNoTracking()
                .Where(l => l.UserId == targetUserId)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CapturedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = logs.Select(MapDetail).ToList();

            var result = new PagedResult<VisitorLocationLogDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        private static VisitorLocationLogDetailDto MapDetail(Api.Domain.Entities.VisitorLocationLog log)
        {
            return new VisitorLocationLogDetailDto
            {
                Id = log.Id,
                UserId = log.UserId,
                Latitude = log.Latitude,
                Longitude = log.Longitude,
                AccuracyMeters = log.AccuracyMeters,
                CapturedAtUtc = log.CapturedAtUtc
            };
        }

    }
}
