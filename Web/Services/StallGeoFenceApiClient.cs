using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.StallGeoFences;

namespace Web.Services
{
    public class StallGeoFenceApiClient
    {
        private readonly HttpClient _httpClient;

        public StallGeoFenceApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<StallGeoFenceDetailDto>>?> GetGeoFencesAsync(int page, int pageSize, Guid? stallId, CancellationToken cancellationToken = default)
        {
            var url = $"api/stall-geo-fence?page={page}&pageSize={pageSize}";
            if (stallId.HasValue) url += $"&stallId={stallId.Value}";
            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<StallGeoFenceDetailDto>>>(url, cancellationToken);
        }

        public async Task<ApiResult<StallGeoFenceDetailDto>?> GetGeoFenceAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>($"api/stall-geo-fence/{id}", cancellationToken);
        }

        public async Task<ApiResult<StallGeoFenceDetailDto>?> CreateGeoFenceAsync(StallGeoFenceCreateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/stall-geo-fence", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<StallGeoFenceDetailDto>?> UpdateGeoFenceAsync(Guid id, StallGeoFenceUpdateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/stall-geo-fence/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(cancellationToken: cancellationToken);
        }
    }
}
