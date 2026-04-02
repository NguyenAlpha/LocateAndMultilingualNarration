using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.StallLocations;

namespace Web.Services
{
    public class StallLocationApiClient
    {
        private readonly HttpClient _httpClient;

        public StallLocationApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<StallLocationDetailDto>>?> GetLocationsAsync(int page, int pageSize, Guid? stallId, bool? isActive, CancellationToken cancellationToken = default, string? stallName = null)
        {
            var url = $"api/stall-location?page={page}&pageSize={pageSize}";
            if (stallId.HasValue) url += $"&stallId={stallId.Value}";
            if (isActive.HasValue) url += $"&isActive={isActive.Value}";
            if (!string.IsNullOrWhiteSpace(stallName)) url += $"&stallName={Uri.EscapeDataString(stallName)}";
            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<StallLocationDetailDto>>>(url, cancellationToken);
        }

        public async Task<ApiResult<StallLocationDetailDto>?> GetLocationAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<StallLocationDetailDto>>($"api/stall-location/{id}", cancellationToken);
        }

        public async Task<ApiResult<StallLocationDetailDto>?> CreateLocationAsync(StallLocationCreateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/stall-location", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<StallLocationDetailDto>?> UpdateLocationAsync(Guid id, StallLocationUpdateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/stall-location/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(cancellationToken: cancellationToken);
        }
    }
}
