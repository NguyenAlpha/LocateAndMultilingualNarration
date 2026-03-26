using System.Security.Claims;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.StallMedia;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/stall-media")]
    [Authorize]
    public class StallMediaController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<StallMediaController> _logger;

        public StallMediaController(AppDbContext context, ILogger<StallMediaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StallMediaCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo stall media - StallId: {StallId}", request.StallId);

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

            var media = new Api.Domain.Entities.StallMedia
            {
                StallId = request.StallId,
                MediaUrl = request.MediaUrl,
                MediaType = request.MediaType,
                Caption = request.Caption,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive
            };

            _context.StallMedia.Add(media);
            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(media));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StallMediaUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật stall media - Id: {MediaId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            if (!IsAdmin() && !IsBusinessOwner())
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            var media = await _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
            {
                return this.NotFoundResult("Không tìm thấy stall media");
            }

            if (!IsAdmin() && media.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            media.MediaUrl = request.MediaUrl;
            media.MediaType = request.MediaType;
            media.Caption = request.Caption;
            media.SortOrder = request.SortOrder;
            media.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return this.OkResult(MapDetail(media));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết stall media - Id: {MediaId}", id);

            if (!TryGetUserId(out var userId))
            {
                return this.UnauthorizedResult("Không xác thực");
            }

            var media = await _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
            {
                return this.NotFoundResult("Không tìm thấy stall media");
            }

            if (!IsAdmin() && media.Stall.Business.OwnerUserId != userId)
            {
                return this.ForbiddenResult("Không có quyền truy cập");
            }

            return this.OkResult(MapDetail(media));
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? stallId = null, [FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách stall media - Page: {Page}, PageSize: {PageSize}", page, pageSize);

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

            var query = _context.StallMedia
                .Include(m => m.Stall)
                .ThenInclude(s => s.Business)
                .AsNoTracking()
                .AsQueryable();

            if (!IsAdmin())
            {
                query = query.Where(m => m.Stall.Business.OwnerUserId == userId);
            }

            if (stallId.HasValue)
            {
                query = query.Where(m => m.StallId == stallId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(m => m.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var mediaList = await query
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = mediaList.Select(MapDetail).ToList();

            var result = new PagedResult<StallMediaDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return this.OkResult(result);
        }

        private static StallMediaDetailDto MapDetail(Api.Domain.Entities.StallMedia media)
        {
            return new StallMediaDetailDto
            {
                Id = media.Id,
                StallId = media.StallId,
                MediaUrl = media.MediaUrl,
                MediaType = media.MediaType,
                Caption = media.Caption,
                SortOrder = media.SortOrder,
                IsActive = media.IsActive
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
