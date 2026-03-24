using Shared.DTOs.Languages;
using Shared.DTOs.Narrations;
using Shared.DTOs.Stalls;

namespace Web.Models
{
    public class StallNarrationContentManagementViewModel
    {
        public IReadOnlyList<StallNarrationContentDetailDto> Items { get; set; } = Array.Empty<StallNarrationContentDetailDto>();
        public IReadOnlyList<LanguageDetailDto> Languages { get; set; } = Array.Empty<LanguageDetailDto>();
        public IReadOnlyList<StallDetailDto> Stalls { get; set; } = Array.Empty<StallDetailDto>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string? Search { get; set; }
        public Guid? StallId { get; set; }
        public Guid? LanguageId { get; set; }
        public bool? IsActive { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
