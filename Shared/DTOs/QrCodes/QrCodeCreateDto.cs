namespace Shared.DTOs.QrCodes;

public class QrCodeCreateDto
{
    public DateTime ExpiryAt { get; set; }
    public string? Note { get; set; }
}
