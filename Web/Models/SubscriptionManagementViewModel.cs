using Shared.DTOs.Businesses;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class SubscriptionManagementViewModel
    {
        public IReadOnlyList<BusinessDetailDto> Items { get; set; } = [];
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string? Search { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public SubscriptionFormViewModel Edit { get; set; } = new();
        public bool ShowEditModal { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public class SubscriptionFormViewModel
    {
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn gói")]
        public string Plan { get; set; } = "Free";

        public DateTimeOffset? PlanExpiresAt { get; set; }
    }
}
