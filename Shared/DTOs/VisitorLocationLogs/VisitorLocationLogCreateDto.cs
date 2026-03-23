using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.VisitorLocationLogs
{
    public class VisitorLocationLogCreateDto
    {
        [Required(ErrorMessage = "Latitude là bắt buộc")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Longitude là bắt buộc")]
        public decimal Longitude { get; set; }

        public decimal? AccuracyMeters { get; set; }
    }
}
