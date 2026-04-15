using Shared.DTOs.QrCodes;
using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class AdminQrCodesViewModel
{
    public IReadOnlyList<QrCodeDetailDto> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public int TotalUsed { get; set; }

    public QrCodeCreateDto Create { get; set; } = new();
    public bool ShowCreateModal { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
