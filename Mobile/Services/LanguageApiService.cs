using Shared.DTOs.Common;
using Shared.DTOs.Languages;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mobile.Services;

public class LanguageApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LanguageApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<LanguageDetailDto>> GetActiveLanguagesAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResult<List<LanguageDetailDto>>>(
            "api/languages/active", _jsonOptions);

        return response?.Data ?? new List<LanguageDetailDto>();
    }
}
