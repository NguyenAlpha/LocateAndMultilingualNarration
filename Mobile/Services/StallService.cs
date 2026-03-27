using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using Mobile.LocalDb;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;

namespace Mobile.Services;

public interface IStallService
{
    /// <summary>Lấy toàn bộ danh sách gian hàng, có hỗ trợ cache.</summary>
    /// <param name="forceRefresh">true = bỏ qua cache, gọi API mới (mặc định false)</param>
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>Lấy một gian hàng cụ thể theo ID.</summary>
    Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation của IStallService — Cache-First pattern:
///   1. Đọc từ SQLite (LocalStallRepository) → trả về ngay, render bản đồ tức thì
///   2. Trigger sync ngầm nếu data quá cũ (> 3 phút)
///   3. Nếu SQLite trống và có mạng → gọi API trực tiếp
///   4. Offline + SQLite trống → fallback mock
/// </summary>
public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ILocalStallRepository _localRepo;
    private readonly ISyncService _syncService;
    private readonly ILogger<StallService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/geo/stalls";

    public StallService(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        ILocalStallRepository localRepo,
        ISyncService syncService,
        ILogger<StallService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _localRepo = localRepo;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Cache-First: đọc SQLite trước (nhanh, không cần mạng)
        if (!forceRefresh)
        {
            var cached = await _localRepo.GetAllAsync();
            if (cached.Count > 0)
            {
                _logger.LogDebug("GetStallsAsync: {Count} stall từ SQLite", cached.Count);

                // Trigger sync ngầm nếu data quá cũ — không await, không block UI
                if (ShouldSync())
                    _ = _syncService.SyncAsync(CancellationToken.None);

                return cached.Select(ToDto).ToList();
            }
        }

        // SQLite trống hoặc forceRefresh: kiểm tra mạng
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            _logger.LogWarning("GetStallsAsync: offline và SQLite trống, dùng mock");
            return GetMockStalls();
        }

        try
        {
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var client = _httpClientFactory.CreateClient();
            var url = $"{BaseUrl}{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";

            _logger.LogInformation("GetStallsAsync: gọi API {Url}", url);
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GetStallsAsync: API trả về {StatusCode}, dùng mock", (int)response.StatusCode);
                return GetMockStalls();
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken);
            var dtos = result?.Data ?? [];

            _logger.LogInformation("GetStallsAsync: tải thành công {Count} gian hàng từ API", dtos.Count);
            return dtos.Count == 0 ? GetMockStalls() : dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStallsAsync: exception");
            return GetMockStalls();
        }
    }

    public async Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.StallId.ToString().Equals(stallId, StringComparison.OrdinalIgnoreCase));
    }

    // Sync nếu chưa sync lần nào hoặc đã quá 3 phút
    private bool ShouldSync()
        => _syncService.LastSyncedAt is null
        || DateTime.UtcNow - _syncService.LastSyncedAt.Value > TimeSpan.FromMinutes(3);

    // Ưu tiên LocalAudioPath (phát offline), fallback AudioUrl (stream từ mạng)
    private static GeoStallDto ToDto(LocalStall s) => new()
    {
        StallId      = Guid.Parse(s.StallId),
        StallName    = s.StallName,
        Latitude     = s.Latitude,
        Longitude    = s.Longitude,
        RadiusMeters = s.RadiusMeters,
        AudioUrl     = s.LocalAudioPath ?? s.AudioUrl
    };

    private static List<GeoStallDto> GetMockStalls() =>
    [
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000001"), StallName = "Bánh Mì Ông 3",   Latitude = 10.777534, Longitude = 106.710669, RadiusMeters = 50, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3" },
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000002"), StallName = "Phở Bò Bình Dân", Latitude = 10.7782,   Longitude = 106.7115,   RadiusMeters = 40, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3" },
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000003"), StallName = "Kem Trái Cây",     Latitude = 10.7769,   Longitude = 106.7098,   RadiusMeters = 30, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3" }
    ];
}
