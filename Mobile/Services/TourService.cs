using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Common;
using Shared.DTOs.Tours;

namespace Mobile.Services;

/// <summary>
/// Lấy dữ liệu tour từ API với cache memory 10 phút.
/// Không cần SQLite vì tour data nhỏ và ít thay đổi.
/// </summary>
public class TourService : ITourService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TourService> _logger;

    private const string ApiClientName = "ApiHttp";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private List<TourListItemDto>? _cachedList;
    private DateTime _lastListFetchUtc = DateTime.MinValue;

    private readonly Dictionary<Guid, (TourDetailDto dto, DateTime fetchedUtc)> _detailCache = new();

    public TourService(IHttpClientFactory httpClientFactory, ILogger<TourService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<TourListItemDto>> GetToursAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _cachedList is not null && DateTime.UtcNow - _lastListFetchUtc < CacheDuration)
            return _cachedList;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return _cachedList ?? [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var response = await client.GetAsync("/api/tours?page=1&pageSize=100", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TourService] API tours trả về {Status}", response.StatusCode);
                return _cachedList ?? [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var apiResult = await JsonSerializer.DeserializeAsync<ApiResult<PagedResult<TourListItemDto>>>(stream, JsonOptions, cancellationToken);

            _cachedList = apiResult?.Data?.Items?.ToList() ?? [];
            _lastListFetchUtc = DateTime.UtcNow;

            _logger.LogInformation("[TourService] Tải {Count} tour từ API", _cachedList.Count);
            return _cachedList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TourService] Lỗi khi gọi API tours");
            return _cachedList ?? [];
        }
    }

    public async Task<TourDetailDto?> GetTourDetailAsync(Guid tourId, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var hasCached = _detailCache.TryGetValue(tourId, out var cached);
        if (!forceRefresh && hasCached && DateTime.UtcNow - cached.fetchedUtc < CacheDuration)
        {
            return cached.dto;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return hasCached ? cached.dto : null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var response = await client.GetAsync($"/api/tours/{tourId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TourService] API tour/{Id} trả về {Status}", tourId, response.StatusCode);
                return hasCached ? cached.dto : null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var apiResult = await JsonSerializer.DeserializeAsync<ApiResult<TourDetailDto>>(stream, JsonOptions, cancellationToken);

            var dto = apiResult?.Data;
            if (dto is not null)
            {
                _detailCache[tourId] = (dto, DateTime.UtcNow);
            }
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TourService] Lỗi khi gọi API tour/{Id}", tourId);
            return hasCached ? cached.dto : null;
        }
    }

    public void InvalidateCache()
    {
        _cachedList = null;
        _lastListFetchUtc = DateTime.MinValue;
        _detailCache.Clear();
    }
}
