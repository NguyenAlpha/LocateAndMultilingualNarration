using Api.Authorization;
using Api.Domain.Entities;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.DeviceLocationLogs;

namespace Api.Controllers;

[ApiController]
[Route("api/device-location-log")]
public class DeviceLocationLogController : ControllerBase
{
    private const int HeatmapMaxDays = 90;
    private const int MaxPointsPerBatch = 500;

    private readonly AppDbContext _context;
    private readonly ILogger<DeviceLocationLogController> _logger;

    public DeviceLocationLogController(AppDbContext context, ILogger<DeviceLocationLogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Nhận batch GPS từ Mobile và lưu vào bảng DeviceLocationLogs.
    /// Endpoint này AllowAnonymous — Mobile gọi không cần token.
    /// </summary>
    [HttpPost("batch")]
    [AllowAnonymous]
    public async Task<IActionResult> BatchCreate([FromBody] DeviceLocationLogBatchDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            return this.BadRequestResult("DeviceId không được để trống.", "DeviceId");

        if (dto.Points is null || dto.Points.Count == 0)
            return this.BadRequestResult("Danh sách tọa độ không được rỗng.", "Points");

        if (dto.Points.Count > MaxPointsPerBatch)
            return this.BadRequestResult($"Tối đa {MaxPointsPerBatch} điểm mỗi lần gửi.", "Points");

        var logs = dto.Points.Select(p => new DeviceLocationLog
        {
            Id = Guid.NewGuid(),
            DeviceId = dto.DeviceId,
            Latitude = (decimal)p.Latitude,
            Longitude = (decimal)p.Longitude,
            AccuracyMeters = p.AccuracyMeters.HasValue ? (decimal)p.AccuracyMeters.Value : null,
            CapturedAtUtc = p.CapturedAt
        }).ToList();

        _context.DeviceLocationLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Cập nhật LastSeenAt để tracking "thiết bị đang online" chính xác hơn —
        // endpoint này được Mobile gọi mỗi ~1 phút (flush GPS buffer), cao hơn tần suất
        // sync stall 3 phút ở GeoController. Dùng ExecuteUpdateAsync để tránh load entity.
        await _context.DevicePreferences
            .Where(d => d.DeviceId == dto.DeviceId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.LastSeenAt, DateTimeOffset.UtcNow));

        _logger.LogInformation("Lưu {Count} điểm GPS cho device {DeviceId}", logs.Count, dto.DeviceId);

        return this.OkResult(logs.Count);
    }

    /// <summary>
    /// Trả về dữ liệu heatmap gom nhóm theo tọa độ (làm tròn 5 chữ số ≈ 1.1m).
    /// Dùng cho Admin Dashboard render Leaflet.heat hoặc Google Heatmap.
    /// </summary>
    [HttpGet("heatmap")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> GetHeatmap(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? deviceId)
    {
        var toUtc = (to ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var fromUtc = (from ?? toUtc.AddDays(-7)).ToUniversalTime();

        if (fromUtc > toUtc)
            return this.BadRequestResult("Khoảng thời gian không hợp lệ: from > to.", "from");

        if ((toUtc - fromUtc).TotalDays > HeatmapMaxDays)
            return this.BadRequestResult($"Khoảng thời gian tối đa {HeatmapMaxDays} ngày.", "from");

        var query = _context.DeviceLocationLogs
            .AsNoTracking()
            .Where(x => x.CapturedAtUtc >= fromUtc && x.CapturedAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(deviceId))
            query = query.Where(x => x.DeviceId == deviceId);

        var points = await query
            .GroupBy(x => new { x.Latitude, x.Longitude })
            .Select(g => new HeatmapPointDto
            {
                Latitude = (double)g.Key.Latitude,
                Longitude = (double)g.Key.Longitude,
                Weight = g.Count()
            })
            .ToListAsync();

        return this.OkResult(points);
    }
}
