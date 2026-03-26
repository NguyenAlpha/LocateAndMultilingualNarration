using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Stalls;

namespace Web.Services
{
    // StallApiClient: client chuyên để gọi các endpoint liên quan tới stall
    // - Dùng HttpClient đã được cấu hình sẵn trong DI
    // - Tách riêng logic gọi API để controller/service dễ đọc và dễ bảo trì
    public class StallApiClient
    {
        private readonly HttpClient _httpClient;

        // Constructor: HttpClient sẽ được inject từ IHttpClientFactory
        public StallApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Lấy danh sách stall có phân trang, tìm kiếm và lọc theo businessId
        // - Tạo query string động dựa trên tham số truyền vào
        // - Gọi GET và deserialize response JSON về ApiResult<PagedResult<StallDetailDto>>
        public async Task<ApiResult<PagedResult<StallDetailDto>>?> GetStallsAsync(int page, int pageSize, string? search, Guid? businessId, CancellationToken cancellationToken = default)
        {
            var url = $"api/stall?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                // EscapeDataString để tránh lỗi khi chuỗi search có ký tự đặc biệt
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            if (businessId.HasValue)
            {
                url += $"&businessId={businessId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<StallDetailDto>>>(url, cancellationToken);
        }

        // Lấy chi tiết một stall theo id
        public async Task<ApiResult<StallDetailDto>?> GetStallAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<StallDetailDto>>($"api/stall/{id}", cancellationToken);
        }

        // Tạo stall mới bằng cách POST dữ liệu StallCreateDto lên API
        public async Task<ApiResult<StallDetailDto>?> CreateStallAsync(StallCreateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/stall", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(cancellationToken: cancellationToken);
        }

        // Cập nhật stall theo id bằng cách PUT dữ liệu StallUpdateDto lên API
        public async Task<ApiResult<StallDetailDto>?> UpdateStallAsync(Guid id, StallUpdateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/stall/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(cancellationToken: cancellationToken);
        }
    }
}
