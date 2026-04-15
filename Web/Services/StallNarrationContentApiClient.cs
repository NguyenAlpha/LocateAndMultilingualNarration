using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Narrations;

namespace Web.Services
{
    public class StallNarrationContentApiClient
    {
        private readonly HttpClient _httpClient;

        public StallNarrationContentApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<StallNarrationContentDetailDto>>?> GetContentsAsync(int page, int pageSize, string? search, Guid? stallId, Guid? languageId, bool? isActive, CancellationToken cancellationToken = default)
        {
            var url = $"api/stall-narration-content?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            if (stallId.HasValue)
            {
                url += $"&stallId={stallId.Value}";
            }

            if (languageId.HasValue)
            {
                url += $"&languageId={languageId.Value}";
            }

            if (isActive.HasValue)
            {
                url += $"&isActive={isActive.Value.ToString().ToLowerInvariant()}";
            }

            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<StallNarrationContentDetailDto>>>(url, cancellationToken);
        }

        public async Task<ApiResult<StallNarrationContentWithAudiosDto>?> GetContentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<StallNarrationContentWithAudiosDto>>($"api/stall-narration-content/{id}", cancellationToken);
        }

        public async Task<ApiResult<StallNarrationContentDetailDto>?> UpdateContentAsync(Guid id, StallNarrationContentUpdateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/stall-narration-content/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentDetailDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<StallNarrationContentDetailDto>?> CreateContentAsync(StallNarrationContentCreateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/stall-narration-content", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentDetailDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<StallNarrationContentDetailDto>?> ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/stall-narration-content/{id}/status", isActive, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentDetailDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<TtsStatusDto>?> GetTtsStatusAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<TtsStatusDto>>($"api/stall-narration-content/{id}/tts-status", cancellationToken);
        }

        public async Task<ApiResult<StallNarrationContentDetailDto>?> RetryTtsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsync($"api/stall-narration-content/{id}/retry-tts", null, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentDetailDto>>(cancellationToken: cancellationToken);
        }
    }
}
