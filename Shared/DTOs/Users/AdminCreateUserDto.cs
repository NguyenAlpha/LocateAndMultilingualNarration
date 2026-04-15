using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Users
{
    public class AdminCreateUserDto
    {
        [Required][MaxLength(100)] public string UserName { get; set; } = null!;
        [Required][EmailAddress]   public string Email { get; set; } = null!;
        [Required][MinLength(6)]   public string Password { get; set; } = null!;
        [MaxLength(20)]            public string? PhoneNumber { get; set; }
        [Required]                 public string RoleName { get; set; } = null!;
    }
}
