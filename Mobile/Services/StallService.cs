using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mobile.LocalDb;
using Mobile.Models;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;

namespace Mobile.Services;

public interface IStallService
{
    /// <summary>
    /// Lấy danh sách gian hàng theo chiến lược cache-first (SQLite/API).
    /// </summary>
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách gian hàng (dùng cho StallListPage)
    /// </summary>
    Task<List<StallItem>> GetAllStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách gian hàng nổi bật cho trang Home
    /// </summary>
    Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy một gian hàng theo ID
    /// </summary>
    Task<StallItem?> GetStallByIdAsync(Guid stallId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cung cấp dữ liệu gian hàng theo chiến lược cache-first.
/// Ưu tiên đọc từ SQLite, sau đó mới gọi API.
/// </summary>
public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ILocalStallRepository _localRepo;
    private readonly ILogger<StallService> _logger;

    private const string ApiClientName = "ApiHttp";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Cache trong memory cho GeoStallDto
    private List<GeoStallDto>? _cachedStalls;
    // OLD CODE (kept for reference): private List<StallItem>? _cachedStallItems;
    private DateTime _lastFetchUtc = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public StallService(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        ILocalStallRepository localRepo,
        ILogger<StallService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _localRepo = localRepo;
        _logger = logger;
    }

    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Ưu tiên cache trong memory
        if (!forceRefresh && _cachedStalls?.Count > 0 && DateTime.UtcNow - _lastFetchUtc < CacheDuration)
        {
            return _cachedStalls;
        }

        // Cache-first: thử đọc từ SQLite
        if (!forceRefresh)
        {
            var localStalls = await _localRepo.GetAllAsync();
            if (localStalls.Count > 0)
            {
                _cachedStalls = localStalls.Select(MapLocalToGeoSafe).ToList();
                _lastFetchUtc = DateTime.UtcNow;
                _logger.LogDebug("[StallService] Trả về {Count} stall từ SQLite", _cachedStalls.Count);
                return _cachedStalls;
            }
        }

        // Không có mạng → fallback SQLite
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            var offline = await _localRepo.GetAllAsync();
            _cachedStalls = offline.Select(MapLocalToGeoSafe).ToList();
            return _cachedStalls;
        }

        try
        {
            var deviceId = _deviceService.GetOrCreateDeviceId();
            var client = _httpClientFactory.CreateClient(ApiClientName);

            var response = await client.GetAsync($"/api/geo/stalls?deviceId={Uri.EscapeDataString(deviceId)}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[StallService] API trả về lỗi {Status}", response.StatusCode);
                var fallback = await _localRepo.GetAllAsync();
                _cachedStalls = fallback.Select(MapLocalToGeoSafe).ToList();
                return _cachedStalls;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var apiResult = await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken);

            var dtos = apiResult?.Data ?? new List<GeoStallDto>();

            // Giữ nguyên GeoStallDto để MapViewModel dùng trực tiếp
            _cachedStalls = dtos;
            _lastFetchUtc = DateTime.UtcNow;

            _logger.LogInformation("[StallService] Tải thành công {Count} gian hàng từ API", _cachedStalls.Count);
            return _cachedStalls;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService] Lỗi khi gọi API stalls");
            var fallback = await _localRepo.GetAllAsync();
            _cachedStalls = fallback.Select(MapLocalToGeoSafe).ToList();
            return _cachedStalls;
        }
    }

    public async Task<List<StallItem>> GetAllStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // OLD CODE (kept for reference): toàn bộ cache-first logic nằm trực tiếp trong GetAllStallsAsync.
        var stalls = await GetStallsAsync(forceRefresh, cancellationToken);

        // Trả về list mới để tách biệt dữ liệu trả về khỏi cache nội bộ.
        return stalls.Select(MapToStallItemSafe).ToList();
    }

    public async Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default)
    {
        var allStalls = await GetAllStallsAsync(false, cancellationToken);
        return allStalls.Take(6).ToList();   // Lấy 6 gian hàng nổi bật
    }

    public async Task<StallItem?> GetStallByIdAsync(Guid stallId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetAllStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(s => s.Id == stallId);
    }

    // ====================== MAPPING ======================

    private static GeoStallDto MapLocalToGeoSafe(LocalStall local) => new()
    {
        StallId = Guid.TryParse(local.StallId, out var parsedId) ? parsedId : Guid.Empty,
        StallName = local.StallName ?? string.Empty,
        Latitude = local.Latitude,
        Longitude = local.Longitude,
        RadiusMeters = local.RadiusMeters,
        NarrationContent = new GeoStallNarrationContentDto
        {
            Id = Guid.TryParse(local.NarrationContentId, out var narrationId) ? narrationId : Guid.Empty,
            Title = local.NarrationTitle ?? string.Empty,
            Description = local.NarrationDescription,
            ScriptText = local.NarrationScriptText ?? string.Empty,
            AudioUrl = local.LocalAudioPath ?? local.AudioUrl
        }
    };

    private static StallItem MapToStallItemSafe(GeoStallDto source) => new()
    {
        Id = source.StallId,
        Name = source.StallName ?? string.Empty,
        Description = source.NarrationContent?.Description ?? string.Empty,
        // OLD CODE (kept for reference): Slug = dto.Slug ?? string.Empty,
        Slug = BuildSlug(source.StallName),
        // OLD CODE (kept for reference): ImageUrl = dto.ImageUrl ?? "https://via.placeholder.com/300x200?text=No+Image",
        ImageUrl = "https://via.placeholder.com/300x200?text=No+Image",
        // OLD CODE (kept for reference): BusinessName = dto.BusinessName ?? string.Empty,
        BusinessName = string.Empty,
        IsActive = true,
        // OLD CODE (kept for reference): DistanceInKm = dto.DistanceInKm,
        DistanceInKm = 0,
        // OLD CODE (kept for reference): Rating = dto.Rating ?? 4.5
        Rating = 4.5
    };

    private static string BuildSlug(string? value)
    {
        // Tạo slug an toàn khi DTO/Local model không có trường Slug.
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToLowerInvariant().Replace(" ", "-");
    }
}