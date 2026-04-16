using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "User name là bắt buộc.")]
        [MinLength(3, ErrorMessage = "User name phải có ít nhất 3 ký tự.")]
        [MaxLength(50, ErrorMessage = "User name tối đa 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "User name chỉ được chứa chữ cái, số và dấu gạch dưới.")]
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^(\+84|0)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678 hoặc +84912345678).")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [MaxLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường và 1 chữ số.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        //[Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với điều khoản.")]
        //[Display(Name = "Đồng ý điều khoản")]
        //public bool AgreeTerms { get; set; }
    }
}
