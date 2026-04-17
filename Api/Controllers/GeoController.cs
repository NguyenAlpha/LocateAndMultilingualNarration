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

        [HttpGet("stalls")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllStalls([FromQuery] string? deviceId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách stall cho bản đồ - DeviceId: {DeviceId}", deviceId);
            var result = await _geoService.GetAllStallsAsync(deviceId, cancellationToken);

            // Heartbeat: cập nhật LastSeenAt để tracking thiết bị đang hoạt động
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

        [HttpGet("active-devices")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetActiveDevices(
            [FromQuery] int withinMinutes = 5,
            CancellationToken cancellationToken = default)
        {
            if (withinMinutes < 1) withinMinutes = 1;
            if (withinMinutes > 60) withinMinutes = 60;

            var threshold = DateTimeOffset.UtcNow.AddMinutes(-withinMinutes);

            var devices = await _context.DevicePreferences
                .AsNoTracking()
                .Where(d => d.LastSeenAt >= threshold)
                .OrderByDescending(d => d.LastSeenAt)
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
                WithinMinutes = withinMinutes,
                AsOf          = DateTimeOffset.UtcNow,
                Devices       = devices
            };

            return this.OkResult(summary);
        }
    }
}
