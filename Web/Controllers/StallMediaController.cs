using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Stalls;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class StallMediaController : Controller
    {
        private readonly StallMediaApiClient _stallMediaApiClient;
        private readonly StallApiClient _stallApiClient;

        public StallMediaController(StallMediaApiClient stallMediaApiClient, StallApiClient stallApiClient)
        {
            _stallMediaApiClient = stallMediaApiClient;
            _stallApiClient = stallApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12, Guid? stallId = null, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            var viewModel = await BuildViewModelAsync(page, pageSize, stallId, isActive, cancellationToken);
            viewModel.SuccessMessage = TempData["SuccessMessage"] as string;
            viewModel.ErrorMessage ??= TempData["ErrorMessage"] as string;
            return View("StallMediaManagement", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCreate([Bind(Prefix = "Create")] StallMediaFormViewModel model, int page = 1, int pageSize = 12, Guid? stallId = null, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            if (model.ImageFile == null)
                ModelState.AddModelError("Create.ImageFile", "Vui lòng chọn ảnh.");

            if (!ModelState.IsValid)
            {
                var vm = await BuildViewModelAsync(page, pageSize, stallId, isActive, cancellationToken);
                vm.Create = model;
                vm.ShowCreateModal = true;
                return View("StallMediaManagement", vm);
            }

            var result = await _stallMediaApiClient.UploadCreateAsync(
                model.StallId, model.ImageFile!, model.Caption, model.SortOrder, model.IsActive, cancellationToken);

            if (result?.Success != true)
            {
                var vm = await BuildViewModelAsync(page, pageSize, stallId, isActive, cancellationToken);
                vm.Create = model;
                vm.ShowCreateModal = true;
                vm.ErrorMessage = result?.Error?.Message ?? "Tạo media thất bại.";
                return View("StallMediaManagement", vm);
            }

            TempData["SuccessMessage"] = "Tạo media thành công.";
            return RedirectToAction(nameof(Index), new { page, pageSize, stallId = model.StallId, isActive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadUpdate(
            [Bind(Prefix = "Edit")] StallMediaFormViewModel model,
            int page = 1, int pageSize = 12, Guid? stallId = null, bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            if (model.ImageFile == null)
                ModelState.AddModelError("Edit.ImageFile", "Vui lòng chọn ảnh mới.");

            if (model.Id == null)
                ModelState.AddModelError("Edit.Id", "Thiếu ID media.");

            if (!ModelState.IsValid)
            {
                var vm = await BuildViewModelAsync(page, pageSize, stallId, isActive, cancellationToken);
                vm.Edit = model;
                vm.ShowEditModal = true;
                return View("StallMediaManagement", vm);
            }

            var result = await _stallMediaApiClient.UploadUpdateAsync(
                model.Id!.Value, model.ImageFile!, model.Caption, model.SortOrder, model.IsActive, cancellationToken);

            if (result?.Success != true)
            {
                var vm = await BuildViewModelAsync(page, pageSize, stallId, isActive, cancellationToken);
                vm.Edit = model;
                vm.ShowEditModal = true;
                vm.ErrorMessage = result?.Error?.Message ?? "Cập nhật media thất bại.";
                return View("StallMediaManagement", vm);
            }

            TempData["SuccessMessage"] = "Cập nhật media thành công.";
            return RedirectToAction(nameof(Index), new { page, pageSize, stallId, isActive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            Guid id, int page = 1, int pageSize = 12, Guid? stallId = null, bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _stallMediaApiClient.DeleteAsync(id, cancellationToken);

            if (result?.Success != true)
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Xóa media thất bại.";
            else
                TempData["SuccessMessage"] = "Xóa media thành công.";

            return RedirectToAction(nameof(Index), new { page, pageSize, stallId, isActive });
        }

        private async Task<StallMediaManagementViewModel> BuildViewModelAsync(
            int page, int pageSize, Guid? stallId, bool? isActive, CancellationToken cancellationToken)
        {
            var stallsResult = await _stallApiClient.GetStallsAsync(1, 500, null, null, cancellationToken);
            var stalls = stallsResult?.Success == true && stallsResult.Data != null
                ? stallsResult.Data.Items
                : Array.Empty<StallDetailDto>();

            var mediaResult = await _stallMediaApiClient.GetListAsync(page, pageSize, stallId, isActive, cancellationToken);

            if (mediaResult?.Success == true && mediaResult.Data != null)
            {
                var data = mediaResult.Data;
                return new StallMediaManagementViewModel
                {
                    Items = data.Items,
                    Page = data.Page,
                    PageSize = data.PageSize,
                    TotalCount = data.TotalCount,
                    FilterStallId = stallId,
                    IsActive = isActive,
                    Stalls = stalls,
                    Create = new StallMediaFormViewModel { StallId = stallId ?? Guid.Empty }
                };
            }

            return new StallMediaManagementViewModel
            {
                Page = page,
                PageSize = pageSize,
                FilterStallId = stallId,
                IsActive = isActive,
                Stalls = stalls,
                Create = new StallMediaFormViewModel { StallId = stallId ?? Guid.Empty },
                ErrorMessage = mediaResult?.Error?.Message ?? "Không lấy được danh sách media."
            };
        }
    }
}
