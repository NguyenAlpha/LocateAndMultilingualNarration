using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.SubscriptionOrders;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly BusinessApiClient _businessApiClient;
        private readonly SubscriptionOrderApiClient _subscriptionOrderApiClient;
        private readonly ApiClient _apiClient;

        public SubscriptionController(
            BusinessApiClient businessApiClient,
            SubscriptionOrderApiClient subscriptionOrderApiClient,
            ApiClient apiClient)
        {
            _businessApiClient = businessApiClient;
            _subscriptionOrderApiClient = subscriptionOrderApiClient;
            _apiClient = apiClient;
        }

        // GET /Subscription/Plans — public
        [HttpGet]
        public async Task<IActionResult> Plans(string? highlight = null, Guid? businessId = null, CancellationToken cancellationToken = default)
        {
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString(ApiClient.TokenSessionKey));

            var hasBusiness = false;
            if (isLoggedIn)
            {
                var businesses = await FetchBusinessSelectItemsAsync(cancellationToken);
                hasBusiness = businesses.Count > 0;
            }

            return View(new PlansViewModel
            {
                IsLoggedIn = isLoggedIn,
                HasBusiness = hasBusiness,
                HighlightPlan = highlight,
                PreselectedBusinessId = businessId,
                ErrorMessage = TempData["ErrorMessage"] as string
            });
        }

        private async Task<List<BusinessSelectItem>> FetchBusinessSelectItemsAsync(CancellationToken ct)
        {
            var result = await _businessApiClient.GetBusinessesAsync(1, 100, null, ct);
            var now = DateTimeOffset.UtcNow;
            return result?.Data?.Items?
                .Select(b =>
                {
                    var isActive = b.Plan != "Free" && b.PlanExpiresAt.HasValue && b.PlanExpiresAt.Value > now;
                    return new BusinessSelectItem
                    {
                        Id = b.Id,
                        Name = b.Name,
                        ActivePlan = isActive ? b.Plan : null,
                        PlanExpiresAt = isActive ? b.PlanExpiresAt : null
                    };
                })
                .ToList() ?? [];
        }

        // GET /Subscription/Checkout?plan=Basic[&businessId=...]
        [HttpGet]
        public async Task<IActionResult> Checkout(string plan, Guid? businessId = null, CancellationToken cancellationToken = default)
        {
            var token = HttpContext.Session.GetString(ApiClient.TokenSessionKey);
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Checkout", "Subscription", new { plan, businessId }) });

            var userRole = HttpContext.Session.GetString(ApiClient.UserRoleSessionKey) ?? "";
            if (userRole != "BusinessOwner" && userRole != "Admin")
                return RedirectToAction("Plans");

            if (plan != "Basic" && plan != "Pro")
                return RedirectToAction("Plans");

            var businesses = await FetchBusinessSelectItemsAsync(cancellationToken);
            if (businesses.Count == 0)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo business trước khi đăng ký gói.";
                return RedirectToAction("Plans", new { plan, businessId });
            }

            // Pre-select business từ query param, fallback về đầu tiên
            var preselected = businessId.HasValue
                ? businesses.FirstOrDefault(b => b.Id == businessId.Value) ?? businesses[0]
                : businesses[0];

            var amount = plan == "Basic" ? 199_000m : 499_000m;

            return View(new CheckoutViewModel
            {
                Plan = plan,
                Amount = amount,
                BusinessId = preselected.Id,
                Businesses = businesses
            });
        }

        // POST /Subscription/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                model.Businesses = await FetchBusinessSelectItemsAsync(cancellationToken);
                model.ErrorMessage = "Vui lòng điền đầy đủ thông tin thẻ.";
                return View("Checkout", model);
            }

            var result = await _subscriptionOrderApiClient.CreateOrderAsync(new SubscriptionOrderCreateDto
            {
                BusinessId = model.BusinessId,
                Plan = model.Plan,
                CardNumber = model.CardNumber.Replace(" ", ""),
                CardExpiry = model.CardExpiry,
                CardCvv = model.CardCvv,
                CardHolder = model.CardHolder
            }, cancellationToken);

            if (result?.Success != true)
            {
                model.Businesses = await FetchBusinessSelectItemsAsync(cancellationToken);
                model.ErrorMessage = result?.Error?.Message ?? "Thanh toán thất bại. Vui lòng thử lại.";
                return View("Checkout", model);
            }

            var order = result.Data!;

            if (order.Status == "Failed")
            {
                model.Businesses = await FetchBusinessSelectItemsAsync(cancellationToken);
                model.ErrorMessage = "Thẻ không hợp lệ. Vui lòng kiểm tra lại số thẻ (phải đủ 16 chữ số).";
                return View("Checkout", model);
            }

            // Cập nhật session plan để badge sidebar refresh ngay
            _apiClient.StoreUserPlan(order.Plan, order.PlanEndAt);

            return RedirectToAction("Success", new { plan = order.Plan, orderId = order.Id });
        }

        // GET /Subscription/Success
        [HttpGet]
        public IActionResult Success(string plan, Guid orderId)
        {
            ViewBag.Plan = plan;
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}
