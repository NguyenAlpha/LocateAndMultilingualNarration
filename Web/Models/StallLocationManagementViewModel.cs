using Shared.DTOs.Common;
using Shared.DTOs.StallLocations;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallLocationManagementViewModel : PagedResult<StallLocationDetailDto>
    {
        public Guid? StallId { get; set; }
        public bool? IsActive { get; set; }
        public IEnumerable<StallDetailDto> Stalls { get; set; } = Array.Empty<StallDetailDto>();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
