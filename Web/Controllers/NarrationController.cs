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
        private readonly NarrationAudioApiClient _narrationAudioApiClient;

        public NarrationController(StallNarrationContentApiClient stallNarrationContentApiClient, StallApiClient stallApiClient, LanguageApiClient languageApiClient, NarrationAudioApiClient narrationAudioApiClient)
        {
            _stallNarrationContentApiClient = stallNarrationContentApiClient;
            _stallApiClient = stallApiClient;
            _languageApiClient = languageApiClient;
            _narrationAudioApiClient = narrationAudioApiClient;
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
            var contentResult = await _stallNarrationContentApiClient.GetContentAsync(id, cancellationToken);
            if (contentResult?.Success != true || contentResult.Data == null)
            {
                return View("show", new StallNarrationContentShowViewModel
                {
                    ErrorMessage = contentResult?.Error?.Message ?? "Không lấy được nội dung narration."
                });
            }

            var content = contentResult.Data;
            var audioResult = await _narrationAudioApiClient.GetAudiosAsync(1, 200, content.Id, null, cancellationToken);
            var stallResult = await _stallApiClient.GetStallAsync(content.StallId, cancellationToken);
            var languageResult = await _languageApiClient.GetActiveLanguagesAsync(cancellationToken);

            var stallName = stallResult?.Success == true && stallResult.Data != null
                ? stallResult.Data.Name
                : content.StallId.ToString();

            var languageName = languageResult?.Success == true && languageResult.Data != null
                ? languageResult.Data.FirstOrDefault(l => l.Id == content.LanguageId)?.Name
                : null;

            var audios = audioResult?.Success == true && audioResult.Data != null
                ? audioResult.Data.Items
                : Array.Empty<NarrationAudioDetailDto>();

            return View("show", new StallNarrationContentShowViewModel
            {
                Content = content,
                Audios = audios,
                StallName = stallName,
                LanguageName = languageName ?? content.LanguageId.ToString(),
                ErrorMessage = audioResult?.Success == true ? null : audioResult?.Error?.Message
            });
        }
    }
}
