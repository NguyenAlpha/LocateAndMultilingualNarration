using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Tours
{
    public class TourStopReorderDto
    {
        [Required]
        public Guid StallId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Order phải là số dương")]
        public int Order { get; set; }
    }
}
