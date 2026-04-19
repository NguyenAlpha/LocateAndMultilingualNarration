using Shared.DTOs.Common;
using Shared.DTOs.QrCodes;
using System.Net.Http.Json;

namespace Web.Services;

/// <summary>
/// Giao tiếp với API endpoint <c>/api/qrcodes</c>.
/// HttpClient được inject sẵn Bearer token qua <see cref="AuthTokenHandler"/>.
/// Mọi method trả về <c>null</c> khi mạng lỗi thay vì ném exception,
/// để controller tự quyết định hiển thị thông báo lỗi phù hợp.
/// </summary>
public class QrCodeApiClient(HttpClient httpClient)
{
    /// <summary>
    /// Lấy danh sách QR có phân trang, hỗ trợ lọc theo trạng thái đã quét và hết hạn.
    /// </summary>
    /// <param name="page">Trang hiện tại (bắt đầu từ 1).</param>
    /// <param name="pageSize">Số item mỗi trang.</param>
    /// <param name="isUsed">Lọc theo đã quét (<c>true</c>), chưa quét (<c>false</c>), hoặc tất cả (<c>null</c>).</param>
    /// <param name="expired">Lọc theo đã hết hạn (<c>true</c>), còn hạn (<c>false</c>), hoặc tất cả (<c>null</c>).</param>
    public async Task<ApiResult<PagedResult<QrCodeDetailDto>>?> GetQrCodesAsync(
        int page = 1, int pageSize = 20,
        bool? isUsed = null, bool? expired = null,
        CancellationToken ct = default)
    {
        var url = $"api/qrcodes?page={page}&pageSize={pageSize}";

        // bool?.ToString() trả về "True"/"False" (viết hoa) — ToLower() để khớp với query param API yêu cầu
        if (isUsed.HasValue)  url += $"&isUsed={isUsed.Value.ToString().ToLower()}";
        if (expired.HasValue) url += $"&expired={expired.Value.ToString().ToLower()}";

        try
        {
            return await httpClient.GetFromJsonAsync<ApiResult<PagedResult<QrCodeDetailDto>>>(url, ct);
        }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>
    /// Lấy chi tiết một mã QR theo ID.
    /// </summary>
    public async Task<ApiResult<QrCodeDetailDto>?> GetQrCodeAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ApiResult<QrCodeDetailDto>>($"api/qrcodes/{id}", ct);
        }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>
    /// Tạo mã QR mới. API sẽ sinh chuỗi code ngẫu nhiên và render ảnh PNG.
    /// </summary>
    /// <param name="request">Thông tin tạo QR: số ngày hiệu lực, ghi chú.</param>
    public async Task<ApiResult<QrCodeDetailDto>?> CreateQrCodeAsync(
        QrCodeCreateDto request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/qrcodes", request, ct);
            return await response.Content.ReadFromJsonAsync<ApiResult<QrCodeDetailDto>>(cancellationToken: ct);
        }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>
    /// Xoá mã QR. Chỉ xoá được mã chưa quét — API trả lỗi nếu mã đã được dùng.
    /// </summary>
    public async Task<ApiResult<object?>?> DeleteQrCodeAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/qrcodes/{id}", ct);
            return await response.Content.ReadFromJsonAsync<ApiResult<object?>>(cancellationToken: ct);
        }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>
    /// Tải ảnh PNG của mã QR dưới dạng mảng byte, dùng để trả file download cho browser.
    /// Khác các method khác: trả <c>byte[]</c> thay vì <c>ApiResult</c> vì response là binary, không phải JSON.
    /// </summary>
    public async Task<byte[]?> GetQrCodeImageAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/qrcodes/{id}/image", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException) { return null; }
    }
}
