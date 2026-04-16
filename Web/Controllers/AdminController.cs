using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Businesses;
using Shared.DTOs.QrCodes;
using Shared.DTOs.Users;
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
        private readonly SubscriptionApiClient _subscriptionApiClient;
        private readonly SubscriptionOrderApiClient _subscriptionOrderApiClient;
        private readonly UserApiClient _userApiClient;
        private readonly QrCodeApiClient _qrCodeApiClient;

        public AdminController(
            BusinessApiClient businessApiClient,
            StallApiClient stallApiClient,
            LanguageApiClient languageApiClient,
            StallNarrationContentApiClient narrationContentApiClient,
            SubscriptionApiClient subscriptionApiClient,
            SubscriptionOrderApiClient subscriptionOrderApiClient,
            UserApiClient userApiClient,
            QrCodeApiClient qrCodeApiClient)
        {
            _businessApiClient = businessApiClient;
            _stallApiClient = stallApiClient;
            _languageApiClient = languageApiClient;
            _narrationContentApiClient = narrationContentApiClient;
            _subscriptionApiClient = subscriptionApiClient;
            _subscriptionOrderApiClient = subscriptionOrderApiClient;
            _userApiClient = userApiClient;
            _qrCodeApiClient = qrCodeApiClient;
        }

        public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        {
            // Gọi song song để giảm latency
            var businessesTask      = _businessApiClient.GetBusinessesAsync(1, 5, null, cancellationToken);
            var stallsTask          = _stallApiClient.GetStallsAsync(1, 5, null, null, cancellationToken);
            var languagesTask       = _languageApiClient.GetActiveLanguagesAsync(cancellationToken);
            var narrationTask       = _narrationContentApiClient.GetContentsAsync(1, 1, null, null, null, null, cancellationToken);
            var usersTask           = _userApiClient.GetUsersAsync(1, 1, null, null, null, cancellationToken);
            var qrTotalTask         = _qrCodeApiClient.GetQrCodesAsync(1, 1, null, null, cancellationToken);
            var qrUsedTask          = _qrCodeApiClient.GetQrCodesAsync(1, 1, true, null, cancellationToken);
            // pageSize=100 để lấy TotalCount + tính tổng doanh thu (tối đa 100 đơn đầu)
            var completedOrdersTask = _subscriptionOrderApiClient.GetOrdersAsync(1, 100, null, "Completed", null, cancellationToken);
            var recentOrdersTask    = _subscriptionOrderApiClient.GetOrdersAsync(1, 5, null, null, null, cancellationToken);

            await Task.WhenAll(businessesTask, stallsTask, languagesTask, narrationTask,
                usersTask, qrTotalTask, qrUsedTask, completedOrdersTask, recentOrdersTask);

            var businesses      = await businessesTask;
            var stalls          = await stallsTask;
            var languages       = await languagesTask;
            var narrations      = await narrationTask;
            var users           = await usersTask;
            var qrTotal         = await qrTotalTask;
            var qrUsed          = await qrUsedTask;
            var completedOrders = await completedOrdersTask;
            var recentOrders    = await recentOrdersTask;

            var vm = new AdminDashboardViewModel
            {
                TotalBusinesses       = businesses?.Data?.TotalCount ?? 0,
                TotalStalls           = stalls?.Data?.TotalCount ?? 0,
                ActiveLanguages       = languages?.Data?.Count ?? 0,
                TotalNarrationContents = narrations?.Data?.TotalCount ?? 0,
                TotalUsers            = users?.Data?.TotalCount ?? 0,
                TotalQrCodes          = qrTotal?.Data?.TotalCount ?? 0,
                UsedQrCodes           = qrUsed?.Data?.TotalCount ?? 0,
                CompletedOrders       = completedOrders?.Data?.TotalCount ?? 0,
                TotalRevenue          = completedOrders?.Data?.Items?.Sum(o => o.Amount) ?? 0,
                RecentBusinesses      = businesses?.Data?.Items?.ToList() ?? [],
                RecentStalls          = stalls?.Data?.Items?.ToList() ?? [],
                Languages             = languages?.Data?.ToList() ?? [],
                RecentOrders          = recentOrders?.Data?.Items?.ToList() ?? [],
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> SubscriptionOrders(
            int page = 1, int pageSize = 30,
            string? plan = null, string? status = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _subscriptionOrderApiClient.GetOrdersAsync(page, pageSize, plan, status, null, cancellationToken);
            var items = result?.Data?.Items?.ToList() ?? [];

            var vm = new SubscriptionOrdersViewModel
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = result?.Data?.TotalCount ?? 0,
                FilterPlan = plan,
                FilterStatus = status,
                TotalRevenue = items.Where(o => o.Status == "Completed").Sum(o => o.Amount),
                TotalCompleted = items.Count(o => o.Status == "Completed"),
                TotalFailed = items.Count(o => o.Status == "Failed"),
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> UserRoleManagement(
            int page = 1, int pageSize = 20,
            string? search = null, string? roleFilter = null, bool? isActiveFilter = null,
            CancellationToken cancellationToken = default)
        {
            var usersTask = _userApiClient.GetUsersAsync(page, pageSize, search, roleFilter, isActiveFilter, cancellationToken);
            var rolesTask = _userApiClient.GetRolesAsync(cancellationToken);
            await Task.WhenAll(usersTask, rolesTask);

            var usersResult = await usersTask;
            var rolesResult = await rolesTask;

            var vm = new UserRoleManagementViewModel
            {
                Users = usersResult?.Data?.Items?.ToList() ?? [],
                Roles = rolesResult?.Data ?? [],
                Page = usersResult?.Data?.Page ?? page,
                PageSize = usersResult?.Data?.PageSize ?? pageSize,
                TotalCount = usersResult?.Data?.TotalCount ?? 0,
                Search = search,
                RoleFilter = roleFilter,
                IsActiveFilter = isActiveFilter,
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreateUser(
            [Bind("UserName,Email,Password,PhoneNumber,RoleName")] AdminCreateUserDto model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(UserRoleManagement));
            }

            var result = await _userApiClient.AdminCreateUserAsync(model, cancellationToken);
            if (result?.Success != true)
            {
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Tạo user thất bại.";
                return RedirectToAction(nameof(UserRoleManagement));
            }

            TempData["SuccessMessage"] = $"Đã tạo user \"{model.UserName}\" thành công.";
            return RedirectToAction(nameof(UserRoleManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(
            Guid id, int page = 1, int pageSize = 20,
            string? search = null, string? roleFilter = null, bool? isActiveFilter = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _userApiClient.ToggleUserActiveAsync(id, cancellationToken);
            if (result?.Success != true)
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Không thể thay đổi trạng thái.";
            else
                TempData["SuccessMessage"] = "Đã cập nhật trạng thái user.";

            return RedirectToAction(nameof(UserRoleManagement), new { page, pageSize, search, roleFilter, isActiveFilter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(
            Guid id, string roleName, int page = 1, int pageSize = 20,
            string? search = null, string? roleFilter = null, bool? isActiveFilter = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _userApiClient.UpdateUserRoleAsync(id, new UserRoleUpdateDto { RoleName = roleName }, cancellationToken);
            if (result?.Success != true)
                TempData["ErrorMessage"] = result?.Error?.Message ?? "Không thể đổi role.";
            else
                TempData["SuccessMessage"] = "Đã cập nhật role.";

            return RedirectToAction(nameof(UserRoleManagement), new { page, pageSize, search, roleFilter, isActiveFilter });
        }

        [HttpGet]
        public async Task<IActionResult> QrCodes(
            int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _qrCodeApiClient.GetQrCodesAsync(page, pageSize, ct: cancellationToken);
            var items = result?.Data?.Items?.ToList() ?? [];

            var vm = new AdminQrCodesViewModel
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = result?.Data?.TotalCount ?? 0,
                TotalUsed = items.Count(q => q.IsUsed),
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQrCode(
            QrCodeCreateDto model, int page = 1, int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid || model.ValidDays <= 0)
            {
                var result = await _qrCodeApiClient.GetQrCodesAsync(page, pageSize, ct: cancellationToken);
                var items = result?.Data?.Items?.ToList() ?? [];
                var vm = new AdminQrCodesViewModel
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = result?.Data?.TotalCount ?? 0,
                    TotalUsed = items.Count(q => q.IsUsed),
                    Create = model,
                    ShowCreateModal = true,
                    ErrorMessage = "Số ngày hiệu lực không hợp lệ."
                };
                return View("QrCodes", vm);
            }

            var apiResult = await _qrCodeApiClient.CreateQrCodeAsync(model, cancellationToken);
            if (apiResult?.Success != true)
            {
                TempData["ErrorMessage"] = apiResult?.Error?.Message ?? "Tạo mã QR thất bại.";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã tạo mã QR thành công.";
            }

            return RedirectToAction(nameof(QrCodes), new { page, pageSize });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQrCode(
            Guid id, int page = 1, int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await _qrCodeApiClient.DeleteQrCodeAsync(id, cancellationToken);
            if (response?.Success != true)
                TempData["ErrorMessage"] = response?.Error?.Message ?? "Xoá mã QR thất bại.";
            else
                TempData["SuccessMessage"] = "Đã xoá mã QR.";

            return RedirectToAction(nameof(QrCodes), new { page, pageSize });
        }

        [HttpGet]
        public async Task<IActionResult> GetQrImage(Guid id, CancellationToken cancellationToken = default)
        {
            var bytes = await _qrCodeApiClient.GetQrCodeImageAsync(id, cancellationToken);
            if (bytes is null) return NotFound();
            return File(bytes, "image/png", $"qr-{id}.png");
        }

        [HttpGet]
        public IActionResult AutoQr() => View();

        [HttpPost]
        public async Task<IActionResult> StartAutoQr(
            [FromBody] Shared.DTOs.QrCodes.QrCodeCreateDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await _qrCodeApiClient.CreateQrCodeAsync(request, cancellationToken);
            if (result?.Success != true || result.Data is null)
                return BadRequest(new { error = result?.Error?.Message ?? "Tạo mã QR thất bại." });

            return Ok(new
            {
                id       = result.Data.Id,
                code     = result.Data.Code,
                imageUrl = Url.Action("GetQrImage", "Admin", new { id = result.Data.Id })
            });
        }

        [HttpGet]
        public async Task<IActionResult> PollAutoQr(
            Guid id, int validDays,
            CancellationToken cancellationToken = default)
        {
            var result = await _qrCodeApiClient.GetQrCodeAsync(id, cancellationToken);
            if (result?.Success != true || result.Data is null)
                return NotFound();

            if (!result.Data.IsUsed)
                return Ok(new { used = false });

            // QR đã được quét → tạo QR mới tự động
            var newResult = await _qrCodeApiClient.CreateQrCodeAsync(
                new Shared.DTOs.QrCodes.QrCodeCreateDto { ValidDays = validDays },
                cancellationToken);

            if (newResult?.Success != true || newResult.Data is null)
                return BadRequest(new { error = "Tạo mã QR mới thất bại." });

            return Ok(new
            {
                used     = true,
                id       = newResult.Data.Id,
                code     = newResult.Data.Code,
                imageUrl = Url.Action("GetQrImage", "Admin", new { id = newResult.Data.Id })
            });
        }

        [HttpGet]
        public async Task<IActionResult> Subscription(
            int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
        {
            var result = await _businessApiClient.GetBusinessesAsync(page, pageSize, search, cancellationToken);

            var vm = new SubscriptionManagementViewModel
            {
                Items = result?.Data?.Items?.ToList() ?? [],
                Page = page,
                PageSize = pageSize,
                TotalCount = result?.Data?.TotalCount ?? 0,
                Search = search,
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscription(
            SubscriptionFormViewModel model,
            int page = 1, int pageSize = 10, string? search = null,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var result = await _businessApiClient.GetBusinessesAsync(page, pageSize, search, cancellationToken);
                var vm = new SubscriptionManagementViewModel
                {
                    Items = result?.Data?.Items?.ToList() ?? [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = result?.Data?.TotalCount ?? 0,
                    Search = search,
                    Edit = model,
                    ShowEditModal = true,
                    ErrorMessage = "Dữ liệu không hợp lệ."
                };
                return View("Subscription", vm);
            }

            var apiResult = await _subscriptionApiClient.UpdateSubscriptionAsync(
                model.BusinessId,
                new SubscriptionUpdateDto { Plan = model.Plan, PlanExpiresAt = model.PlanExpiresAt },
                cancellationToken);

            if (apiResult?.Success != true)
            {
                var result = await _businessApiClient.GetBusinessesAsync(page, pageSize, search, cancellationToken);
                var vm = new SubscriptionManagementViewModel
                {
                    Items = result?.Data?.Items?.ToList() ?? [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = result?.Data?.TotalCount ?? 0,
                    Search = search,
                    Edit = model,
                    ShowEditModal = true,
                    ErrorMessage = apiResult?.Error?.Message ?? "Cập nhật gói thất bại."
                };
                return View("Subscription", vm);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật gói {model.Plan} cho \"{model.BusinessName}\" thành công.";
            return RedirectToAction(nameof(Subscription), new { page, pageSize, search });
        }
    }
}
