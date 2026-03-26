using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.StallGeoFences;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class StallGeoFenceController : Controller
    {
        private readonly StallGeoFenceApiClient _stallGeoFenceApiClient;
        private readonly StallApiClient _stallApiClient;

        public StallGeoFenceController(StallGeoFenceApiClient stallGeoFenceApiClient, StallApiClient stallApiClient)
        {
            _stallGeoFenceApiClient = stallGeoFenceApiClient;
            _stallApiClient = stallApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, Guid? stallId = null, CancellationToken cancellationToken = default)
        {
            var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
            var stalls = stallsResult?.Success == true && stallsResult.Data != null
                ? stallsResult.Data.Items
                : Array.Empty<Shared.DTOs.Stalls.StallDetailDto>();

            var geoResult = await _stallGeoFenceApiClient.GetGeoFencesAsync(page, pageSize, stallId, cancellationToken);
            if (geoResult?.Success == true && geoResult.Data != null)
            {
                var data = geoResult.Data;
                return View("StallGeoFenceIndex", new StallGeoFenceManagementViewModel
                {
                    Items = data.Items,
                    Page = data.Page,
                    PageSize = data.PageSize,
                    TotalCount = data.TotalCount,
                    StallId = stallId,
                    Stalls = stalls
                });
            }

            return View("StallGeoFenceIndex", new StallGeoFenceManagementViewModel
            {
                Items = Array.Empty<StallGeoFenceDetailDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                StallId = stallId,
                Stalls = stalls,
                ErrorMessage = geoResult?.Error?.Message ?? "Không lấy được danh sách geofence."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] StallGeoFenceCreateDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _stallGeoFenceApiClient.CreateGeoFenceAsync(request, cancellationToken);
            if (result?.Success != true)
            {
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Tạo geofence thất bại.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Tạo geofence thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, [FromForm] StallGeoFenceUpdateDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _stallGeoFenceApiClient.UpdateGeoFenceAsync(id, request, cancellationToken);
            if (result?.Success != true)
            {
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Cập nhật geofence thất bại.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Cập nhật geofence thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
