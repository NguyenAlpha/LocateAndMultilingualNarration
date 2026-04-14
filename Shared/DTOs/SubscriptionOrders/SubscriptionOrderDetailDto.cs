namespace Shared.DTOs.SubscriptionOrders
{
    public class SubscriptionOrderDetailDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = null!;
        public string Plan { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationMonths { get; set; }
        public string Status { get; set; } = null!;       // "Completed" | "Failed"
        public string? CardLastFour { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset PlanStartAt { get; set; }
        public DateTimeOffset PlanEndAt { get; set; }
    }
}
