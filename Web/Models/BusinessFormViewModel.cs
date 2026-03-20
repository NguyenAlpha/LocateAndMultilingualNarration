using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class BusinessFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Tên business là bắt buộc.")]
        [MaxLength(256, ErrorMessage = "Tên business tối đa 256 ký tự.")]
        [Display(Name = "Tên business")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(32, ErrorMessage = "Mã số thuế tối đa 32 ký tự.")]
        [Display(Name = "Mã số thuế")]
        public string? TaxCode { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
        [Display(Name = "Email liên hệ")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [MaxLength(32, ErrorMessage = "Số điện thoại tối đa 32 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string? ContactPhone { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}
