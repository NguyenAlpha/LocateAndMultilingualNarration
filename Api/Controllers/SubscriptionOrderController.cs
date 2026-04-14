using Api.Authorization;
using Api.Domain;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.SubscriptionOrders;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/subscription-orders")]
    [Authorize]
    public class SubscriptionOrderController : AppControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly ILogger<SubscriptionOrderController> _logger;

        public SubscriptionOrderController(AppDbContext context, ILogger<SubscriptionOrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo đơn thanh toán mock và kích hoạt plan
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOrBusinessOwner)]
        public async Task<IActionResult> CreateOrder([FromBody] SubscriptionOrderCreateDto request)
        {
            _logger.LogInformation("Tạo đơn subscription - BusinessId: {BusinessId}, Plan: {Plan}", request.BusinessId, request.Plan);

            if (!TryGetUserId(out var userId))
                return this.UnauthorizedResult("Không xác thực");

            // Chỉ Basic và Pro mới được đăng ký trả phí
            var validPlans = new[] { SubscriptionPlan.Basic, SubscriptionPlan.Pro };
            if (!validPlans.Contains(request.Plan))
                return this.BadRequestResult("Chỉ có thể đăng ký gói Basic hoặc Pro", "Plan");

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == request.BusinessId);
            if (business == null)
                return this.NotFoundResult("Không tìm thấy business");

            if (!IsAdmin() && business.OwnerUserId != userId)
                return this.ForbiddenResult("Không có quyền truy cập");

            // Chặn downgrade khi plan hiện tại còn hạn
            static int PlanRank(string p) => p switch { "Pro" => 2, "Basic" => 1, _ => 0 };
            var hasActivePlan = business.Plan != SubscriptionPlan.Free
                                && business.PlanExpiresAt.HasValue
                                && business.PlanExpiresAt.Value > DateTimeOffset.UtcNow;
            if (hasActivePlan && PlanRank(request.Plan) < PlanRank(business.Plan))
                return this.BadRequestResult(
                    $"Không thể đăng ký gói {request.Plan} khi đang dùng gói {business.Plan} còn hạn. Vui lòng chờ hết hạn hoặc gia hạn/nâng cấp gói hiện tại.",
                    "Plan");

            // Mock card validation: strip spaces, phải đúng 16 chữ số
            var cardStripped = request.CardNumber.Replace(" ", "").Replace("-", "");
            var paymentSuccess = cardStripped.Length == 16 && cardStripped.All(char.IsDigit);
            var status = paymentSuccess ? "Completed" : "Failed";
            var cardLastFour = cardStripped.Length >= 4 ? cardStripped[^4..] : null;

            // Tính thời gian plan
            var now = DateTimeOffset.UtcNow;
            var planStartAt = (business.Plan != SubscriptionPlan.Free && business.PlanExpiresAt.HasValue && business.PlanExpiresAt.Value > now)
                ? business.PlanExpiresAt.Value   // extend từ ngày hết hạn hiện tại
                : now;
            var planEndAt = planStartAt.AddMonths(1);

            var order = new Api.Domain.Entities.SubscriptionOrder
            {
                BusinessId = request.BusinessId,
                Plan = request.Plan,
                Amount = SubscriptionPlan.GetPrice(request.Plan),
                Currency = "VND",
                DurationMonths = 1,
                Status = status,
                PaymentMethod = "MockCard",
                CardLastFour = cardLastFour,
                CreatedAt = now,
                PaidAt = paymentSuccess ? now : null,
                PlanStartAt = planStartAt,
                PlanEndAt = planEndAt
            };

            _context.SubscriptionOrders.Add(order);

            if (paymentSuccess)
            {
                business.Plan = request.Plan;
                business.PlanExpiresAt = planEndAt;
                _logger.LogInformation("Kích hoạt plan {Plan} cho BusinessId: {BusinessId}, hết hạn: {EndAt}", request.Plan, request.BusinessId, planEndAt);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tạo đơn subscription {Status} - OrderId: {OrderId}", status, order.Id);

            return this.OkResult(MapDetail(order, business.Name));
        }

        /// <summary>
        /// Lấy danh sách đơn subscription (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? plan = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? businessId = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.SubscriptionOrders
                .Include(o => o.Business)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(plan))
                query = query.Where(o => o.Plan == plan);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            if (businessId.HasValue)
                query = query.Where(o => o.BusinessId == businessId.Value);

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var timeZone = GetTimeZone();
            var items = orders.Select(o => MapDetail(o, o.Business.Name, timeZone)).ToList();

            return this.OkResult(new PagedResult<SubscriptionOrderDetailDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        private static SubscriptionOrderDetailDto MapDetail(
            Api.Domain.Entities.SubscriptionOrder o,
            string businessName,
            TimeZoneInfo? timeZone = null)
        {
            timeZone ??= TimeZoneInfo.Utc;
            return new SubscriptionOrderDetailDto
            {
                Id = o.Id,
                BusinessId = o.BusinessId,
                BusinessName = businessName,
                Plan = o.Plan,
                Amount = o.Amount,
                Currency = o.Currency,
                DurationMonths = o.DurationMonths,
                Status = o.Status,
                CardLastFour = o.CardLastFour,
                CreatedAt = ConvertFromUtc(o.CreatedAt, timeZone),
                PaidAt = o.PaidAt.HasValue ? ConvertFromUtc(o.PaidAt.Value, timeZone) : null,
                PlanStartAt = ConvertFromUtc(o.PlanStartAt, timeZone),
                PlanEndAt = ConvertFromUtc(o.PlanEndAt, timeZone)
            };
        }
    }
}
