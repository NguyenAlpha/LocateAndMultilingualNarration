namespace Api.Domain.Entities
{
    public class SubscriptionOrder
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Plan { get; set; } = null!;           // "Basic" | "Pro"
        public decimal Amount { get; set; }                  // 199000 | 499000
        public string Currency { get; set; } = "VND";
        public int DurationMonths { get; set; } = 1;
        public string Status { get; set; } = null!;         // "Completed" | "Failed"
        public string PaymentMethod { get; set; } = "MockCard";
        public string? CardLastFour { get; set; }            // 4 chữ số cuối của thẻ
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset PlanStartAt { get; set; }
        public DateTimeOffset PlanEndAt { get; set; }

        // Navigation
        public Business Business { get; set; } = null!;
    }
}
