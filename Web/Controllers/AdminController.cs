using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly BusinessApiClient _businessApiClient;
        private readonly StallApiClient _stallApiClient;
        private readonly LanguageApiClient _languageApiClient;
        private readonly StallNarrationContentApiClient _narrationContentApiClient;

        public AdminController(
            BusinessApiClient businessApiClient,
            StallApiClient stallApiClient,
            LanguageApiClient languageApiClient,
            StallNarrationContentApiClient narrationContentApiClient)
        {
            _businessApiClient = businessApiClient;
            _stallApiClient = stallApiClient;
            _languageApiClient = languageApiClient;
            _narrationContentApiClient = narrationContentApiClient;
        }

        public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        {
            // Gọi song song để giảm latency
            var businessesTask = _businessApiClient.GetBusinessesAsync(1, 5, null, cancellationToken);
            var stallsTask = _stallApiClient.GetStallsAsync(1, 5, null, null, cancellationToken);
            var languagesTask = _languageApiClient.GetActiveLanguagesAsync(cancellationToken);
            var narrationTask = _narrationContentApiClient.GetContentsAsync(1, 1, null, null, null, null, cancellationToken);

            await Task.WhenAll(businessesTask, stallsTask, languagesTask, narrationTask);

            var businesses = await businessesTask;
            var stalls = await stallsTask;
            var languages = await languagesTask;
            var narrations = await narrationTask;

            var vm = new AdminDashboardViewModel
            {
                TotalBusinesses = businesses?.Data?.TotalCount ?? 0,
                TotalStalls = stalls?.Data?.TotalCount ?? 0,
                ActiveLanguages = languages?.Data?.Count ?? 0,
                TotalNarrationContents = narrations?.Data?.TotalCount ?? 0,
                RecentBusinesses = businesses?.Data?.Items?.ToList() ?? [],
                RecentStalls = stalls?.Data?.Items?.ToList() ?? [],
                Languages = languages?.Data?.ToList() ?? [],
            };

            return View(vm);
        }

        public IActionResult UserRoleManagement() => View();

        public IActionResult Statistics() => View();
    }
}
