using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Businesses;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class BusinessController : Controller
    {
        private readonly BusinessApiClient _businessApiClient;

        public BusinessController(BusinessApiClient businessApiClient)
        {
            _businessApiClient = businessApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
        {
            var viewModel = await BuildViewModel(page, pageSize, search, sortBy, sortDir, cancellationToken);
            viewModel.SuccessMessage = TempData["SuccessMessage"] as string;
            viewModel.ErrorMessage ??= TempData["ErrorMessage"] as string;
            return View("BusinessManagement", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "Create")] BusinessFormViewModel model, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return await ReturnWithCreateError(model, page, pageSize, search, sortBy, sortDir, null, cancellationToken);
            }

            var result = await _businessApiClient.CreateBusinessAsync(new BusinessCreateDto
            {
                Name = model.Name,
                TaxCode = model.TaxCode,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone
            }, cancellationToken);

            if (result?.Success != true)
            {
                return await ReturnWithCreateError(model, page, pageSize, search, sortBy, sortDir, result?.Error?.Message ?? "Tạo business thất bại.", cancellationToken);
            }

            TempData["SuccessMessage"] = "Tạo business thành công.";
            return RedirectToAction(nameof(Index), new { page, pageSize, search, sortBy, sortDir });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([Bind(Prefix = "Edit")] BusinessFormViewModel model, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
        {
            if (model.Id == null)
            {
                return await ReturnWithEditError(model, page, pageSize, search, sortBy, sortDir, "Không tìm thấy business cần cập nhật.", cancellationToken);
            }

            if (!ModelState.IsValid)
            {
                return await ReturnWithEditError(model, page, pageSize, search, sortBy, sortDir, null, cancellationToken);
            }

            var result = await _businessApiClient.UpdateBusinessAsync(model.Id.Value, new BusinessUpdateDto
            {
                Name = model.Name,
                TaxCode = model.TaxCode,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                IsActive = model.IsActive
            }, cancellationToken);

            if (result?.Success != true)
            {
                return await ReturnWithEditError(model, page, pageSize, search, sortBy, sortDir, result?.Error?.Message ?? "Cập nhật business thất bại.", cancellationToken);
            }

            TempData["SuccessMessage"] = "Cập nhật business thành công.";
            return RedirectToAction(nameof(Index), new { page, pageSize, search, sortBy, sortDir });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
        {
            var detailResult = await _businessApiClient.GetBusinessAsync(id, cancellationToken);
            if (detailResult?.Success != true || detailResult.Data == null)
            {
                TempData["ErrorMessage"] = detailResult?.Error?.Message ?? "Không lấy được thông tin business.";
                return RedirectToAction(nameof(Index), new { page, pageSize, search, sortBy, sortDir });
            }

            var detail = detailResult.Data;
            var newActive = !detail.IsActive;

            var updateResult = await _businessApiClient.UpdateBusinessAsync(id, new BusinessUpdateDto
            {
                Name = detail.Name,
                TaxCode = detail.TaxCode,
                ContactEmail = detail.ContactEmail,
                ContactPhone = detail.ContactPhone,
                IsActive = newActive
            }, cancellationToken);

            if (updateResult?.Success != true)
            {
                TempData["ErrorMessage"] = updateResult?.Error?.Message ?? "Không thể cập nhật trạng thái business.";
                return RedirectToAction(nameof(Index), new { page, pageSize, search, sortBy, sortDir });
            }

            TempData["SuccessMessage"] = newActive ? "Business đã được kích hoạt." : "Business đã được vô hiệu hóa.";
            return RedirectToAction(nameof(Index), new { page, pageSize, search, sortBy, sortDir });
        }

        private async Task<BusinessManagementViewModel> BuildViewModel(int page, int pageSize, string? search, string? sortBy, string? sortDir, CancellationToken cancellationToken)
        {
            var result = await _businessApiClient.GetBusinessesAsync(page, pageSize, search, cancellationToken, sortBy, sortDir);
            if (result?.Success == true && result.Data != null)
            {
                return new BusinessManagementViewModel
                {
                    Items = result.Data.Items,
                    Page = result.Data.Page,
                    PageSize = result.Data.PageSize,
                    TotalCount = result.Data.TotalCount,
                    Search = search,
                    SortBy = sortBy,
                    SortDir = sortDir
                };
            }

            return new BusinessManagementViewModel
            {
                Items = Array.Empty<Shared.DTOs.Businesses.BusinessDetailDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                Search = search,
                SortBy = sortBy,
                SortDir = sortDir,
                ErrorMessage = result?.Error?.Message ?? "Không lấy được danh sách business."
            };
        }

        private async Task<IActionResult> ReturnWithCreateError(BusinessFormViewModel model, int page, int pageSize, string? search, string? sortBy, string? sortDir, string? errorMessage, CancellationToken cancellationToken)
        {
            var viewModel = await BuildViewModel(page, pageSize, search, sortBy, sortDir, cancellationToken);
            viewModel.Create = model;
            viewModel.ErrorMessage = errorMessage ?? viewModel.ErrorMessage;
            viewModel.ShowCreateModal = true;
            return View("BusinessManagement", viewModel);
        }

        private async Task<IActionResult> ReturnWithEditError(BusinessFormViewModel model, int page, int pageSize, string? search, string? sortBy, string? sortDir, string? errorMessage, CancellationToken cancellationToken)
        {
            var viewModel = await BuildViewModel(page, pageSize, search, sortBy, sortDir, cancellationToken);
            viewModel.Edit = model;
            viewModel.ErrorMessage = errorMessage ?? viewModel.ErrorMessage;
            viewModel.ShowEditModal = true;
            return View("BusinessManagement", viewModel);
        }
    }
}
