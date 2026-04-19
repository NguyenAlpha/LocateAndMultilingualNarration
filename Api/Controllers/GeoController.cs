using Api.Application.Services;
using Api.Authorization;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Geo;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/geo")]
    public class GeoController : ControllerBase
    {
        private readonly IGeoService _geoService;
        private readonly AppDbContext _context;
        private readonly ILogger<GeoController> _logger;

        public GeoController(IGeoService geoService, AppDbContext context, ILogger<GeoController> logger)
        {
            _geoService = geoService;
            _context = context;
            _logger = logger;
        }

        // Mobile gọi endpoint này mỗi 3 phút (SyncBackgroundService) để lấy danh sách stall cho bản đồ.
        // AllowAnonymous vì Mobile không có user account — chỉ có DeviceId.
        // deviceId là optional: khách chưa setup preference vẫn có thể xem stall.
        [HttpGet("stalls")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllStalls([FromQuery] string? deviceId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách stall cho bản đồ - DeviceId: {DeviceId}", deviceId);
            var result = await _geoService.GetAllStallsAsync(deviceId, cancellationToken);

            // Tận dụng request này làm heartbeat — mỗi lần Mobile sync stall là cập nhật LastSeenAt.
            // Dùng ExecuteUpdateAsync thay vì Load + SaveChanges để tránh load toàn bộ entity vào memory
            // chỉ để update 1 field.
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                await _context.DevicePreferences
                    .Where(d => d.DeviceId == deviceId)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(d => d.LastSeenAt, DateTimeOffset.UtcNow),
                        cancellationToken);
            }

            _logger.LogInformation("Lấy danh sách stall cho bản đồ thành công - Tổng: {Total}", result.Count);
            return this.OkResult(result);
        }

        // Chỉ Admin mới được xem — thiết bị nào đang online được xác định dựa vào LastSeenAt,
        // tức là thiết bị nào ping server trong withinSeconds giây qua thì coi là đang hoạt động.
        [HttpGet("active-devices")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetActiveDevices(
            [FromQuery] int withinSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            // Clamp: tối thiểu 10 giây, tối đa 300 giây (5 phút)
            if (withinSeconds < 10)  withinSeconds = 10;
            if (withinSeconds > 300) withinSeconds = 300;

            // threshold là mốc thời gian: chỉ lấy device có LastSeenAt >= mốc này
            var threshold = DateTimeOffset.UtcNow.AddSeconds(-withinSeconds);

            var devices = await _context.DevicePreferences
                .AsNoTracking()                              // chỉ đọc, không cần tracking
                .Where(d => d.LastSeenAt >= threshold)
                .OrderByDescending(d => d.LastSeenAt)        // device hoạt động gần nhất lên đầu
                .Select(d => new ActiveDeviceItemDto
                {
                    DeviceId     = d.DeviceId,
                    Platform     = d.Platform,
                    DeviceModel  = d.DeviceModel,
                    Manufacturer = d.Manufacturer,
                    LastSeenAt   = d.LastSeenAt
                })
                .ToListAsync(cancellationToken);

            var summary = new ActiveDevicesSummaryDto
            {
                ActiveCount   = devices.Count,
                WithinSeconds = withinSeconds,
                AsOf          = DateTimeOffset.UtcNow,       // timestamp server tạo response, dùng để hiển thị "cập nhật lúc"
                Devices       = devices
            };

            return this.OkResult(summary);
        }
    }
}
