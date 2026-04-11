using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Mobile.Models;
using SQLite;

namespace Mobile.Services;

/// <summary>
/// Contract quản lý phiên quét QR cho luồng anonymous trên Mobile.
/// </summary>
public interface IQrScanService
{
    Task SaveQrScanAsync(string qrResult);
    Task<bool> IsQrSessionValidAsync();
    Task<DateTime?> GetQrSessionExpiryAsync();
    Task<bool> HasScannedQrAsync();
    Task MarkAsScannedAsync();
    Task<QrScanSessionDto> GetCurrentScanSessionAsync();
}

/// <summary>
/// Service lưu log quét QR vào SQLite và quản lý trạng thái phiên quét theo thời gian.
/// </summary>
public class QrScanService : IQrScanService
{
    // OLD CODE (kept for reference): TODO cũ mô tả session 7 ngày.
    // Yêu cầu hiện tại của đồ án: phiên quét có hiệu lực trong 24 giờ kể từ lần quét gần nhất.
    private static readonly TimeSpan QrSessionDuration = TimeSpan.FromHours(24);

    private const string QrDbName = "stalls.db3";
    private const string DeviceIdKey = "device_id";
    private const string HasScannedQrKey = "has_scanned_qr";

    private readonly IDeviceService _deviceService;
    private readonly ILogger<QrScanService> _logger;

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _dbInitLock = new(1, 1);

