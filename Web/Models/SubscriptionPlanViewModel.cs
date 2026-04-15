using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class PlansViewModel
    {
        public bool IsLoggedIn { get; set; }
        public bool HasBusiness { get; set; }
        public string? HighlightPlan { get; set; }
        public Guid? PreselectedBusinessId { get; set; }  // từ Business Management
        public string? ErrorMessage { get; set; }
    }

    public class BusinessSelectItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ActivePlan { get; set; }       // null hoặc "Free" nếu không có plan đang active
        public DateTimeOffset? PlanExpiresAt { get; set; }
    }

    public class CheckoutViewModel
    {
        public string Plan { get; set; } = null!;
        public decimal Amount { get; set; }
        public Guid BusinessId { get; set; }
        public List<BusinessSelectItem> Businesses { get; set; } = [];

        [Required(ErrorMessage = "Vui lòng nhập số thẻ")]
        [Display(Name = "Số thẻ")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập ngày hết hạn")]
        [Display(Name = "Ngày hết hạn (MM/YY)")]
        public string CardExpiry { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập CVV")]
        [Display(Name = "CVV")]
        public string CardCvv { get; set; } = string.Empty;

        [Display(Name = "Tên chủ thẻ")]
        public string CardHolder { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    public class SubscriptionOrdersViewModel
    {
        public IReadOnlyList<Shared.DTOs.SubscriptionOrders.SubscriptionOrderDetailDto> Items { get; set; } = [];
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? FilterPlan { get; set; }
        public string? FilterStatus { get; set; }

        // Revenue stats (tính từ Items hiện tại — Admin sẽ thấy tổng từ API)
        public decimal TotalRevenue { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalFailed { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
