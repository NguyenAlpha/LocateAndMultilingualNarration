using Shared.DTOs.Tours;

namespace Web.Models;

public class TourManagementViewModel
{
    public IReadOnlyList<TourListItemDto> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? Search { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
