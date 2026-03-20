using Shared.DTOs.Businesses;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallManagementViewModel
    {
        public IReadOnlyList<StallDetailDto> Items { get; set; } = Array.Empty<StallDetailDto>();
        public IReadOnlyList<BusinessDetailDto> Businesses { get; set; } = Array.Empty<BusinessDetailDto>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public Guid? BusinessId { get; set; }
        public string? Search { get; set; }
        public StallFormViewModel Create { get; set; } = new();
        public StallFormViewModel Edit { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool ShowCreateModal { get; set; }
        public bool ShowEditModal { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
