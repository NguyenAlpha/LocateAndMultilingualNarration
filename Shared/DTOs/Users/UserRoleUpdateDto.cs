using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Users
{
    public class UserRoleUpdateDto
    {
        [Required] public string RoleName { get; set; } = null!;
    }
}
