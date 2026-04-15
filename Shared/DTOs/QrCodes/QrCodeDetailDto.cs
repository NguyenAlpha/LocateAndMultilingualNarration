namespace Shared.DTOs.QrCodes;

public class QrCodeDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedByDeviceId { get; set; }
    public string? Note { get; set; }
}
