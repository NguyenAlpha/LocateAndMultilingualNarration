using System.Net.Http.Json;
using Shared.DTOs.Businesses;
using Shared.DTOs.Common;

namespace Web.Services
{
    // BusinessApiClient: client chuyên để gọi các endpoint liên quan tới business
    // - Dùng HttpClient đã được cấu hình sẵn trong DI
    // - Tách riêng logic gọi API để controller/service dễ đọc và dễ bảo trì
    public class BusinessApiClient
    {
        private readonly HttpClient _httpClient;

        // Constructor: HttpClient sẽ được inject từ IHttpClientFactory
        public BusinessApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Lấy danh sách business có phân trang và tìm kiếm
        // - Tạo query string động dựa trên tham số truyền vào
        // - Gọi GET và deserialize response JSON về ApiResult<PagedResult<BusinessDetailDto>>
        public async Task<ApiResult<PagedResult<BusinessDetailDto>>?> GetBusinessesAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var url = $"api/business?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                // EscapeDataString để tránh lỗi khi chuỗi search có ký tự đặc biệt
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<BusinessDetailDto>>>(url, cancellationToken);
        }

        // Lấy chi tiết của một business theo id
        public async Task<ApiResult<BusinessDetailDto>?> GetBusinessAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<BusinessDetailDto>>($"api/business/{id}", cancellationToken);
        }

        // Tạo business mới bằng cách POST dữ liệu BusinessCreateDto lên API
        public async Task<ApiResult<BusinessDetailDto>?> CreateBusinessAsync(BusinessCreateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/business", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(cancellationToken: cancellationToken);
        }

        // Cập nhật business theo id bằng cách PUT dữ liệu BusinessUpdateDto lên API
        public async Task<ApiResult<BusinessDetailDto>?> UpdateBusinessAsync(Guid id, BusinessUpdateDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/business/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(cancellationToken: cancellationToken);
        }
    }
}
