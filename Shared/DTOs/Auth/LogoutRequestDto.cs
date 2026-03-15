using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Auth
{
    public class LogoutRequestDto
    {
        [Required(ErrorMessage = "RefreshToken là bắt buộc")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
