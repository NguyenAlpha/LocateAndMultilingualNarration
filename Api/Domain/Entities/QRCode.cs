using System.ComponentModel.DataAnnotations;

namespace LocateAndMultilingualNarration.Domain.Entities;

/// <summary>
/// Bảng quản lý mã QR độc lập.
/// Mỗi mã QR được Admin tạo ra và chỉ cho phép sử dụng một lần.
/// </summary>
public class QrCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Mã QR thực tế mà người dùng sẽ quét (có thể là chuỗi ngẫu nhiên hoặc Guid.ToString())
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm tạo mã QR
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Số ngày hiệu lực kể từ thời điểm thiết bị quét QR.
    /// Thời hạn truy cập = UsedAt + ValidDays.
    /// </summary>
    public int ValidDays { get; set; }

    /// <summary>
    /// Đã được sử dụng chưa
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Thời điểm mã QR được quét và sử dụng
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// DeviceId của người dùng đã quét mã này (dùng để traceability)
    /// </summary>
    [MaxLength(100)]
    public string? UsedByDeviceId { get; set; }

    /// <summary>
    /// Ghi chú hoặc mô tả (nếu Admin muốn thêm thông tin)
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }
}