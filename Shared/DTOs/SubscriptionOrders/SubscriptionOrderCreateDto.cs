using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.SubscriptionOrders
{
    public class SubscriptionOrderCreateDto
    {
        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [MaxLength(16)]
        public string Plan { get; set; } = null!;          // "Basic" | "Pro"

        [Required]
        public string CardNumber { get; set; } = null!;    // 16 chữ số, có thể có spaces

        [Required]
        [MaxLength(5)]
        public string CardExpiry { get; set; } = null!;    // "MM/YY"

        [Required]
        [MaxLength(4)]
        public string CardCvv { get; set; } = null!;       // 3-4 chữ số

        [MaxLength(128)]
        public string CardHolder { get; set; } = string.Empty;
    }
}
