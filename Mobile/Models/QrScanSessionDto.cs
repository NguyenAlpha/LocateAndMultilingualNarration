namespace Mobile.Models;

/// <summary>
/// DTO trả về trạng thái phiên quét QR hiện tại cho UI/ViewModel.
/// </summary>
public class QrScanSessionDto
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime? LastQrScanAt { get; set; }
    public Guid? LastScannedStallId { get; set; }
    public string? LastScannedSlug { get; set; }
    public DateTime? QrSessionExpiry { get; set; }
    public bool HasScannedQr { get; set; }
    public bool IsSessionValid { get; set; }
    public string? LastQrRawResult { get; set; }
}
