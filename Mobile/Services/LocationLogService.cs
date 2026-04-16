using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Shared.DTOs.DeviceLocationLogs;

namespace Mobile.Services;

/// <summary>
/// Thu thập tọa độ GPS định kỳ vào buffer, gom gửi lên API theo batch.
/// </summary>
public interface ILocationLogService
{
    /// <summary>
    /// Thử lấy mẫu vị trí hiện tại. Bỏ qua nếu chưa đủ khoảng thời gian lấy mẫu hoặc buffer đầy.
    /// </summary>
    void TrySample(double lat, double lon, double? accuracy);

    /// <summary>
    /// Gửi toàn bộ điểm trong buffer lên API rồi xóa buffer (chỉ khi gửi thành công).
    /// </summary>
    Task FlushAsync();
}

/// <summary>
/// Lấy mẫu GPS mỗi 5 giây, buffer tối đa 500 điểm.
/// Flush được trigger từ SyncBackgroundService (mỗi 3 phút) và App.OnSleep.
/// </summary>
public class LocationLogService : ILocationLogService
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(5);
    private const int MaxBufferSize = 500;
    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string BatchEndpoint = $"{BaseUrl}/api/device-location-log/batch";

    private readonly List<LocationPointDto> _buffer = [];
    private readonly Lock _bufferLock = new();
    private readonly IDeviceService _deviceService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LocationLogService> _logger;

    private DateTimeOffset _lastSampleAt = DateTimeOffset.MinValue;

    public LocationLogService(
        IDeviceService deviceService,
        IHttpClientFactory httpClientFactory,
        ILogger<LocationLogService> logger)
    {
        _deviceService = deviceService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public void TrySample(double lat, double lon, double? accuracy)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastSampleAt < SampleInterval) return;

        lock (_bufferLock)
        {
            if (_buffer.Count >= MaxBufferSize) return;

            _buffer.Add(new LocationPointDto
            {
                Latitude = lat,
                Longitude = lon,
                AccuracyMeters = accuracy,
                CapturedAt = now
            });
            _logger.LogDebug("[LocationLog] Sample #{Count} — lat={Lat:F6}, lng={Lng:F6}", _buffer.Count, lat, lon);
        }

        _lastSampleAt = now;
    }

    public async Task FlushAsync()
    {
        List<LocationPointDto> snapshot;
        lock (_bufferLock)
        {
            if (_buffer.Count == 0) return;
            snapshot = [.. _buffer];
        }

        var deviceId = _deviceService.GetOrCreateDeviceId();
        var dto = new DeviceLocationLogBatchDto
        {
            DeviceId = deviceId,
            Points = snapshot
        };

        try
        {
            var client = _httpClientFactory.CreateClient(string.Empty);
            var response = await client.PostAsJsonAsync(BatchEndpoint, dto);

            if (response.IsSuccessStatusCode)
            {
                lock (_bufferLock)
                {
                    // Chỉ xóa đúng số điểm đã gửi — có thể có điểm mới được add trong lúc await
                    _buffer.RemoveRange(0, Math.Min(snapshot.Count, _buffer.Count));
                }
                _logger.LogInformation("[LocationLog] Flush thành công: {Count} điểm", snapshot.Count);
            }
            else
            {
                _logger.LogWarning("[LocationLog] Flush thất bại: HTTP {Status}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Analytics là best-effort — không throw, giữ buffer để thử lại lần sau
            _logger.LogWarning("[LocationLog] Flush lỗi: {Message}", ex.Message);
        }
    }
}
