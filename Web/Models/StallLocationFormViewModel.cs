using Shared.DTOs.StallLocations;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallLocationFormViewModel
    {
        public StallLocationCreateDto Create { get; set; } = new StallLocationCreateDto();
        public StallLocationUpdateDto Edit { get; set; } = new StallLocationUpdateDto();
        public IEnumerable<StallDetailDto> Stalls { get; set; } = Array.Empty<StallDetailDto>();
    }
}
