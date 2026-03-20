using System.Net.Http.Json;
using Shared.DTOs.Businesses;
using Shared.DTOs.Common;

namespace Web.Services
{
    public class BusinessApiClient
    {
        private readonly HttpClient _httpClient;

        public BusinessApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<BusinessDetailDto>>?> GetBusinessesAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var url = $"api/business?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<BusinessDetailDto>>>(url, cancellationToken);
        }

        public async Task<ApiResult<BusinessDetailDto>?> GetBusinessAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<BusinessDetailDto>>($"api/business/{id}", cancellationToken);
        }

        public async Task<ApiResult<BusinessDetailDto>?> CreateBusinessAsync(BusinessCreateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/business", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<BusinessDetailDto>?> UpdateBusinessAsync(Guid id, BusinessUpdateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/business/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(cancellationToken: cancellationToken);
        }
    }
}
