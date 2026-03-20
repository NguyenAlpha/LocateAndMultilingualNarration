using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Businesses
{
    public class BusinessCreateDto
    {
        [Required(ErrorMessage = "Business name là bắt buộc")]
        [MaxLength(256, ErrorMessage = "Business name tối đa 256 ký tự")]
        public string Name { get; set; } = null!;

        [MaxLength(32, ErrorMessage = "TaxCode tối đa 32 ký tự")]
        public string? TaxCode { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(256, ErrorMessage = "ContactEmail tối đa 256 ký tự")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(32, ErrorMessage = "ContactPhone tối đa 32 ký tự")]
        public string? ContactPhone { get; set; }

    }
}
