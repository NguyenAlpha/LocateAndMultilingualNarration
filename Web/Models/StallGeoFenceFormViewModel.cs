using Shared.DTOs.StallGeoFences;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallGeoFenceFormViewModel
    {
        public StallGeoFenceCreateDto Create { get; set; } = new StallGeoFenceCreateDto();
        public StallGeoFenceUpdateDto Edit { get; set; } = new StallGeoFenceUpdateDto();
        public IEnumerable<StallDetailDto> Stalls { get; set; } = Array.Empty<StallDetailDto>();
    }
}
