using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Tours
{
    public class TourUpdateDto
    {
        [Required(ErrorMessage = "Tour name là bắt buộc")]
        [MaxLength(128, ErrorMessage = "Tour name tối đa 128 ký tự")]
        public string Name { get; set; } = null!;

        [MaxLength(1024, ErrorMessage = "Description tối đa 1024 ký tự")]
        public string? Description { get; set; }

        [Range(1, 1440, ErrorMessage = "EstimatedMinutes phải từ 1 đến 1440")]
        public int? EstimatedMinutes { get; set; }

        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Stops là bắt buộc")]
        [MinLength(1, ErrorMessage = "Tour phải có ít nhất 1 stop")]
        public List<TourStopCreateDto> Stops { get; set; } = new();
    }
}
