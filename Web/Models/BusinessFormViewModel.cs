using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class BusinessFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Tên business là bắt buộc.")]
        [MinLength(2, ErrorMessage = "Tên business phải có ít nhất 2 ký tự.")]
        [MaxLength(256, ErrorMessage = "Tên business tối đa 256 ký tự.")]
        [Display(Name = "Tên business")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(14, ErrorMessage = "Mã số thuế tối đa 14 ký tự.")]
        [RegularExpression(@"^\d{10}(\d{3})?(-\d)?$", ErrorMessage = "Mã số thuế không hợp lệ (10 hoặc 13 chữ số).")]
        [Display(Name = "Mã số thuế")]
        public string? TaxCode { get; set; }

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
