using Api.Authorization;
using Api.Domain.Entities;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.Tours;

namespace Api.Controllers
{
    /// <summary>
    /// Controller quản lý Tour (tuyến tham quan). Chỉ Admin được tạo/sửa/xóa.
    /// </summary>
    [ApiController]
    [Route("api/tours")]
    public class TourController : AppControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<TourController> _logger;

        public TourController(AppDbContext context, ILogger<TourController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tour active (Mobile gọi không cần token).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetTours(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("<<BEGIN>>Bắt đầu lấy danh sách tour - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Tours.AsNoTracking().AsQueryable();

            // Mobile (anonymous) chỉ thấy tour active. Admin có thể filter theo isActive param.
            if (!IsAdmin())
            {
                query = query.Where(t => t.IsActive);
            }
            else if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(t => t.Name.Contains(keyword) || (t.Description != null && t.Description.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();
            var tours = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.EstimatedMinutes,
                    t.IsActive,
                    t.CreatedAt,
                    StopCount = t.Stops.Count
                })
                .ToListAsync();

            var timeZone = GetTimeZone();
            var items = tours.Select(t => new TourListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                EstimatedMinutes = t.EstimatedMinutes,
                IsActive = t.IsActive,
                StopCount = t.StopCount,
                CreatedAt = ConvertFromUtc(t.CreatedAt, timeZone)
            }).ToList();

            var result = new PagedResult<TourListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            _logger.LogInformation("Lấy danh sách tour thành công - TotalCount: {TotalCount}<<END>>", totalCount);
            return this.OkResult(result);
        }

        /// <summary>
        /// Lấy chi tiết tour kèm stops đã order (Mobile dùng để play tour).
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTourDetail(Guid id)
        {
            _logger.LogInformation("Bắt đầu lấy chi tiết tour - Id: {TourId}", id);

            var tour = await _context.Tours
                .AsNoTracking()
                .Include(t => t.Stops.OrderBy(s => s.Order))
                    .ThenInclude(s => s.Stall)
                        .ThenInclude(st => st.StallLocations)
                .Include(t => t.Stops)
                    .ThenInclude(s => s.Stall)
                        .ThenInclude(st => st.StallMedia)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                _logger.LogWarning("Không tìm thấy tour - Id: {TourId}", id);
                return this.NotFoundResult("Không tìm thấy tour");
            }

            // Nếu không phải Admin thì tour inactive bị ẩn
            if (!tour.IsActive && !IsAdmin())
            {
                _logger.LogWarning("Tour inactive không hiển thị cho anonymous - Id: {TourId}", id);
                return this.NotFoundResult("Không tìm thấy tour");
            }

            var timeZone = GetTimeZone();
            _logger.LogInformation("Lấy chi tiết tour thành công - Id: {TourId}", id);
            return this.OkResult(MapTourDetail(tour, timeZone));
        }

