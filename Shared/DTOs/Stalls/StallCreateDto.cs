using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Stalls
{
    public class StallCreateDto
    {
        [Required(ErrorMessage = "BusinessId là bắt buộc")]
        public Guid BusinessId { get; set; }

        [Required(ErrorMessage = "Stall name là bắt buộc")]
        [MaxLength(128, ErrorMessage = "Stall name tối đa 128 ký tự")]
        public string Name { get; set; } = null!;

        [MaxLength(256, ErrorMessage = "Description tối đa 256 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Slug là bắt buộc")]
        [MaxLength(256, ErrorMessage = "Slug tối đa 256 ký tự")]
        [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug phải là chữ thường và chỉ gồm chữ, số hoặc dấu gạch ngang")]
        public string Slug { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(256, ErrorMessage = "ContactEmail tối đa 256 ký tự")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(16, ErrorMessage = "ContactPhone tối đa 16 ký tự")]
        public string? ContactPhone { get; set; }

    }
}
