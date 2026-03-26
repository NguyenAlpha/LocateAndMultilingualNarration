using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Networking;
using Mobile.Models;

namespace Mobile.Services;

public interface IStallService
{
    Task<List<Stall>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Stall?> GetStallByIdAsync(string boothId, CancellationToken cancellationToken = default);
}

public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private List<Stall>? _cachedStalls;
    private DateTime _lastFetchUtc;

    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/stalls";

    public StallService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<Stall>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Dùng cache để giảm số lần gọi API và tránh lag khi vào map nhiều lần.
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
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync($"{BaseUrl}{StallsEndpoint}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return _cachedStalls ?? GetMockStalls();
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParseStalls(raw);

            _cachedStalls = parsed.Count == 0 ? GetMockStalls() : parsed;
            _lastFetchUtc = DateTime.UtcNow;
            return _cachedStalls;
        }
        catch
        {
            return _cachedStalls ?? GetMockStalls();
        }
    }

    public async Task<Stall?> GetStallByIdAsync(string boothId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.Id.Equals(boothId, StringComparison.OrdinalIgnoreCase));
    }

    private static List<Stall> ParseStalls(string json)
    {
        var result = new List<Stall>();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var list = root;

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataNode))
        {
            list = dataNode;
        }

        if (list.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in list.EnumerateArray())
        {
            result.Add(new Stall
            {
                Id = item.TryGetProperty("id", out var id) ? id.ToString() : Guid.NewGuid().ToString(),
                Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? "Unknown stall" : "Unknown stall",
                Latitude = item.TryGetProperty("latitude", out var lat) ? lat.GetDouble() : 0,
                Longitude = item.TryGetProperty("longitude", out var lng) ? lng.GetDouble() : 0,
                AudioUrl = item.TryGetProperty("audioUrl", out var audio) ? audio.GetString() ?? string.Empty : string.Empty
            });
        }

        return result;
    }

    private static List<Stall> GetMockStalls() =>
    [
        new Stall { Id = "1", Name = "Bánh Mì Ông 3", Latitude = 10.762622, Longitude = 106.660172, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3" },
        new Stall { Id = "2", Name = "Phở Bò Bình Dân", Latitude = 10.7628, Longitude = 106.6595, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3" },
        new Stall { Id = "3", Name = "Kem Trái Cây", Latitude = 10.7625, Longitude = 106.6605, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3" }
    ];
}
