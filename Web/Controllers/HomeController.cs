using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly BusinessApiClient _businessApiClient;
        private readonly StallApiClient _stallApiClient;
        private readonly LanguageApiClient _languageApiClient;
        private readonly UserApiClient _userApiClient;

        public HomeController(
            BusinessApiClient businessApiClient,
            StallApiClient stallApiClient,
            LanguageApiClient languageApiClient,
            UserApiClient userApiClient)
        {
            _businessApiClient = businessApiClient;
            _stallApiClient = stallApiClient;
            _languageApiClient = languageApiClient;
            _userApiClient = userApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("AuthToken");
            var isLoggedIn = !string.IsNullOrWhiteSpace(token);
            var userRole = HttpContext.Session.GetString("UserRole") ?? "";
            var isAdmin = userRole == "Admin";
            var userPlan = HttpContext.Session.GetString("UserPlan") ?? "";
            var userPlanExpiresAtStr = HttpContext.Session.GetString("UserPlanExpiresAt") ?? "";
            var planIsExpired = DateTimeOffset.TryParse(userPlanExpiresAtStr, out var planExpiry)
                                && planExpiry <= DateTimeOffset.UtcNow;
            var effectivePlan = (planIsExpired && userPlan != "Free") ? "Free" : userPlan;

            var vm = new HomeViewModel
            {
                IsLoggedIn = isLoggedIn,
                IsAdmin = isAdmin,
                UserRole = userRole,
                UserPlan = userPlan,
                PlanIsExpired = planIsExpired,
                EffectivePlan = effectivePlan,
                PlanExpiresAt = planIsExpired || !DateTimeOffset.TryParse(userPlanExpiresAtStr, out var exp)
                    ? (DateTimeOffset?)null
                    : exp,
            };

            // api/languages/active là [AllowAnonymous] – gọi được kể cả khi chưa login
            var languagesTask = _languageApiClient.GetActiveLanguagesAsync(cancellationToken);

            if (isLoggedIn)
            {
                // Gọi song song tất cả endpoint cần auth
                var businessesTask = _businessApiClient.GetBusinessesAsync(1, 1, null, cancellationToken);
                var stallsTask = _stallApiClient.GetStallsAsync(1, 1, null, null, cancellationToken);

                if (isAdmin)
                {
                    var usersTask = _userApiClient.GetUsersAsync(1, 1, null, null, null, cancellationToken);
                    await Task.WhenAll(languagesTask, businessesTask, stallsTask, usersTask);
                    vm.TotalUsers = (await usersTask)?.Data?.TotalCount ?? 0;
                }
                else
                {
                    await Task.WhenAll(languagesTask, businessesTask, stallsTask);
                }

                vm.TotalBusinesses = (await businessesTask)?.Data?.TotalCount ?? 0;
                vm.TotalStalls = (await stallsTask)?.Data?.TotalCount ?? 0;
            }
            else
            {
                await languagesTask;
            }

            vm.TotalLanguages = (await languagesTask)?.Data?.Count ?? 0;

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
