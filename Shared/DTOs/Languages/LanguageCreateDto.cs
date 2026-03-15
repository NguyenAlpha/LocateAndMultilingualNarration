using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Languages
{
    public class LanguageCreateDto
    {
        [Required(ErrorMessage = "Language code là bắt buộc")]
        [MaxLength(16, ErrorMessage = "Language code tối đa 16 ký tự")]
        [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Language code phải là chữ thường và chỉ gồm chữ, số hoặc dấu gạch ngang")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Language name là bắt buộc")]
        [MaxLength(64, ErrorMessage = "Language name tối đa 64 ký tự")]
        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }
}
