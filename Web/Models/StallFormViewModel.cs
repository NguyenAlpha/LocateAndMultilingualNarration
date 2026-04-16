using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class StallFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Business là bắt buộc.")]
        [Display(Name = "Business")]
        public Guid BusinessId { get; set; }

        [Required(ErrorMessage = "Tên stall là bắt buộc.")]
        [MinLength(2, ErrorMessage = "Tên stall phải có ít nhất 2 ký tự.")]
        [MaxLength(128, ErrorMessage = "Tên stall tối đa 128 ký tự.")]
        [Display(Name = "Tên stall")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256, ErrorMessage = "Mô tả tối đa 256 ký tự.")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Slug là bắt buộc.")]
        [MinLength(2, ErrorMessage = "Slug phải có ít nhất 2 ký tự.")]
        [MaxLength(128, ErrorMessage = "Slug tối đa 128 ký tự.")]
        [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug phải là chữ thường và chỉ gồm chữ, số hoặc dấu gạch ngang.")]
        [Display(Name = "Slug")]
        public string Slug { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
        [Display(Name = "Email liên hệ")]
        public string? ContactEmail { get; set; }

        [RegularExpression(@"^(\+84|0)[0-9]{8,11}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678 hoặc +84912345678).")]
        [MaxLength(16, ErrorMessage = "Số điện thoại tối đa 16 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string? ContactPhone { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}
