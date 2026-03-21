using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.StallLocations
{
    public class StallLocationUpdateDto
    {
        [Required(ErrorMessage = "Latitude là bắt buộc")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Longitude là bắt buộc")]
        public decimal Longitude { get; set; }

        public decimal RadiusMeters { get; set; }

        [MaxLength(256, ErrorMessage = "Address tối đa 256 ký tự")]
        public string? Address { get; set; }

        [MaxLength(128, ErrorMessage = "MapProviderPlaceId tối đa 128 ký tự")]
        public string? MapProviderPlaceId { get; set; }

        public bool IsActive { get; set; }
    }
}
