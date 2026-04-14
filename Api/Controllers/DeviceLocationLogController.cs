using Api.Domain.Entities;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.DeviceLocationLogs;

namespace Api.Controllers;

[ApiController]
[Route("api/device-location-log")]
[AllowAnonymous]
public class DeviceLocationLogController : ControllerBase
{
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

        _logger.LogInformation("Lưu {Count} điểm GPS cho device {DeviceId}", logs.Count, dto.DeviceId);

        return this.OkResult(logs.Count);
    }
}
