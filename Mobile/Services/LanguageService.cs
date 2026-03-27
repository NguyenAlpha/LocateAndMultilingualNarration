using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mobile.Helpers;
using Shared.DTOs.Languages;

namespace Mobile.Services;

public interface ILanguageService
{
    Task<IReadOnlyList<LanguageDetailDto>> GetLanguagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserLanguageAsync(string languageId, CancellationToken cancellationToken = default);
}

public class LanguageService : ILanguageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SessionService _sessionService;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private List<LanguageDetailDto>? _cachedLanguages;
    private DateTime _lastFetchUtc;

    private const string BaseUrl = "http://10.0.2.2:5299";

    public LanguageService(IHttpClientFactory httpClientFactory, SessionService sessionService)
    {
        _httpClientFactory = httpClientFactory;
        _sessionService = sessionService;
    }

    public async Task<IReadOnlyList<LanguageDetailDto>> GetLanguagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Cache để tránh gọi API lặp lại gây lag UI.
        if (!forceRefresh && _cachedLanguages is { Count: > 0 } && DateTime.UtcNow - _lastFetchUtc < CacheDuration)
        {
            return _cachedLanguages;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/languages/active", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return _cachedLanguages ?? [];
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var languages = ParseLanguages(raw);

            _cachedLanguages = languages;
            _lastFetchUtc = DateTime.UtcNow;
            return languages;
        }
        catch
        {
            return _cachedLanguages ?? [];
        }
    }

    public async Task<bool> UpdateUserLanguageAsync(string languageId, CancellationToken cancellationToken = default)
    {
        // OLD CODE: trước đây chỉ lưu local preference, không đồng bộ API.
        if (string.IsNullOrWhiteSpace(languageId))
        {
            return false;
        }

        var token = _sessionService.GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = JsonSerializer.Serialize(new { languageId = Guid.Parse(languageId) });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var putResponse = await client.PutAsync($"{BaseUrl}/api/visitor-profile", content, cancellationToken);
            if (putResponse.IsSuccessStatusCode)
            {
                return true;
            }

            // Nếu profile chưa tồn tại, thử tạo mới.
            var postResponse = await client.PostAsync($"{BaseUrl}/api/visitor-profile", content, cancellationToken);
            return postResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static List<LanguageDetailDto> ParseLanguages(string json)
    {
        var result = new List<LanguageDetailDto>();
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
            result.Add(new LanguageDetailDto
            {
                Id          = item.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : Guid.Empty,
                Name        = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Code        = item.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty,
                DisplayName = item.TryGetProperty("displayName", out var dn) ? dn.GetString() : null,
                FlagCode    = item.TryGetProperty("flagCode", out var fc) ? fc.GetString() : null,
                IsActive    = item.TryGetProperty("isActive", out var ia) && ia.GetBoolean()
            });
        }

        return result;
    }
}
