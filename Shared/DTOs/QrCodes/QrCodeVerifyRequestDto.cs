namespace Shared.DTOs.QrCodes;

public class QrCodeVerifyRequestDto
{
    public string Code { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}
