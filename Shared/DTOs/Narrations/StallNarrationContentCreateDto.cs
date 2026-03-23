using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Narrations
{
    public class StallNarrationContentCreateDto
    {
        [Required(ErrorMessage = "StallId là bắt buộc")]
        public Guid StallId { get; set; }

        [Required(ErrorMessage = "LanguageId là bắt buộc")]
        public Guid LanguageId { get; set; }

        [Required(ErrorMessage = "Title là bắt buộc")]
        [MaxLength(128, ErrorMessage = "Title tối đa 128 ký tự")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(256, ErrorMessage = "Description tối đa 256 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "ScriptText là bắt buộc")]
        public string ScriptText { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
