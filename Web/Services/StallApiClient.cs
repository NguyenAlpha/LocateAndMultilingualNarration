using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Stalls;

namespace Web.Services
{
    public class StallApiClient
    {
        private readonly HttpClient _httpClient;

        public StallApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<StallDetailDto>>?> GetStallsAsync(int page, int pageSize, string? search, Guid? businessId, CancellationToken cancellationToken = default)
        {
            var url = $"api/stall?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (businessId.HasValue)
                url += $"&businessId={businessId.Value}";

            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<StallDetailDto>>>(url, cancellationToken);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<StallDetailDto>?> GetStallAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResult<StallDetailDto>>($"api/stall/{id}", cancellationToken);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<StallDetailDto>?> CreateStallAsync(StallCreateDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/stall", request, cancellationToken);
                return await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(cancellationToken: cancellationToken);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<StallDetailDto>?> UpdateStallAsync(Guid id, StallUpdateDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/stall/{id}", request, cancellationToken);
                return await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(cancellationToken: cancellationToken);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<StallDetailDto>?> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"api/stall/{id}/toggle-active", null, cancellationToken);
                return await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(cancellationToken: cancellationToken);
            }
            catch (HttpRequestException) { return null; }
        }
    }
}
