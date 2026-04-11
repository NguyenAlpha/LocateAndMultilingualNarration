using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Domain.Entities;

public class QRCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Giá trị thực tế của QR Code (chuỗi ngẫu nhiên, thường 8-32 ký tự)
    /// Đây là thứ mà mobile app sẽ quét được và gửi lên server
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Loại QR (ví dụ: "AppEntry", "DownloadLink", "Onboarding", ...)
    /// </summary>
    [MaxLength(50)]
    public string Type { get; set; } = "AppEntry";

    /// <summary>
    /// Thời điểm tạo QR
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời hạn sử dụng (nếu NULL thì vĩnh viễn)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// QR này đã được quét và sử dụng chưa (dùng cho one-time QR)
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Thời điểm quét lần cuối
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Số lần đã quét (nếu cho phép tái sử dụng)
    /// </summary>
    public int ScanCount { get; set; } = 0;

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ghi chú (tùy chọn)
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// URL ảnh QR (nếu bạn muốn lưu hình QR lên Azure Blob để in ấn hoặc hiển thị)
    /// </summary>
    [MaxLength(500)]
    public string? QrImageUrl { get; set; }
}