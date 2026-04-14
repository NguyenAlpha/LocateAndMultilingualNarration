using Shared.DTOs.Businesses;
using Shared.DTOs.Common;
using System.Net.Http.Json;

namespace Web.Services
{
    public class SubscriptionApiClient(HttpClient httpClient)
    {
        public async Task<ApiResult<BusinessDetailDto>?> UpdateSubscriptionAsync(
            Guid businessId, SubscriptionUpdateDto request, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync($"api/business/{businessId}/subscription", request, ct);
            return await response.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(cancellationToken: ct);
        }
    }
}
