using SQLite;

namespace Mobile.Models;

/// <summary>
/// Bảng lưu lịch sử quét QR trong SQLite.
/// Chỉ giữ logic phiên 24 giờ.
/// </summary>
public class ScanLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string QrRawResult { get; set; } = string.Empty;        // Nội dung QR gốc

    public DateTime LastQrScanAt { get; set; }                     // Thời điểm quét lần cuối

    public Guid? LastScannedStallId { get; set; }

    public string? LastScannedSlug { get; set; }

    /// <summary>
    /// Thời điểm hết hạn phiên quét (24 giờ kể từ LastQrScanAt)
    /// </summary>
    public DateTime QrSessionExpiry { get; set; }

    public bool HasScannedQr { get; set; } = true;
}