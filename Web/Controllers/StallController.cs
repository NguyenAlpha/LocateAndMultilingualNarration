using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Businesses;
using Shared.DTOs.Common;
using Shared.DTOs.Stalls;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class StallController : Controller
    {
        private readonly StallApiClient _stallApiClient;
        private readonly BusinessApiClient _businessApiClient;

        public StallController(StallApiClient stallApiClient, BusinessApiClient businessApiClient)
        {
            _stallApiClient = stallApiClient;
            _businessApiClient = businessApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, Guid? businessId = null, CancellationToken cancellationToken = default)
        {
            var viewModel = await BuildViewModel(page, pageSize, search, businessId, cancellationToken);
            viewModel.SuccessMessage = TempData["SuccessMessage"] as string;
            viewModel.ErrorMessage ??= TempData["ErrorMessage"] as string;
            return View("StallManagement", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "Create")] StallFormViewModel model, int page = 1, int pageSize = 10, string? search = null, Guid? businessId = null, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return await ReturnWithCreateError(model, page, pageSize, search, businessId, null, cancellationToken);
            }

            var result = await _stallApiClient.CreateStallAsync(new StallCreateDto
            {
                BusinessId = model.BusinessId,
                Name = model.Name,
                Description = model.Description,
                Slug = model.Slug,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone
            }, cancellationToken);

            if (result?.Success != true)
            {
                return await ReturnWithCreateError(model, page, pageSize, search, businessId, result?.Error?.Message ?? "Tạo stall thất bại.", cancellationToken);
            }

            TempData["SuccessMessage"] = "Tạo stall thành công.";
            return RedirectToAction(nameof(Index), new { page, pageSize, search, businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([Bind(Prefix = "Edit")] StallFormViewModel model, int page = 1, int pageSize = 10, string? search = null, Guid? businessId = null, CancellationToken cancellationToken = default)
        {
            if (model.Id == null)
            {
                return await ReturnWithEditError(model, page, pageSize, search, businessId, "Không tìm thấy stall cần cập nhật.", cancellationToken);
            }

            if (!ModelState.IsValid)
            {
                return await ReturnWithEditError(model, page, pageSize, search, businessId, null, cancellationToken);
            }

            var result = await _stallApiClient.UpdateStallAsync(model.Id.Value, new StallUpdateDto
            {
                Name = model.Name,
                Description = model.Description,
                Slug = model.Slug,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                IsActive = model.IsActive
            }, cancellationToken);

            if (result?.Success != true)
            {
                return await ReturnWithEditError(model, page, pageSize, search, businessId, result?.Error?.Message ?? "Cập nhật stall thất bại.", cancellationToken);
            }

            TempData["SuccessMessage"] = "Cập nhật stall thành công.";
            return RedirectToAction(nameof(Index), new { page, pageSize, search, businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id, int page = 1, int pageSize = 10, string? search = null, Guid? businessId = null, CancellationToken cancellationToken = default)
        {
            var detailResult = await _stallApiClient.GetStallAsync(id, cancellationToken);
            if (detailResult?.Success != true || detailResult.Data == null)
            {
                TempData["ErrorMessage"] = detailResult?.Error?.Message ?? "Không lấy được thông tin stall.";
                return RedirectToAction(nameof(Index), new { page, pageSize, search, businessId });
            }

            var detail = detailResult.Data;
            var updateResult = await _stallApiClient.UpdateStallAsync(id, new StallUpdateDto
            {
                Name = detail.Name,
                Description = detail.Description,
                Slug = detail.Slug,
                ContactEmail = detail.ContactEmail,
                ContactPhone = detail.ContactPhone,
                IsActive = false
            }, cancellationToken);

            if (updateResult?.Success != true)
            {
                TempData["ErrorMessage"] = updateResult?.Error?.Message ?? "Không thể vô hiệu hóa stall.";
                return RedirectToAction(nameof(Index), new { page, pageSize, search, businessId });
            }

            TempData["SuccessMessage"] = "Stall đã được vô hiệu hóa.";
            return RedirectToAction(nameof(Index), new { page, pageSize, search, businessId });
        }

        private async Task<StallManagementViewModel> BuildViewModel(int page, int pageSize, string? search, Guid? businessId, CancellationToken cancellationToken)
        {
            var businessResult = await _businessApiClient.GetBusinessesAsync(1, 100, null, cancellationToken);
            var businesses = businessResult?.Success == true && businessResult.Data != null
                ? businessResult.Data.Items
                : Array.Empty<BusinessDetailDto>();

            var stallsResult = await _stallApiClient.GetStallsAsync(page, pageSize, search, businessId, cancellationToken);
            if (stallsResult?.Success == true && stallsResult.Data != null)
            {
                return new StallManagementViewModel
                {
                    Items = stallsResult.Data.Items,
                    Businesses = businesses,
                    Page = stallsResult.Data.Page,
                    PageSize = stallsResult.Data.PageSize,
                    TotalCount = stallsResult.Data.TotalCount,
                    Search = search,
                    BusinessId = businessId
                };
            }

            return new StallManagementViewModel
            {
                Items = Array.Empty<StallDetailDto>(),
                Businesses = businesses,
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                Search = search,
                BusinessId = businessId,
                ErrorMessage = stallsResult?.Error?.Message ?? "Không lấy được danh sách stall."
            };
        }

        private async Task<IActionResult> ReturnWithCreateError(StallFormViewModel model, int page, int pageSize, string? search, Guid? businessId, string? errorMessage, CancellationToken cancellationToken)
        {
            var viewModel = await BuildViewModel(page, pageSize, search, businessId, cancellationToken);
            viewModel.Create = model;
            viewModel.ErrorMessage = errorMessage ?? viewModel.ErrorMessage;
            viewModel.ShowCreateModal = true;
            return View("StallManagement", viewModel);
        }

        private async Task<IActionResult> ReturnWithEditError(StallFormViewModel model, int page, int pageSize, string? search, Guid? businessId, string? errorMessage, CancellationToken cancellationToken)
        {
            var viewModel = await BuildViewModel(page, pageSize, search, businessId, cancellationToken);
            viewModel.Edit = model;
            viewModel.ErrorMessage = errorMessage ?? viewModel.ErrorMessage;
            viewModel.ShowEditModal = true;
            return View("StallManagement", viewModel);
        }
    }
}
