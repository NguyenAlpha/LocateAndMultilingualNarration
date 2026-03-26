using System.Text.Json;
using Microsoft.Maui.Networking;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;

namespace Mobile.Services;

public interface IStallService
{
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default);
}

public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private List<GeoStallDto>? _cachedStalls;
    private DateTime _lastFetchUtc;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/geo/stalls";

    public StallService(IHttpClientFactory httpClientFactory, IDeviceService deviceService)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
    }

    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _cachedStalls is { Count: > 0 } && DateTime.UtcNow - _lastFetchUtc < CacheDuration)
        {
            return _cachedStalls;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return _cachedStalls ?? GetMockStalls();
        }

        try
        {
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var client = _httpClientFactory.CreateClient();
            var url = $"{BaseUrl}{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return _cachedStalls ?? GetMockStalls();
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken);
            var dtos = result?.Data ?? [];

            _cachedStalls = dtos.Count == 0 ? GetMockStalls() : dtos;
            _lastFetchUtc = DateTime.UtcNow;
            return _cachedStalls;
        }
        catch
        {
            return _cachedStalls ?? GetMockStalls();
        }
    }

    public async Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.StallId.ToString().Equals(stallId, StringComparison.OrdinalIgnoreCase));
    }

    private static List<GeoStallDto> GetMockStalls() =>
    [
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000001"), StallName = "Bánh Mì Ông 3",   Latitude = 10.777534, Longitude = 106.710669, RadiusMeters = 50, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3" },
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000002"), StallName = "Phở Bò Bình Dân", Latitude = 10.7782,   Longitude = 106.7115,   RadiusMeters = 40, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3" },
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000003"), StallName = "Kem Trái Cây",     Latitude = 10.7769,   Longitude = 106.7098,   RadiusMeters = 30, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3" }
    ];
}
