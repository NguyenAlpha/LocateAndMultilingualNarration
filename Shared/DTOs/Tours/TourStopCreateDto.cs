using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Tours
{
    public class TourStopCreateDto
    {
        [Required(ErrorMessage = "StallId là bắt buộc")]
        public Guid StallId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Order phải là số dương")]
        public int Order { get; set; }

        [MaxLength(256, ErrorMessage = "Note tối đa 256 ký tự")]
        public string? Note { get; set; }
    }
}
