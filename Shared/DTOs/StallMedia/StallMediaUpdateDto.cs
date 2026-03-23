using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.StallMedia
{
    public class StallMediaUpdateDto
    {
        [Required(ErrorMessage = "MediaUrl là bắt buộc")]
        [MaxLength(512, ErrorMessage = "MediaUrl tối đa 512 ký tự")]
        public string MediaUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "MediaType là bắt buộc")]
        [MaxLength(32, ErrorMessage = "MediaType tối đa 32 ký tự")]
        public string MediaType { get; set; } = string.Empty;

        [MaxLength(256, ErrorMessage = "Caption tối đa 256 ký tự")]
        public string? Caption { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }
}
