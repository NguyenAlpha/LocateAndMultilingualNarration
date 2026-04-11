namespace Mobile.Models;

/// <summary>
/// DTO dùng để truyền thông tin phiên QR cho ViewModel và UI.
/// </summary>
public class ScanSessionDto
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime? LastQrScanAt { get; set; }
    public Guid? LastScannedStallId { get; set; }
    public string? LastScannedSlug { get; set; }
    public DateTime? QrSessionExpiry { get; set; }     // Hết hạn sau 24h
    public bool HasScannedQr { get; set; }
    public bool IsSessionValid { get; set; }           // true nếu còn trong 24h
    public string? LastQrRawResult { get; set; }
}