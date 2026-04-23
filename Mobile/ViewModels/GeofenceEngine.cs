using Microsoft.Extensions.Logging;
using Mobile.Services;
using Shared.DTOs.Geo;

namespace Mobile.ViewModels;

/// <summary>
/// Quản lý state geofence tách khỏi MapViewModel:
///   - Giữ danh sách stall, tập triggered và hàng chờ audio.
///   - Tính khoảng cách Haversine, phát hiện stall vào/ra vùng.
///   - Yêu cầu phát audio qua event AutoPlayRequested — không tự chạm UI-selection.
/// Không phải service DI — được khởi tạo bên trong MapViewModel vì state gắn với 1 session trang.
/// Mutation luôn chạy trên MainThread (caller đảm bảo).
/// </summary>
public class GeofenceEngine
{
    private readonly IAudioGuideService _audioGuideService;
    private readonly ILogger _logger;

    private IReadOnlyList<GeoStallDto> _stalls = [];
    private readonly HashSet<Guid> _triggeredIds = [];
    private Queue<GeoStallDto> _queue = new();

    // Tour mode: khi != null thì chỉ cho phép trigger stall có StallId trong set.
    private HashSet<Guid>? _tourStallIds;

    /// <summary>
    /// Fire khi engine quyết định phát một stall (vào vùng mới hoặc dequeue sau khi audio trước kết thúc).
    /// Subscriber phải trả Task để engine có thể await — tránh fire-and-forget gây race với IsPlaying.
    /// </summary>
    public event Func<GeoStallDto, Task>? AutoPlayRequested;

    public GeofenceEngine(IAudioGuideService audioGuideService, ILogger logger)
    {
        _audioGuideService = audioGuideService;
        _logger = logger;
    }

    /// <summary>
    /// Cập nhật danh sách stall hiện có (sau khi load/reload). Không reset triggered — caller gọi Reset nếu cần.
    /// </summary>
    public void SetStalls(IReadOnlyList<GeoStallDto> stalls) => _stalls = stalls;

    /// <summary>
    /// Bật/tắt chế độ tour. Truyền tập StallId của các stop trong tour để giới hạn auto-play
    /// chỉ trigger những stall đó. Truyền null để tắt tour mode.
    /// </summary>
    public void SetActiveTour(IEnumerable<Guid>? tourStallIds)
    {
        _tourStallIds = tourStallIds is null ? null : new HashSet<Guid>(tourStallIds);
    }

    /// <summary>
    /// Reset toàn bộ state để lần tick tiếp theo re-enqueue mọi stall trong vùng.
    /// Gọi sau khi audio được download mới hoặc user refresh.
    /// </summary>
    public void Reset()
    {
        _triggeredIds.Clear();
        _queue = new Queue<GeoStallDto>();
    }

    /// <summary>
    /// Xử lý 1 tick GPS: enqueue stall mới vào vùng, loại stall thoát vùng, dequeue và phát nếu rảnh.
    /// </summary>
    public async Task OnLocationAsync(double lat, double lng)
    {
        var inRange = _stalls
            .Select(s => (stall: s, dist: CalculateDistance(lat, lng, s.Latitude, s.Longitude)))
            .Where(x => x.dist <= x.stall.RadiusMeters)
            .OrderBy(x => x.dist)
            .Select(x => x.stall)
            .ToList();

        var inRangeIds = inRange.Select(s => s.StallId).ToHashSet();

        var exited = _triggeredIds.Where(id => !inRangeIds.Contains(id)).ToList();
        if (exited.Count > 0)
        {
            foreach (var id in exited)
                _triggeredIds.Remove(id);

            _queue = new Queue<GeoStallDto>(_queue.Where(s => inRangeIds.Contains(s.StallId)));

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[Geofence] {Count} stall thoát vùng", exited.Count);
        }

        foreach (var stall in inRange)
        {
            if (_triggeredIds.Contains(stall.StallId)) continue;
            if (string.IsNullOrWhiteSpace(stall.NarrationContent?.AudioUrl)) continue;

            // Tour mode: bỏ qua stall không nằm trong tour.
            if (_tourStallIds is not null && !_tourStallIds.Contains(stall.StallId)) continue;

            _triggeredIds.Add(stall.StallId);
            _queue.Enqueue(stall);
            _logger.LogInformation("[Queue] Thêm vào hàng chờ: {StallName} (queue size: {Size})",
                stall.StallName, _queue.Count);
        }

        if (!_audioGuideService.IsPlaying && _queue.TryDequeue(out var next))
        {
            _logger.LogInformation("[Queue] Bắt đầu phát: {StallName}", next.StallName);
            await InvokeAutoPlayAsync(next);
        }
    }

    /// <summary>
    /// Gọi khi audio kết thúc tự nhiên — phát stall tiếp theo trong hàng chờ nếu có.
    /// </summary>
    public async Task OnPlaybackCompletedAsync()
    {
        if (_queue.TryDequeue(out var next))
        {
            _logger.LogInformation("[Queue] Phát tiếp: {StallName}", next.StallName);
            await InvokeAutoPlayAsync(next);
        }
    }

    private Task InvokeAutoPlayAsync(GeoStallDto stall)
        => AutoPlayRequested?.Invoke(stall) ?? Task.CompletedTask;

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
