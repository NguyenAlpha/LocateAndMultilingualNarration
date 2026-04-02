using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.StallLocations;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class StallLocationController : Controller
    {
        private readonly StallLocationApiClient _stallLocationApiClient;
        private readonly StallApiClient _stallApiClient;
        private readonly IConfiguration _configuration;

        public StallLocationController(StallLocationApiClient stallLocationApiClient, StallApiClient stallApiClient, IConfiguration configuration)
        {
            _stallLocationApiClient = stallLocationApiClient;
            _stallApiClient = stallApiClient;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? stallName = null, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            var locationsResult = await _stallLocationApiClient.GetLocationsAsync(page, pageSize, null, isActive, cancellationToken, stallName);

            var viewModel = new StallLocationManagementViewModel();

            if (locationsResult?.Success == true && locationsResult.Data != null)
            {
                var data = locationsResult.Data;
                viewModel = new StallLocationManagementViewModel
                {
                    Items = data.Items,
                    Page = data.Page,
                    PageSize = data.PageSize,
                    TotalCount = data.TotalCount,
                    StallName = stallName,
                    IsActive = isActive,
                };
            }
            else
            {
                viewModel = new StallLocationManagementViewModel
                {
                    Items = Array.Empty<StallLocationDetailDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    StallName = stallName,
                    IsActive = isActive,
                    ErrorMessage = locationsResult?.Error?.Message ?? "Không lấy được danh sách vị trí."
                };
            }

            viewModel.SuccessMessage = TempData["SuccessMessage"] as string;
            viewModel.ErrorMessage ??= TempData["ErrorMessage"] as string;

            return View("StallLocationIndex", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateMap(Guid? stallId = null, CancellationToken cancellationToken = default)
        {
            var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
            var stalls = stallsResult?.Success == true && stallsResult.Data != null
                ? stallsResult.Data.Items
                : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

            ViewBag.Stalls = stalls;
            ViewBag.ApiBaseUrl = _configuration.GetValue<string>("Api:BaseUrl") ?? "https://localhost:7188/";
            ViewBag.Mode = "create";
            ViewBag.AllLocationsJson = await BuildAllLocationsJsonAsync(cancellationToken);

            if (stallId.HasValue)
            {
                ViewBag.StallId = stallId;
                var existingResult = await _stallLocationApiClient.GetLocationsAsync(1, 1, stallId, null, cancellationToken);
                var existing = existingResult?.Success == true ? existingResult.Data?.Items.FirstOrDefault() : null;
                if (existing != null)
                {
                    ViewBag.Mode = "edit";
                    ViewBag.LocationId = existing.Id;
                    ViewBag.Latitude = existing.Latitude;
                    ViewBag.Longitude = existing.Longitude;
                    ViewBag.RadiusMeters = existing.RadiusMeters;
                    ViewBag.Address = existing.Address;
                    ViewBag.IsActive = existing.IsActive;
                }
            }

            return View("StallLocationMap");
        }

        [HttpGet]
        public async Task<IActionResult> EditMap(Guid id, CancellationToken cancellationToken = default)
        {
            var locationResult = await _stallLocationApiClient.GetLocationAsync(id, cancellationToken);
            if (locationResult?.Success != true || locationResult.Data == null)
            {
                TempData["ErrorMessage"] = locationResult?.Error?.Message ?? "Không tìm thấy vị trí.";
                return RedirectToAction(nameof(Index));
            }

            var location = locationResult.Data;
            var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
            var stalls = stallsResult?.Success == true && stallsResult.Data != null
                ? stallsResult.Data.Items
                : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

            ViewBag.Mode = "edit";
            ViewBag.Stalls = stalls;
            ViewBag.StallId = location.StallId;
            ViewBag.LocationId = location.Id;

            // Set form values
            ViewBag.Latitude = location.Latitude;
            ViewBag.Longitude = location.Longitude;
            ViewBag.RadiusMeters = location.RadiusMeters;
            ViewBag.Address = location.Address;
            ViewBag.IsActive = location.IsActive;
            ViewBag.ApiBaseUrl = _configuration.GetValue<string>("Api:BaseUrl") ?? "https://localhost:7188/";
            ViewBag.AllLocationsJson = await BuildAllLocationsJsonAsync(cancellationToken);

            return View("StallLocationMap");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] StallLocationCreateDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
                var stalls = stallsResult?.Success == true && stallsResult.Data != null
                    ? stallsResult.Data.Items
                    : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

                ViewBag.Mode = "create";
                ViewBag.Stalls = stalls;
                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ.";
                return View("StallLocationMap", request);
            }

            var result = await _stallLocationApiClient.CreateLocationAsync(request, cancellationToken);
            if (result?.Success != true)
            {
                var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
                var stalls = stallsResult?.Success == true && stallsResult.Data != null
                    ? stallsResult.Data.Items
                    : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

                ViewBag.Mode = "create";
                ViewBag.Stalls = stalls;
                ViewBag.ErrorMessage = result?.Error?.Message ?? "Tạo vị trí thất bại.";
                return View("StallLocationMap", request);
            }

            TempData["SuccessMessage"] = "Tạo vị trí thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, [FromForm] StallLocationUpdateDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
                var stalls = stallsResult?.Success == true && stallsResult.Data != null
                    ? stallsResult.Data.Items
                    : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

                ViewBag.Mode = "edit";
                ViewBag.Stalls = stalls;
                ViewBag.LocationId = id;
                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ.";
                return View("StallLocationMap", request);
            }

            var result = await _stallLocationApiClient.UpdateLocationAsync(id, request, cancellationToken);
            if (result?.Success != true)
            {
                var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
                var stalls = stallsResult?.Success == true && stallsResult.Data != null
                    ? stallsResult.Data.Items
                    : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

                ViewBag.Mode = "edit";
                ViewBag.Stalls = stalls;
                ViewBag.LocationId = id;
                ViewBag.ErrorMessage = result?.Error?.Message ?? "Cập nhật vị trí thất bại.";
                return View("StallLocationMap", request);
            }

            TempData["SuccessMessage"] = "Cập nhật vị trí thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> BuildAllLocationsJsonAsync(CancellationToken cancellationToken)
        {
            var result = await _stallLocationApiClient.GetLocationsAsync(1, 500, null, null, cancellationToken);
            var items = result?.Success == true && result.Data != null
                ? result.Data.Items
                : Array.Empty<StallLocationDetailDto>();

            return JsonSerializer.Serialize(items.Select(l => new
            {
                id = l.Id.ToString(),
                stallId = l.StallId.ToString(),
                stallName = l.StallName ?? "",
                latitude = l.Latitude,
                longitude = l.Longitude,
                radiusMeters = l.RadiusMeters
            }));
        }
    }
}
