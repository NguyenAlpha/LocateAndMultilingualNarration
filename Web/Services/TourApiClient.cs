using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Tours;

namespace Web.Services
{
    /// <summary>
    /// Giao tiếp với API <c>/api/tours</c>. HttpClient được inject sẵn Bearer token
    /// qua <see cref="AuthTokenHandler"/>. Trả null khi lỗi mạng thay vì ném exception.
    /// </summary>
    public class TourApiClient
    {
        private readonly HttpClient _httpClient;

        public TourApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<TourListItemDto>>?> GetToursAsync(
            int page = 1, int pageSize = 20, string? search = null, bool? isActive = null,
            CancellationToken ct = default)
        {
            var url = $"api/tours?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (isActive.HasValue)
                url += $"&isActive={isActive.Value.ToString().ToLower()}";

            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<TourListItemDto>>>(url, ct);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<TourDetailDto>?> GetTourDetailAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApiResult<TourDetailDto>>($"api/tours/{id}", ct);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<TourDetailDto>?> CreateTourAsync(TourCreateDto request, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/tours", request, ct);
                return await response.Content.ReadFromJsonAsync<ApiResult<TourDetailDto>>(cancellationToken: ct);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<TourDetailDto>?> UpdateTourAsync(Guid id, TourUpdateDto request, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/tours/{id}", request, ct);
                return await response.Content.ReadFromJsonAsync<ApiResult<TourDetailDto>>(cancellationToken: ct);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<bool>?> DeleteTourAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/tours/{id}", ct);
                return await response.Content.ReadFromJsonAsync<ApiResult<bool>>(cancellationToken: ct);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<TourDetailDto>?> ToggleActiveAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"api/tours/{id}/toggle-active", null, ct);
                return await response.Content.ReadFromJsonAsync<ApiResult<TourDetailDto>>(cancellationToken: ct);
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ApiResult<TourDetailDto>?> ReorderStopsAsync(Guid id, List<TourStopReorderDto> stops, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/tours/{id}/stops/reorder", stops, ct);
                return await response.Content.ReadFromJsonAsync<ApiResult<TourDetailDto>>(cancellationToken: ct);
            }
            catch (HttpRequestException) { return null; }
        }
    }
}
