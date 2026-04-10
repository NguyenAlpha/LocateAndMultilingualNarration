using Api.Application.Services;
using Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/geo")]
    public class GeoController : ControllerBase
    {
        private readonly IGeoService _geoService;
        private readonly ILogger<GeoController> _logger;

        public GeoController(IGeoService geoService, ILogger<GeoController> logger)
        {
            _geoService = geoService;
            _logger = logger;
        }

        [HttpGet("stalls")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllStalls([FromQuery] string? deviceId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu lấy danh sách stall cho bản đồ - DeviceId: {DeviceId}", deviceId);
            var result = await _geoService.GetAllStallsAsync(deviceId, cancellationToken);
            _logger.LogInformation("Lấy danh sách stall cho bản đồ thành công - Tổng: {Total}", result.Count);
            return this.OkResult(result);
        }
    }
}
