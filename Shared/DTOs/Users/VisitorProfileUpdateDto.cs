using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Users
{
    public class VisitorProfileUpdateDto
    {
        [Required(ErrorMessage = "LanguageId là bắt buộc")]
        public Guid LanguageId { get; set; }
    }
}