    public QrScanService(
        IDeviceService deviceService,
        ILogger<QrScanService> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Lưu một lần quét QR: parse StallId/Slug, bảo đảm có DeviceId, ghi log vào SQLite.
    /// </summary>
    public async Task SaveQrScanAsync(string qrResult)
    {
        if (string.IsNullOrWhiteSpace(qrResult))
            throw new ArgumentException("QR result không được rỗng", nameof(qrResult));

        var deviceId = await EnsureDeviceIdAsync();
        var (stallId, slug) = ParseQrPayload(qrResult);

        var nowUtc = DateTime.UtcNow;
        var log = new QrScanLog
        {
            DeviceId = deviceId,
            QrRawResult = qrResult.Trim(),
            LastQrScanAt = nowUtc,
            LastScannedStallId = stallId,
            LastScannedSlug = slug,
            QrSessionExpiry = nowUtc.Add(QrSessionDuration),
            HasScannedQr = true
        };

        var db = await GetDbAsync();
        await db.InsertAsync(log);

        // Đánh dấu cờ đã quét để các màn hình có thể kiểm tra nhanh không cần query DB mỗi lần.
        await SecureStorage.Default.SetAsync(HasScannedQrKey, bool.TrueString);

        _logger.LogInformation(
            "[QrScanService] Saved QR scan. DeviceId={DeviceId}, StallId={StallId}, Slug={Slug}, Expiry={Expiry:O}",
            deviceId,
            stallId,
            slug,
            log.QrSessionExpiry);
    }

    /// <summary>
    /// Kiểm tra phiên QR hiện tại có còn hợp lệ hay không (24h kể từ lần quét gần nhất).
    /// </summary>
    public async Task<bool> IsQrSessionValidAsync()
    {
        var session = await GetCurrentScanSessionAsync();

        if (!session.HasScannedQr || session.QrSessionExpiry is null)
            return false;

        var isValid = DateTime.UtcNow <= session.QrSessionExpiry.Value;
        return isValid;
    }

    /// <summary>
    /// Trả về thời điểm hết hạn phiên quét hiện tại.
    /// </summary>
    public async Task<DateTime?> GetQrSessionExpiryAsync()
    {
        var latest = await GetLatestLogAsync();
        return latest?.QrSessionExpiry;
    }

    /// <summary>
    /// Kiểm tra thiết bị đã từng quét QR hay chưa.
    /// </summary>
    public async Task<bool> HasScannedQrAsync()
    {
        // Ưu tiên đọc cờ nhanh từ SecureStorage.
        var flag = await SecureStorage.Default.GetAsync(HasScannedQrKey);
        if (bool.TryParse(flag, out var parsed) && parsed)
            return true;

        // Fallback về SQLite nếu chưa có cờ hoặc cờ bị xóa.
        var latest = await GetLatestLogAsync();
        return latest?.HasScannedQr == true;
    }

    /// <summary>
    /// Đánh dấu thiết bị đã quét QR (không cần ghi QR payload).
    /// </summary>
    public async Task MarkAsScannedAsync()
    {
        await SecureStorage.Default.SetAsync(HasScannedQrKey, bool.TrueString);

        var latest = await GetLatestLogAsync();
        if (latest is null)
        {
            // Tạo bản ghi tối thiểu để bảo toàn trạng thái nếu chưa từng có log nào.
            var nowUtc = DateTime.UtcNow;
            var deviceId = await EnsureDeviceIdAsync();

            var placeholder = new QrScanLog
            {
                DeviceId = deviceId,
                QrRawResult = "manual-mark",
                LastQrScanAt = nowUtc,
                QrSessionExpiry = nowUtc.Add(QrSessionDuration),
                HasScannedQr = true
            };

            var db = await GetDbAsync();
            await db.InsertAsync(placeholder);
            return;
        }

        latest.HasScannedQr = true;
        var conn = await GetDbAsync();
        await conn.UpdateAsync(latest);
    }

    /// <summary>
    /// Lấy snapshot phiên quét gần nhất để phục vụ UI/ViewModel.
    /// </summary>
    public async Task<QrScanSessionDto> GetCurrentScanSessionAsync()
    {
        var latest = await GetLatestLogAsync();
        if (latest is null)
        {
            var hasScanned = await HasScannedQrAsync();
            var deviceId = await EnsureDeviceIdAsync();

            return new QrScanSessionDto
            {
                DeviceId = deviceId,
                HasScannedQr = hasScanned,
                IsSessionValid = false
            };
        }

        var valid = DateTime.UtcNow <= latest.QrSessionExpiry;

        return new QrScanSessionDto
        {
            DeviceId = latest.DeviceId,
            LastQrScanAt = latest.LastQrScanAt,
            LastScannedStallId = latest.LastScannedStallId,
            LastScannedSlug = latest.LastScannedSlug,
            QrSessionExpiry = latest.QrSessionExpiry,
            HasScannedQr = latest.HasScannedQr,
            IsSessionValid = valid,
            LastQrRawResult = latest.QrRawResult
        };
    }

    /// <summary>
    /// Bảo đảm luôn có DeviceId và lưu vào SecureStorage ngay khi quét QR.
    /// </summary>
    private async Task<string> EnsureDeviceIdAsync()
    {
        var existingFromSecure = await SecureStorage.Default.GetAsync(DeviceIdKey);
        if (!string.IsNullOrWhiteSpace(existingFromSecure))
            return existingFromSecure;

        // Tái sử dụng DeviceService hiện có để thống nhất định danh trong toàn app.
        var deviceId = _deviceService.GetOrCreateDeviceId();

       
        await SecureStorage.Default.SetAsync(DeviceIdKey, deviceId);
        return deviceId;
    }

    /// <summary>
    /// Truy vấn bản ghi quét gần nhất theo thời gian.
    /// </summary>
    private async Task<QrScanLog?> GetLatestLogAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<QrScanLog>()
            .OrderByDescending(x => x.LastQrScanAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Khởi tạo SQLite connection và tạo bảng QrScanLogs nếu chưa tồn tại.
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null)
            return _db;

        await _dbInitLock.WaitAsync();
        try
        {
            if (_db is not null)
                return _db;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, QrDbName);
            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            await _db.CreateTableAsync<QrScanLog>();
            return _db;
        }
        finally
        {
            _dbInitLock.Release();
        }
    }

    /// <summary>
    /// Parse dữ liệu QR để lấy StallId (GUID) hoặc Slug.
    /// </summary>
    private static (Guid? StallId, string? Slug) ParseQrPayload(string raw)
    {
        var value = raw.Trim();

        // Trường hợp QR chứa trực tiếp GUID.
        if (Guid.TryParse(value, out var guid))
            return (guid, null);

        // Trường hợp dạng stall:{idOrSlug}.
        if (value.StartsWith("stall:", StringComparison.OrdinalIgnoreCase))
        {
            var token = value.Split(':', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim();
            if (Guid.TryParse(token, out var tokenGuid))
                return (tokenGuid, null);

            return (null, string.IsNullOrWhiteSpace(token) ? null : token);
        }

        // Trường hợp URL/query: ...?stallId=... hoặc ...?slug=...
        var stallIdMatch = Regex.Match(value, @"(?:stallId|boothId)=(?<id>[^&\s]+)", RegexOptions.IgnoreCase);
        if (stallIdMatch.Success)
        {
            var idValue = Uri.UnescapeDataString(stallIdMatch.Groups["id"].Value);
            if (Guid.TryParse(idValue, out var idGuid))
                return (idGuid, null);

            return (null, idValue);
        }

        var slugMatch = Regex.Match(value, @"(?:slug|stallSlug)=(?<slug>[^&\s]+)", RegexOptions.IgnoreCase);
        if (slugMatch.Success)
        {
            var slug = Uri.UnescapeDataString(slugMatch.Groups["slug"].Value);
            return (null, slug);
        }

        // Trường hợp không parse được: giữ raw dưới dạng slug dự phòng để truy vết.
        return (null, value);
    }
}
