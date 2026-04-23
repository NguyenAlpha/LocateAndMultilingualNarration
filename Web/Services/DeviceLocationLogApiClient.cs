using Shared.DTOs.Common;
using Shared.DTOs.DeviceLocationLogs;
using System.Net.Http.Json;

namespace Web.Services;

/// <summary>
/// Gọi các endpoint log GPS thiết bị. HttpClient được inject Bearer qua <see cref="AuthTokenHandler"/>.
/// </summary>
public class DeviceLocationLogApiClient(HttpClient httpClient)
{
    /// <summary>
    /// Lấy dữ liệu heatmap gom nhóm theo tọa độ. Gọi <c>GET /api/device-location-log/heatmap</c> (AdminOnly).
    /// </summary>
    public async Task<ApiResult<List<HeatmapPointDto>>?> GetHeatmapAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? deviceId = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = new List<string>();
            if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
            if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
            if (!string.IsNullOrWhiteSpace(deviceId)) query.Add($"deviceId={Uri.EscapeDataString(deviceId)}");

            var url = "api/device-location-log/heatmap";
            if (query.Count > 0) url += "?" + string.Join("&", query);

            return await httpClient.GetFromJsonAsync<ApiResult<List<HeatmapPointDto>>>(url, ct);
        }
        catch (HttpRequestException) { return null; }
    }
}