        /// <summary>
        /// Admin tạo tour mới kèm danh sách stops.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> CreateTour([FromBody] TourCreateDto request)
        {
            _logger.LogInformation("Bắt đầu tạo tour - Name: {Name}", request.Name);

            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            if (request.Stops == null || request.Stops.Count == 0)
                return this.BadRequestResult("Tour phải có ít nhất 1 stop", "Stops");

            var distinctStallIds = request.Stops.Select(s => s.StallId).Distinct().ToList();
            if (distinctStallIds.Count != request.Stops.Count)
                return this.BadRequestResult("Danh sách stops chứa stall trùng lặp", "Stops");

            var existingStallCount = await _context.Stalls.CountAsync(s => distinctStallIds.Contains(s.Id));
            if (existingStallCount != distinctStallIds.Count)
                return this.BadRequestResult("Một hoặc nhiều stallId không tồn tại", "Stops");

            if (await _context.Tours.NameExistsAsync(request.Name))
                return this.ConflictResult("Tên tour đã tồn tại", "Name");

            var tour = new Tour
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                EstimatedMinutes = request.EstimatedMinutes,
                IsActive = request.IsActive,
                CreatedByUserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Re-index Order 1..N theo thứ tự client gửi (bỏ qua Order client gửi nếu lệch)
            var ordered = request.Stops.OrderBy(s => s.Order).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                tour.Stops.Add(new TourStop
                {
                    StallId = ordered[i].StallId,
                    Order = i + 1,
                    Note = ordered[i].Note,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tạo tour thành công - TourId: {TourId}, StopCount: {StopCount}", tour.Id, tour.Stops.Count);

            // Reload với stall info để map detail
            var created = await LoadTourWithStopsAsync(tour.Id);
            var timeZone = GetTimeZone();
            return this.OkResult(MapTourDetail(created!, timeZone));
        }

        /// <summary>
        /// Admin cập nhật metadata + full replace stops.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateTour(Guid id, [FromBody] TourUpdateDto request)
        {
            _logger.LogInformation("Bắt đầu cập nhật tour - Id: {TourId}", id);

            if (request.Stops == null || request.Stops.Count == 0)
                return this.BadRequestResult("Tour phải có ít nhất 1 stop", "Stops");

            var distinctStallIds = request.Stops.Select(s => s.StallId).Distinct().ToList();
            if (distinctStallIds.Count != request.Stops.Count)
                return this.BadRequestResult("Danh sách stops chứa stall trùng lặp", "Stops");

            var existingStallCount = await _context.Stalls.CountAsync(s => distinctStallIds.Contains(s.Id));
            if (existingStallCount != distinctStallIds.Count)
                return this.BadRequestResult("Một hoặc nhiều stallId không tồn tại", "Stops");

            var tour = await _context.Tours
                .Include(t => t.Stops)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return this.NotFoundResult("Không tìm thấy tour");

            if (!string.Equals(tour.Name, request.Name, StringComparison.Ordinal))
            {
                if (await _context.Tours.NameExistsAsync(request.Name, excludeId: id))
                    return this.ConflictResult("Tên tour đã tồn tại", "Name");

                tour.Name = request.Name.Trim();
            }

            tour.Description = request.Description;
            tour.EstimatedMinutes = request.EstimatedMinutes;
            tour.IsActive = request.IsActive;
            tour.UpdatedAt = DateTimeOffset.UtcNow;

            // Full replace stops
            _context.TourStops.RemoveRange(tour.Stops);
            tour.Stops.Clear();

            var ordered = request.Stops.OrderBy(s => s.Order).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                tour.Stops.Add(new TourStop
                {
                    TourId = tour.Id,
                    StallId = ordered[i].StallId,
                    Order = i + 1,
                    Note = ordered[i].Note,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cập nhật tour thành công - Id: {TourId}", id);

            var updated = await LoadTourWithStopsAsync(id);
            var timeZone = GetTimeZone();
            return this.OkResult(MapTourDetail(updated!, timeZone));
        }

        /// <summary>
        /// Admin xóa tour (cascade stops).
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteTour(Guid id)
        {
            var tour = await _context.Tours.GetByIdAsync(id);
            if (tour == null)
                return this.NotFoundResult("Không tìm thấy tour");

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Xóa tour thành công - Id: {TourId}", id);
            return this.OkResult(true);
        }

        /// <summary>
        /// Admin bật/tắt trạng thái active của tour.
        /// </summary>
        [HttpPatch("{id:guid}/toggle-active")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var tour = await _context.Tours.GetByIdAsync(id);
            if (tour == null)
                return this.NotFoundResult("Không tìm thấy tour");

            tour.IsActive = !tour.IsActive;
            tour.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var reloaded = await LoadTourWithStopsAsync(id);
            var timeZone = GetTimeZone();
            return this.OkResult(MapTourDetail(reloaded!, timeZone));
        }

        /// <summary>
        /// Admin reorder các stop trong tour.
        /// </summary>
        [HttpPost("{id:guid}/stops/reorder")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> ReorderStops(Guid id, [FromBody] List<TourStopReorderDto> request)
        {
            if (request == null || request.Count == 0)
                return this.BadRequestResult("Danh sách reorder rỗng");

            var tour = await _context.Tours
                .Include(t => t.Stops)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return this.NotFoundResult("Không tìm thấy tour");

            if (request.Count != tour.Stops.Count)
                return this.BadRequestResult("Số lượng stop không khớp với tour");

            var requestStallIds = request.Select(r => r.StallId).ToHashSet();
            var tourStallIds = tour.Stops.Select(s => s.StallId).ToHashSet();
            if (!requestStallIds.SetEquals(tourStallIds))
                return this.BadRequestResult("Danh sách stallId không khớp với tour");

            // Re-index 1..N theo Order client gửi
            var ordered = request.OrderBy(r => r.Order).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var stop = tour.Stops.First(s => s.StallId == ordered[i].StallId);
                stop.Order = i + 1;
            }

            tour.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reorder tour stops thành công - TourId: {TourId}", id);

            var reloaded = await LoadTourWithStopsAsync(id);
            var timeZone = GetTimeZone();
            return this.OkResult(MapTourDetail(reloaded!, timeZone));
        }

        private async Task<Tour?> LoadTourWithStopsAsync(Guid id)
        {
            return await _context.Tours
                .AsNoTracking()
                .Include(t => t.Stops.OrderBy(s => s.Order))
                    .ThenInclude(s => s.Stall)
                        .ThenInclude(st => st.StallLocations)
                .Include(t => t.Stops)
                    .ThenInclude(s => s.Stall)
                        .ThenInclude(st => st.StallMedia)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        private static TourDetailDto MapTourDetail(Tour tour, TimeZoneInfo timeZone)
        {
            var stops = tour.Stops
                .OrderBy(s => s.Order)
                .Select(s => MapStopDetail(s))
                .ToList();

            return new TourDetailDto
            {
                Id = tour.Id,
                Name = tour.Name,
                Description = tour.Description,
                EstimatedMinutes = tour.EstimatedMinutes,
                IsActive = tour.IsActive,
                StopCount = stops.Count,
                CreatedAt = ConvertFromUtc(tour.CreatedAt, timeZone),
                UpdatedAt = tour.UpdatedAt.HasValue ? ConvertFromUtc(tour.UpdatedAt.Value, timeZone) : null,
                Stops = stops
            };
        }

        private static TourStopDetailDto MapStopDetail(TourStop stop)
        {
            var primaryLocation = stop.Stall?.StallLocations?.FirstOrDefault(l => l.IsActive)
                                  ?? stop.Stall?.StallLocations?.FirstOrDefault();
            var thumbnail = stop.Stall?.StallMedia?
                .Where(m => m.IsActive && m.MediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.SortOrder)
                .FirstOrDefault()?.MediaUrl;

            return new TourStopDetailDto
            {
                Id = stop.Id,
                StallId = stop.StallId,
                StallName = stop.Stall?.Name ?? string.Empty,
                StallSlug = stop.Stall?.Slug ?? string.Empty,
                Order = stop.Order,
                Note = stop.Note,
                Latitude = primaryLocation?.Latitude,
                Longitude = primaryLocation?.Longitude,
                ThumbnailUrl = thumbnail
            };
        }
    }
}
