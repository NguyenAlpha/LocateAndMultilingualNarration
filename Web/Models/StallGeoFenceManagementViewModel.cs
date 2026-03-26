using Shared.DTOs.Common;
using Shared.DTOs.StallGeoFences;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallGeoFenceManagementViewModel : PagedResult<StallGeoFenceDetailDto>
    {
        public Guid? StallId { get; set; }
        public IEnumerable<StallDetailDto> Stalls { get; set; } = Array.Empty<StallDetailDto>();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
