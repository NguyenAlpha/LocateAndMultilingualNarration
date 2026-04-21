using Shared.DTOs.Common;
using Shared.DTOs.Geo;
using System.Net.Http.Json;

namespace Web.Services;

/// <summary>
/// Gọi các endpoint /api/geo (AllowAnonymous phía API).
/// Dùng server-side để tránh browser CORS khi Web và API ở 2 origin khác nhau.
/// </summary>
public class GeoApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<List<GeoStallDto>>?> GetStallsForMapAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ApiResult<List<GeoStallDto>>>("api/geo/stalls", ct);
        }
        catch (HttpRequestException) { return null; }
    }
}
