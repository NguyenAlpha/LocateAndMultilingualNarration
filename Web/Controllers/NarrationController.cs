using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Languages;
using Shared.DTOs.Narrations;
using Shared.DTOs.Stalls;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class NarrationController : Controller
    {
        private readonly StallNarrationContentApiClient _stallNarrationContentApiClient;
        private readonly StallApiClient _stallApiClient;
        private readonly LanguageApiClient _languageApiClient;

        public NarrationController(StallNarrationContentApiClient stallNarrationContentApiClient, StallApiClient stallApiClient, LanguageApiClient languageApiClient)
        {
            _stallNarrationContentApiClient = stallNarrationContentApiClient;
            _stallApiClient = stallApiClient;
            _languageApiClient = languageApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> StallNarrationContents(int page = 1, int pageSize = 10, string? search = null, Guid? stallId = null, Guid? languageId = null, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            var stallsResult = await _stallApiClient.GetStallsAsync(1, 200, null, null, cancellationToken);
            var languagesResult = await _languageApiClient.GetActiveLanguagesAsync(cancellationToken);
            var contentsResult = await _stallNarrationContentApiClient.GetContentsAsync(page, pageSize, search, stallId, languageId, isActive, cancellationToken);

            var stalls = stallsResult?.Success == true && stallsResult.Data != null
                ? stallsResult.Data.Items
                : Array.Empty<StallDetailDto>();

            var languages = languagesResult?.Success == true && languagesResult.Data != null
                ? languagesResult.Data
                : Array.Empty<LanguageDetailDto>();

            if (contentsResult?.Success == true && contentsResult.Data != null)
            {
                var data = contentsResult.Data;
                return View("StallNarrationContentManagement", new StallNarrationContentManagementViewModel
                {
                    Items = data.Items,
                    Page = data.Page,
                    PageSize = data.PageSize,
                    TotalCount = data.TotalCount,
                    Search = search,
                    StallId = stallId,
                    LanguageId = languageId,
                    IsActive = isActive,
                    Stalls = stalls,
                    Languages = languages
                });
            }

            return View("StallNarrationContentManagement", new StallNarrationContentManagementViewModel
            {
                Items = Array.Empty<StallNarrationContentDetailDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                Search = search,
                StallId = stallId,
                LanguageId = languageId,
                IsActive = isActive,
                Stalls = stalls,
                Languages = languages,
                ErrorMessage = contentsResult?.Error?.Message ?? "Không lấy được danh sách narration content."
            });
        }

        [HttpGet]
        public async Task<IActionResult> Show(Guid id, CancellationToken cancellationToken = default)
        {
            var viewModel = await BuildShowViewModel(id, null, cancellationToken);
            return View("show", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id, bool isActive, CancellationToken cancellationToken = default)
        {
            var result = await _stallNarrationContentApiClient.ToggleStatusAsync(id, isActive, cancellationToken);
            if (result?.Success != true)
            {
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Không đổi được trạng thái narration content.";
                return RedirectToAction(nameof(StallNarrationContents));
            }

            TempData["SuccessMessage"] = $"Đã đổi trạng thái thành {(isActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(StallNarrationContents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StallNarrationContentCreateDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(StallNarrationContents));
            }

            var result = await _stallNarrationContentApiClient.CreateContentAsync(request, cancellationToken);
            if (result?.Success != true)
            {
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Không tạo được narration content.";
                return RedirectToAction(nameof(StallNarrationContents));
            }

            TempData["SuccessMessage"] = "Tạo narration content thành công.";
            return RedirectToAction(nameof(StallNarrationContents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, StallNarrationContentUpdateDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildShowViewModel(id, "Dữ liệu cập nhật không hợp lệ.", cancellationToken);
                invalidViewModel.Content = invalidViewModel.Content ?? new StallNarrationContentDetailDto();
                invalidViewModel.Content.Title = request.Title;
                invalidViewModel.Content.Description = request.Description;
                invalidViewModel.Content.ScriptText = request.ScriptText;
                invalidViewModel.Content.IsActive = request.IsActive;
                return View("show", invalidViewModel);
            }

            var updateResult = await _stallNarrationContentApiClient.UpdateContentAsync(id, request, cancellationToken);
            if (updateResult?.Success != true)
            {
                var errorViewModel = await BuildShowViewModel(id, updateResult?.Error?.Message ?? "Không cập nhật được narration content.", cancellationToken);
                errorViewModel.Content = errorViewModel.Content ?? new StallNarrationContentDetailDto();
                errorViewModel.Content.Title = request.Title;
                errorViewModel.Content.Description = request.Description;
                errorViewModel.Content.ScriptText = request.ScriptText;
                errorViewModel.Content.IsActive = request.IsActive;
                return View("show", errorViewModel);
            }

            TempData["SuccessMessage"] = "Cập nhật narration content thành công.";
            return RedirectToAction(nameof(Show), new { id });
        }

        private async Task<StallNarrationContentShowViewModel> BuildShowViewModel(Guid id, string? errorMessage, CancellationToken cancellationToken)
        {
            var detailResult = await _stallNarrationContentApiClient.GetContentAsync(id, cancellationToken);
            if (detailResult?.Success != true || detailResult.Data == null)
            {
                return new StallNarrationContentShowViewModel
                {
                    ErrorMessage = errorMessage ?? detailResult?.Error?.Message ?? "Không lấy được nội dung narration."
                };
            }

            var detail = detailResult.Data;
            var content = detail.Content;
            var stallResult = await _stallApiClient.GetStallAsync(content.StallId, cancellationToken);
            var languageResult = await _languageApiClient.GetActiveLanguagesAsync(cancellationToken);

            var stallName = stallResult?.Success == true && stallResult.Data != null
                ? stallResult.Data.Name
                : content.StallId.ToString();

            var languageName = languageResult?.Success == true && languageResult.Data != null
                ? languageResult.Data.FirstOrDefault(l => l.Id == content.LanguageId)?.Name
                : null;

            return new StallNarrationContentShowViewModel
            {
                Content = content,
                Audios = detail.Audios,
                StallName = stallName,
                LanguageName = languageName ?? content.LanguageId.ToString(),
                ErrorMessage = errorMessage
            };
        }
    }
}
