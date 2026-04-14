using Api.Domain;
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Seeds
{
    public static class SubscriptionOrderSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Idempotent: skip nếu đã có orders
            if (await db.SubscriptionOrders.AnyAsync())
                return;

            var random = new Random(42);
            var now = DateTimeOffset.UtcNow;

            var businesses = await db.Businesses.ToListAsync();

            foreach (var business in businesses)
            {
                var availableDays = (now - business.CreatedAt).TotalDays;
                if (availableDays < 1)
                    continue;

                // Phân phối:
                //  0–39  (40%) → không có order, giữ Free
                // 40–74  (35%) → 1 completed order (có thể đã hết hạn hoặc còn hạn)
                // 75–89  (15%) → 2–3 renewal orders nối tiếp nhau
                // 90–99  (10%) → chỉ failed orders (thử đăng ký nhưng thất bại)
                int roll = random.Next(100);

                if (roll < 40)
                    continue;

                string plan = random.Next(2) == 0 ? SubscriptionPlan.Basic : SubscriptionPlan.Pro;

                if (roll < 90)
                {
                    bool hasRenewals = roll >= 75 && availableDays >= 60;
                    int maxOrders = hasRenewals ? random.Next(2, 4) : 1;

                    // Điểm bắt đầu của order đầu tiên: random sau khi business được tạo
                    // Với renewal, cần đủ chỗ cho ít nhất 1 order hết hạn trước order cuối
                    double latestStart = hasRenewals
                        ? availableDays - 30  // đảm bảo order đầu không phải là order còn hạn
                        : availableDays;
                    latestStart = Math.Max(0, latestStart);

                    var firstStart = business.CreatedAt.AddDays(random.NextDouble() * latestStart);
                    DateTimeOffset currentStart = firstStart;
                    DateTimeOffset? lastEndAt = null;
                    bool anyActive = false;

                    for (int r = 0; r < maxOrders; r++)
                    {
                        // Order sau bắt đầu ngay khi order trước kết thúc (renewal)
                        var orderStart = r == 0 ? currentStart : lastEndAt!.Value;

                        // Không tạo order bắt đầu trong tương lai
                        if (orderStart > now)
                            break;

                        var orderEnd  = orderStart.AddDays(30);
                        var paidAt    = orderStart.AddMinutes(random.Next(1, 30));
                        bool isActive = orderEnd > now;

                        db.SubscriptionOrders.Add(new SubscriptionOrder
                        {
                            Id             = Guid.NewGuid(),
                            BusinessId     = business.Id,
                            Plan           = plan,
                            Amount         = SubscriptionPlan.GetPrice(plan),
                            Currency       = "VND",
                            DurationMonths = 1,
                            Status         = "Completed",
                            PaymentMethod  = "MockCard",
                            CardLastFour   = random.Next(1000, 9999).ToString(),
                            CreatedAt      = paidAt,
                            PaidAt         = paidAt,
                            PlanStartAt    = orderStart,
                            PlanEndAt      = orderEnd
                        });

                        lastEndAt = orderEnd;
                        if (isActive) { anyActive = true; break; } // order còn hạn → không gia hạn thêm
                    }

                    // Cập nhật plan trên business nếu order cuối vẫn còn hạn
                    if (anyActive && lastEndAt.HasValue)
                    {
                        business.Plan         = plan;
                        business.PlanExpiresAt = lastEndAt.Value;
                    }

                    // 25% chance: thêm 1 failed order (nhập sai thẻ trước hoặc sau khi mua thành công)
                    if (random.Next(100) < 25)
                        AddFailedOrder(db, random, business, now);
                }
                else
                {
                    // Chỉ failed orders
                    int failedCount = random.Next(1, 3);
                    for (int f = 0; f < failedCount; f++)
                        AddFailedOrder(db, random, business, now);
                }
            }

            await db.SaveChangesAsync();
        }

        private static void AddFailedOrder(AppDbContext db, Random random, Business business, DateTimeOffset now)
        {
            var availableDays = (now - business.CreatedAt).TotalDays;
            if (availableDays < 1) return;

            string plan      = random.Next(2) == 0 ? SubscriptionPlan.Basic : SubscriptionPlan.Pro;
            var attemptAt    = business.CreatedAt.AddDays(random.NextDouble() * availableDays);

            db.SubscriptionOrders.Add(new SubscriptionOrder
            {
                Id             = Guid.NewGuid(),
                BusinessId     = business.Id,
                Plan           = plan,
                Amount         = SubscriptionPlan.GetPrice(plan),
                Currency       = "VND",
                DurationMonths = 1,
                Status         = "Failed",
                PaymentMethod  = "MockCard",
                CardLastFour   = random.Next(1000, 9999).ToString(),
                CreatedAt      = attemptAt,
                PaidAt         = null,
                PlanStartAt    = attemptAt,
                PlanEndAt      = attemptAt.AddDays(30)
            });
        }
    }
}
