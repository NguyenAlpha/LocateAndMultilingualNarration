using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Businesses
{
    public class SubscriptionUpdateDto
    {
        [Required]
        [MaxLength(16)]
        public string Plan { get; set; } = "Free";

        public DateTimeOffset? PlanExpiresAt { get; set; }
    }
}
