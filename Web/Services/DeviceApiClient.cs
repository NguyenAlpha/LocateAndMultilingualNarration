using Shared.DTOs.Common;
using Shared.DTOs.Geo;
using System.Net.Http.Json;

namespace Web.Services;

public class DeviceApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<ActiveDevicesSummaryDto>?> GetActiveDevicesAsync(
        int withinMinutes = 5, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ApiResult<ActiveDevicesSummaryDto>>(
                $"api/geo/active-devices?withinMinutes={withinMinutes}", ct);
        }
        catch (HttpRequestException) { return null; }
    }
}
