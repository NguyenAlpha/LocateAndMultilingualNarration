using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Mobile.Models;
using SQLite;

namespace Mobile.Services;

// OLD CODE (kept for reference): public interface ScanService
public interface IScanService
{
    Task SaveQrScanAsync(string qrResult);
    Task<bool> IsQrSessionValidAsync();
    Task<DateTime?> GetQrSessionExpiryAsync();
    Task<bool> HasScannedQrAsync();
    Task MarkAsScannedAsync();
    Task<ScanSessionDto> GetCurrentScanSessionAsync();
}

// OLD CODE (kept for reference): public class ScanService : ScanService
public class ScanService : IScanService
{
    private static readonly TimeSpan QrSessionDuration = TimeSpan.FromHours(24);   // TODO 11: 24h
    //private static readonly TimeSpan QrValidityDuration = TimeSpan.FromDays(7);   // TODO 11: 7 ngày

    private const string DbName = "stalls.db3";
    private const string DeviceIdKey = "device_id";
    private const string HasScannedQrKey = "has_scanned_qr";

    private readonly IDeviceService _deviceService;
    private readonly ILogger<ScanService> _logger;

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    public ScanService(IDeviceService deviceService, ILogger<ScanService> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    public async Task SaveQrScanAsync(string qrResult)
    {
        if (string.IsNullOrWhiteSpace(qrResult))
            throw new ArgumentException("QR result cannot be empty", nameof(qrResult));

        var deviceId = await EnsureDeviceIdAsync();
        var (stallId, slug) = ParseQrPayload(qrResult);

        var now = DateTime.UtcNow;

        var log = new ScanLog
        {
            DeviceId = deviceId,
            QrRawResult = qrResult.Trim(),
            LastQrScanAt = now,
            LastScannedStallId = stallId,
            LastScannedSlug = slug,
            QrSessionExpiry = now.Add(QrSessionDuration),     // 24h
            //QrValidityExpiry = now.Add(QrValidityDuration),   // 7 ngày
            HasScannedQr = true
        };

        var db = await GetDbAsync();
        await db.InsertAsync(log);

        await SecureStorage.Default.SetAsync(HasScannedQrKey, "true");

        _logger.LogInformation("[ScanService] QR scan saved. Device={DeviceId}, Expiry={Expiry}",
            deviceId, stallId, log.QrSessionExpiry);
    }

    public async Task<bool> IsQrSessionValidAsync()
    {
        var session = await GetCurrentScanSessionAsync();
        return session.IsSessionValid;
    }

    public async Task<DateTime?> GetQrSessionExpiryAsync()
    {
        var log = await GetLatestLogAsync();
        return log?.QrSessionExpiry;
    }

    public async Task<bool> HasScannedQrAsync()
    {
        var flag = await SecureStorage.Default.GetAsync(HasScannedQrKey);
        if (bool.TryParse(flag, out bool result) && result)
            return true;

        var log = await GetLatestLogAsync();
        return log?.HasScannedQr == true;
    }

    public async Task MarkAsScannedAsync()
    {
        await SecureStorage.Default.SetAsync(HasScannedQrKey, "true");

        var db = await GetDbAsync();
        var latest = await GetLatestLogAsync();

        if (latest == null)
        {
            var deviceId = await EnsureDeviceIdAsync();
            var now = DateTime.UtcNow;

            var placeholder = new ScanLog
            {
                DeviceId = deviceId,
                QrRawResult = "manual_mark",
                LastQrScanAt = now,
                QrSessionExpiry = now.Add(QrSessionDuration),
                //QrValidityExpiry = now.Add(QrValidityDuration),
                HasScannedQr = true
            };
            await db.InsertAsync(placeholder);
        }
        else
        {
            latest.HasScannedQr = true;
            await db.UpdateAsync(latest);
        }
    }

    public async Task<ScanSessionDto> GetCurrentScanSessionAsync()
    {
        var log = await GetLatestLogAsync();
        var deviceId = await EnsureDeviceIdAsync();

        if (log == null)
        {
            return new ScanSessionDto
            {
                DeviceId = deviceId,
                HasScannedQr = false,
                IsSessionValid = false
            };
        }

        bool isValid = DateTime.UtcNow <= log.QrSessionExpiry;

        return new ScanSessionDto
        {
            DeviceId = log.DeviceId,
            LastQrScanAt = log.LastQrScanAt,
            //LastScannedStallId = log.LastScannedStallId,
            LastScannedSlug = log.LastScannedSlug,
            QrSessionExpiry = log.QrSessionExpiry,
            HasScannedQr = log.HasScannedQr,
            IsSessionValid = isValid,
            LastQrRawResult = log.QrRawResult
        };
    }

    // ==================== Private Helpers ====================

    private async Task<string> EnsureDeviceIdAsync()
    {
        var saved = await SecureStorage.Default.GetAsync(DeviceIdKey);
        if (!string.IsNullOrWhiteSpace(saved))
            return saved;

        var deviceId = _deviceService.GetOrCreateDeviceId();
        await SecureStorage.Default.SetAsync(DeviceIdKey, deviceId);
        return deviceId;
    }

    private async Task<ScanLog?> GetLatestLogAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<ScanLog>()
                       .OrderByDescending(x => x.LastQrScanAt)
                       .FirstOrDefaultAsync();
    }

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db != null) return _db;

        await _dbLock.WaitAsync();
        try
        {
            if (_db != null) return _db;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
            _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _db.CreateTableAsync<ScanLog>();
            return _db;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private static (Guid? StallId, string? Slug) ParseQrPayload(string raw)
    {
        var value = raw.Trim();

        if (Guid.TryParse(value, out var guid))
            return (guid, null);

        if (value.StartsWith("stall:", StringComparison.OrdinalIgnoreCase))
        {
            var token = value.Split(':', 2).LastOrDefault()?.Trim();
            return Guid.TryParse(token, out var g) ? (g, null) : (null, token);
        }

        var match = Regex.Match(value, @"(?:stallId|boothId)=(?<id>[^&\s]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var idStr = Uri.UnescapeDataString(match.Groups["id"].Value);
            return Guid.TryParse(idStr, out var g) ? (g, null) : (null, idStr);
        }

        return (null, value);
    }
}