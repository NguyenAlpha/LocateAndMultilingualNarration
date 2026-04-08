using Shared.DTOs.Common;
using Shared.DTOs.StallLocations;

namespace Web.Models
{
    public class StallLocationManagementViewModel : PagedResult<StallLocationDetailDto>
    {
        public string? StallName { get; set; }
        public bool? IsActive { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
