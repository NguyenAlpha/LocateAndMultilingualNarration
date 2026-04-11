using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using Mobile.LocalDb;
using Mobile.Models;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;

namespace Mobile.Services;

/// <summary>
/// Cung cấp dữ liệu gian hàng cho UI theo chiến lược cache-first: ưu tiên đọc local trước, sau đó mới gọi API khi cần.
/// </summary>
public interface IStallService
{
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default);

    Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation của IStallService — Cache-First pattern.
/// </summary>
public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ILocalStallRepository _localRepo;
    // OLD CODE (kept for reference): private readonly ISyncService _syncService;
    private readonly IServiceProvider _serviceProvider;
    private ISyncService? _resolvedSyncService;
    private readonly ILogger<StallService> _logger;
    private const string ApiClientName = "ApiHttp";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    // OLD CODE (kept for reference): private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/geo/stalls";

    public StallService(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        ILocalStallRepository localRepo,
        // OLD CODE (kept for reference): ISyncService syncService,
        IServiceProvider serviceProvider,
        ILogger<StallService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _localRepo = localRepo;
        // OLD CODE (kept for reference): _syncService = syncService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách gian hàng theo chiến lược cache-first.
    /// </summary>
    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh)
        {
            var cached = await _localRepo.GetAllAsync();
            if (cached.Count > 0)
            {
                _logger.LogDebug("[StallService][GetStallsAsync]: {Count} stall từ SQLite", cached.Count);

                if (ShouldSync())
                {
                    // OLD CODE (kept for reference): _ = _syncService.SyncAsync(CancellationToken.None);
                    _ = TriggerBackgroundSyncSafelyAsync();
                }

                return cached.Select(ToDto).ToList();
            }
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            var offline = await _localRepo.GetAllAsync();
            if (offline.Count > 0)
            {
                _logger.LogWarning("[StallService][GetStallsAsync]: offline, fallback {Count} stall từ SQLite", offline.Count);
                return [..offline.Select(ToDto)];
            }
            _logger.LogWarning("[StallService][GetStallsAsync]: offline và SQLite trống");
            return [];
        }

        try
        {
            // Lấy deviceId để API trả về dữ liệu đúng theo thiết bị/ngữ cảnh người dùng.
            var deviceId = _deviceService.GetOrCreateDeviceId();
            // Tạo URL gọi API danh sách gian hàng.
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var url = $"{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";

            _logger.LogInformation("[StallService][GetStallsAsync]: gọi API {Url}", url);

            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[StallService][GetStallsAsync]: API trả về {StatusCode}, fallback SQLite", (int)response.StatusCode);
                var fallback = await _localRepo.GetAllAsync();
                return [..fallback.Select(ToDto)];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var dtos = (await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken))?.Data ?? [];

            _logger.LogInformation("[StallService][GetStallsAsync]: tải thành công {Count} gian hàng từ API", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService][GetStallsAsync]: exception");
            return [];
        }
    }

    public async Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.StallId.ToString().Equals(stallId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceId = _deviceService.GetOrCreateDeviceId();
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var url = $"/api/stalls?deviceId={Uri.EscapeDataString(deviceId)}";

            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: gọi API {Url}", url);
            
            using var response = await client.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[StallService][GetFeaturedStallsAsync]: API trả về {StatusCode}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<ApiResult<List<StallItem>>>(stream, JsonOptions, cancellationToken);
            var stalls = result?.Data ?? [];

            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: tải thành công {Count} gian hàng", stalls.Count);
            return stalls.Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService][GetFeaturedStallsAsync]: exception");
            return [];
        }
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180);
    

    private bool ShouldSync()
    {
        // OLD CODE (kept for reference): return _syncService.LastSyncedAt is null
        // OLD CODE (kept for reference):     || DateTime.UtcNow - _syncService.LastSyncedAt.Value > TimeSpan.FromMinutes(3);

        // Resolve muộn để phá vòng phụ thuộc DI: SyncService -> StallService -> ISyncService.
        _resolvedSyncService ??= _serviceProvider.GetService<ISyncService>();

        if (_resolvedSyncService is null)
            return false;

        return _resolvedSyncService.LastSyncedAt is null
            || DateTime.UtcNow - _resolvedSyncService.LastSyncedAt.Value > TimeSpan.FromMinutes(3);
    }

    // Fire-and-forget có bắt lỗi để không crash process nếu sync ném exception ngoài dự kiến.
    private async Task TriggerBackgroundSyncSafelyAsync()
    {
        try
        {
            _resolvedSyncService ??= _serviceProvider.GetService<ISyncService>();
            if (_resolvedSyncService is null)
                return;

            await _resolvedSyncService.SyncAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TriggerBackgroundSyncSafelyAsync: lỗi khi sync ngầm");
        }
    }

    /// <summary>
    /// Chuyển <see cref="LocalStall"/> sang <see cref="GeoStallDto"/>, ưu tiên audio cục bộ nếu đã tải sẵn.
    /// </summary>
    /// <param name="s">Dữ liệu local của gian hàng.</param>
    /// <returns>DTO gian hàng để UI sử dụng.</returns>
    private static GeoStallDto ToDto(LocalStall s) => new()
    {
        StallId = Guid.Parse(s.StallId),
        StallName = s.StallName,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        RadiusMeters = s.RadiusMeters,
        NarrationContent = s.NarrationContentId is null ? null : new GeoStallNarrationContentDto
        {
            Id = Guid.Parse(s.NarrationContentId),
            Title = s.NarrationTitle ?? string.Empty,
            Description = s.NarrationDescription,
            ScriptText  = s.NarrationScriptText ?? string.Empty,
            // Ưu tiên file local đã tải, fallback về URL remote
            AudioUrl    = s.LocalAudioPath ?? s.AudioUrl
        }
    };
}