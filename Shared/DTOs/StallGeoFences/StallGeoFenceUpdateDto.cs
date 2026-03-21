using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.StallGeoFences
{
    public class StallGeoFenceUpdateDto
    {
        [Required(ErrorMessage = "PolygonJson là bắt buộc")]
        public string PolygonJson { get; set; } = string.Empty;

        public int? MinZoom { get; set; }

        public int? MaxZoom { get; set; }
    }
}
