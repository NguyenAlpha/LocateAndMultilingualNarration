using SQLite;

namespace Mobile.Models;

/// <summary>
/// Log mỗi lần người dùng quét QR để phục vụ kiểm tra phiên sử dụng theo thời gian.
/// </summary>
[Table("QrScanLogs")]
public class QrScanLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// DeviceId tại thời điểm quét (anonymous-first, không yêu cầu đăng nhập).
    /// </summary>
    [Indexed]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị QR gốc nhận được từ camera hoặc nhập tay.
    /// </summary>
    public string QrRawResult { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm quét gần nhất (UTC).
    /// </summary>
    [Indexed]
    public DateTime LastQrScanAt { get; set; }

    /// <summary>
    /// StallId cuối cùng đọc được từ QR (nếu parse được GUID).
    /// </summary>
    public Guid? LastScannedStallId { get; set; }

    /// <summary>
    /// Slug cuối cùng đọc được từ QR (nếu QR dùng slug thay vì GUID).
    /// </summary>
    public string? LastScannedSlug { get; set; }

    /// <summary>
    /// Thời điểm hết hạn phiên quét (UTC).
    /// </summary>
    [Indexed]
    public DateTime QrSessionExpiry { get; set; }

    /// <summary>
    /// Cờ đánh dấu thiết bị đã từng quét QR.
    /// </summary>
    public bool HasScannedQr { get; set; }
}
