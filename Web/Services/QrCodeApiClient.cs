using Shared.DTOs.Common;
using Shared.DTOs.QrCodes;
using System.Net.Http.Json;

namespace Web.Services;

public class QrCodeApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<PagedResult<QrCodeDetailDto>>?> GetQrCodesAsync(
        int page = 1, int pageSize = 20,
        bool? isUsed = null, bool? expired = null,
        CancellationToken ct = default)
    {
        var url = $"api/qrcodes?page={page}&pageSize={pageSize}";
        if (isUsed.HasValue) url += $"&isUsed={isUsed.Value.ToString().ToLower()}";
        if (expired.HasValue) url += $"&expired={expired.Value.ToString().ToLower()}";
        return await httpClient.GetFromJsonAsync<ApiResult<PagedResult<QrCodeDetailDto>>>(url, ct);
    }

    public async Task<ApiResult<QrCodeDetailDto>?> CreateQrCodeAsync(
        QrCodeCreateDto request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/qrcodes", request, ct);
        return await response.Content.ReadFromJsonAsync<ApiResult<QrCodeDetailDto>>(cancellationToken: ct);
    }

    public async Task<ApiResult<object?>?> DeleteQrCodeAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/qrcodes/{id}", ct);
        return await response.Content.ReadFromJsonAsync<ApiResult<object?>>(cancellationToken: ct);
    }

    public async Task<byte[]?> GetQrCodeImageAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/qrcodes/{id}/image", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
