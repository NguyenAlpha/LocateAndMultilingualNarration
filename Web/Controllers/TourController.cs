using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Tours;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class TourController : Controller
    {
        private readonly TourApiClient _tourApiClient;
        private readonly GeoApiClient _geoApiClient;

        public TourController(TourApiClient tourApiClient, GeoApiClient geoApiClient)
        {
            _tourApiClient = tourApiClient;
            _geoApiClient = geoApiClient;
        }

        private IActionResult? EnsureAdmin()
        {
            var role = HttpContext.Session.GetString(ApiClient.UserRoleSessionKey);
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1, int pageSize = 20, string? search = null,
            CancellationToken ct = default)
        {
            var guard = EnsureAdmin();
            if (guard != null) return guard;

            var result = await _tourApiClient.GetToursAsync(page, pageSize, search, isActive: null, ct);
            var vm = new TourManagementViewModel
            {
                Items = result?.Data?.Items?.ToList() ?? [],
                Page = result?.Data?.Page ?? page,
                PageSize = result?.Data?.PageSize ?? pageSize,
                TotalCount = result?.Data?.TotalCount ?? 0,
                Search = search,
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View("TourManagement", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Designer(Guid? id, CancellationToken ct = default)
        {
            var guard = EnsureAdmin();
            if (guard != null) return guard;

            var geoTask = _geoApiClient.GetStallsForMapAsync(ct);
            Task<Shared.DTOs.Common.ApiResult<TourDetailDto>?>? tourTask = null;
            if (id.HasValue)
            {
                tourTask = _tourApiClient.GetTourDetailAsync(id.Value, ct);
            }

            var geoResult = await geoTask;
            var tourResult = tourTask == null ? null : await tourTask;

            var available = (geoResult?.Data ?? [])
                .Select(s => new TourDesignerStallItem
                {
                    StallId = s.StallId,
                    Name = s.StallName,
                    Slug = s.StallName,
                    Latitude = (decimal)s.Latitude,
                    Longitude = (decimal)s.Longitude,
                    ThumbnailUrl = s.MediaImages?.FirstOrDefault()?.Url
                })
                .ToList();

            var vm = new TourDesignerViewModel
            {
                IsEdit = id.HasValue,
                Tour = tourResult?.Data,
                AvailableStalls = available,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            if (id.HasValue && tourResult?.Data == null)
            {
                vm.ErrorMessage = "Không tìm thấy tour";
            }

            return View("TourDesigner", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Guid? id, [FromBody] TourSavePayload payload, CancellationToken ct = default)
        {
            var guard = EnsureAdmin();
            if (guard != null) return guard;

            if (payload == null || payload.Stops == null || payload.Stops.Count == 0)
            {
                return BadRequest(new { success = false, message = "Tour phải có ít nhất 1 stop" });
            }

            if (id.HasValue)
            {
                var update = new TourUpdateDto
                {
                    Name = payload.Name,
                    Description = payload.Description,
                    EstimatedMinutes = payload.EstimatedMinutes,
                    IsActive = payload.IsActive,
                    Stops = payload.Stops
                };
                var result = await _tourApiClient.UpdateTourAsync(id.Value, update, ct);
                if (result?.Success != true)
                    return BadRequest(new { success = false, message = result?.Error?.Message ?? "Cập nhật tour thất bại" });

                TempData["SuccessMessage"] = $"Đã cập nhật tour \"{payload.Name}\".";
                return Ok(new { success = true, id = result.Data!.Id });
            }
            else
            {
                var create = new TourCreateDto
                {
                    Name = payload.Name,
                    Description = payload.Description,
                    EstimatedMinutes = payload.EstimatedMinutes,
                    IsActive = payload.IsActive,
                    Stops = payload.Stops
                };
                var result = await _tourApiClient.CreateTourAsync(create, ct);
                if (result?.Success != true)
                    return BadRequest(new { success = false, message = result?.Error?.Message ?? "Tạo tour thất bại" });

                TempData["SuccessMessage"] = $"Đã tạo tour \"{payload.Name}\".";
                return Ok(new { success = true, id = result.Data!.Id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct = default)
        {
            var guard = EnsureAdmin();
            if (guard != null) return guard;

            var result = await _tourApiClient.ToggleActiveAsync(id, ct);
            if (result?.Success != true)
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Đổi trạng thái tour thất bại.";
            else
                TempData["SuccessMessage"] = $"Đã đổi trạng thái tour \"{result.Data?.Name}\".";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            var guard = EnsureAdmin();
            if (guard != null) return guard;

            var result = await _tourApiClient.DeleteTourAsync(id, ct);
            if (result?.Success != true)
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Xóa tour thất bại.";
            else
                TempData["SuccessMessage"] = "Đã xóa tour.";

            return RedirectToAction(nameof(Index));
        }
    }

    public class TourSavePayload
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int? EstimatedMinutes { get; set; }
        public bool IsActive { get; set; } = true;
        public List<TourStopCreateDto> Stops { get; set; } = new();
    }
}
