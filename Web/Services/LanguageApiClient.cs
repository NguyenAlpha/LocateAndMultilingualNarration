using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Languages;

namespace Web.Services
{
    public class LanguageApiClient
    {
        private readonly HttpClient _httpClient;

        public LanguageApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<IReadOnlyList<LanguageDetailDto>>?> GetActiveLanguagesAsync(CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<IReadOnlyList<LanguageDetailDto>>>("api/languages/active", cancellationToken);
        }
    }
}
