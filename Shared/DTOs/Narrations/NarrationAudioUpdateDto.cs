using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Narrations
{
    public class NarrationAudioUpdateDto : IValidatableObject
    {
        [MaxLength(512, ErrorMessage = "AudioUrl tối đa 512 ký tự")]
        public string? AudioUrl { get; set; }

        [MaxLength(128, ErrorMessage = "BlobId tối đa 128 ký tự")]
        public string? BlobId { get; set; }

        [MaxLength(64, ErrorMessage = "Voice tối đa 64 ký tự")]
        public string? Voice { get; set; }

        [MaxLength(64, ErrorMessage = "Provider tối đa 64 ký tự")]
        public string? Provider { get; set; }

        public int? DurationSeconds { get; set; }

        public bool IsTts { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(AudioUrl) && string.IsNullOrWhiteSpace(BlobId))
            {
                yield return new ValidationResult("AudioUrl hoặc BlobId là bắt buộc", new[] { nameof(AudioUrl), nameof(BlobId) });
            }
        }
    }
}
