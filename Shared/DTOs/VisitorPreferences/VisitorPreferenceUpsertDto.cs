using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.VisitorPreferences
{
    public class VisitorPreferenceUpsertDto
    {
        [Required(ErrorMessage = "LanguageId là bắt buộc")]
        public Guid LanguageId { get; set; }

        [MaxLength(64, ErrorMessage = "Voice tối đa 64 ký tự")]
        public string? Voice { get; set; }

        public decimal SpeechRate { get; set; }

        public bool AutoPlay { get; set; }
    }
}
