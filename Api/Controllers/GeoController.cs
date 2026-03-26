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

        [HttpGet("nearest-stall")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearestStall([FromQuery] decimal lat, [FromQuery] decimal lng, [FromQuery] string? langCode, [FromQuery] decimal? radius, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu tìm stall gần nhất - Lat: {Lat}, Lng: {Lng}", lat, lng);

            if (lat < -90 || lat > 90)
            {
                return this.BadRequestResult("Latitude không hợp lệ", "lat");
            }

            if (lng < -180 || lng > 180)
            {
                return this.BadRequestResult("Longitude không hợp lệ", "lng");
            }

            if (radius.HasValue && radius.Value <= 0)
            {
                return this.BadRequestResult("Radius phải lớn hơn 0", "radius");
            }

            var result = await _geoService.FindNearestStallAsync(lat, lng, langCode, radius, cancellationToken);
            if (result == null)
            {
                return NoContent();
            }

            _logger.LogInformation("Tìm stall gần nhất thành công - StallId: {StallId}", result.StallId);
            return this.OkResult(result);
        }
    }
}
