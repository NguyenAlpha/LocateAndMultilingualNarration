using System.Text.Json;
using Shared.DTOs.TtsVoiceProfiles;

namespace Mobile.Services;

public interface IVoiceService
{
    Task<IReadOnlyList<TtsVoiceProfileListItemDto>> GetVoicesByLanguageAsync(Guid languageId, CancellationToken cancellationToken = default);
}

public class VoiceService : IVoiceService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string BaseUrl = "http://10.0.2.2:5299";

    public VoiceService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<TtsVoiceProfileListItemDto>> GetVoicesByLanguageAsync(Guid languageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/tts-voice-profiles/active?languageId={languageId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseVoices(raw);
        }
        catch
        {
            return [];
        }
    }

    private static List<TtsVoiceProfileListItemDto> ParseVoices(string json)
    {
        var result = new List<TtsVoiceProfileListItemDto>();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var list = root;

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataNode))
            list = dataNode;

        if (list.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in list.EnumerateArray())
        {
            result.Add(new TtsVoiceProfileListItemDto
            {
                Id          = item.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : Guid.Empty,
                LanguageId  = item.TryGetProperty("languageId", out var lid) && lid.TryGetGuid(out var lg) ? lg : Guid.Empty,
                DisplayName = item.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty,
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Style       = item.TryGetProperty("style", out var st) ? st.GetString() : null,
                Role        = item.TryGetProperty("role", out var role) ? role.GetString() : null,
                IsDefault   = item.TryGetProperty("isDefault", out var isd) && isd.GetBoolean(),
                Priority    = item.TryGetProperty("priority", out var pri) ? pri.GetInt32() : 0
            });
        }

        return result;
    }
}
