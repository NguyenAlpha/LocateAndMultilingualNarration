using Shared.DTOs.Common;
using Shared.DTOs.SubscriptionOrders;
using System.Net.Http.Json;

namespace Web.Services
{
    public class SubscriptionOrderApiClient(HttpClient httpClient)
    {
        public async Task<ApiResult<SubscriptionOrderDetailDto>?> CreateOrderAsync(
            SubscriptionOrderCreateDto request, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync("api/subscription-orders", request, ct);
            return await response.Content.ReadFromJsonAsync<ApiResult<SubscriptionOrderDetailDto>>(cancellationToken: ct);
        }

        public async Task<ApiResult<PagedResult<SubscriptionOrderDetailDto>>?> GetOrdersAsync(
            int page = 1, int pageSize = 20,
            string? plan = null, string? status = null, Guid? businessId = null,
            CancellationToken ct = default)
        {
            var url = $"api/subscription-orders?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(plan)) url += $"&plan={Uri.EscapeDataString(plan)}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (businessId.HasValue) url += $"&businessId={businessId}";
            return await httpClient.GetFromJsonAsync<ApiResult<PagedResult<SubscriptionOrderDetailDto>>>(url, ct);
        }
    }
}
