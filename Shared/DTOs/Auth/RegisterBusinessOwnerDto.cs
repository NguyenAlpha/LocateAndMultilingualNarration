using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Auth
{
    public class RegisterBusinessOwnerDto
    {
        [Required(ErrorMessage = "UserName là bắt buộc")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "PhoneNumber là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = null!;
    }
}
