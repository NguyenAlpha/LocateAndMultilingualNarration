using Shared.DTOs.StallMedia;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallMediaManagementViewModel
    {
        public IReadOnlyList<StallMediaDetailDto> Items { get; set; } = Array.Empty<StallMediaDetailDto>();
        public IReadOnlyList<StallDetailDto> Stalls { get; set; } = Array.Empty<StallDetailDto>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public Guid? FilterStallId { get; set; }
        public bool? IsActive { get; set; }
        public StallMediaFormViewModel Create { get; set; } = new();
        public StallMediaFormViewModel Edit { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public bool ShowCreateModal { get; set; }
        public bool ShowEditModal { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
