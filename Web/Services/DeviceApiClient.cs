using Shared.DTOs.Common;
using Shared.DTOs.Geo;
using System.Net.Http.Json;

namespace Web.Services;

/// <summary>
/// Giao tiếp với các API endpoint liên quan đến thiết bị mobile.
/// HttpClient được inject sẵn Bearer token qua <see cref="AuthTokenHandler"/>.
/// </summary>
public class DeviceApiClient(HttpClient httpClient)
{
    /// <summary>
    /// Lấy danh sách thiết bị đang hoạt động trong khoảng thời gian gần nhất.
    /// Gọi <c>GET /api/geo/active-devices</c> — endpoint AdminOnly.
    /// </summary>
    /// <param name="withinMinutes">
    /// Lọc thiết bị có GPS ping trong <paramref name="withinMinutes"/> phút qua.
    /// Controller đã clamp về [1, 60] trước khi gọi hàm này.
    /// </param>
    /// <returns><c>null</c> nếu mạng lỗi; controller tự xử lý fallback.</returns>
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

    /// <summary>
    /// Yêu cầu API đặt cờ <c>NeedsReset = true</c> cho thiết bị chỉ định.
    /// Thiết bị sẽ tự phát hiện cờ này qua <c>SyncBackgroundService</c> (chu kỳ ~3 phút),
    /// sau đó xóa toàn bộ Preferences và điều hướng về LoadingPage.
    /// </summary>
    /// <param name="deviceId">
    /// GUID dạng chuỗi của thiết bị. Được <see cref="Uri.EscapeDataString"/> để tránh
    /// ký tự đặc biệt làm sai URL.
    /// </param>
    /// <returns><c>true</c> nếu API xác nhận thành công; <c>false</c> nếu lỗi.</returns>
    public async Task<bool> ResetDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            // Body là null vì endpoint chỉ cần deviceId trên URL, không cần request body
            var response = await httpClient.PostAsync(
                $"api/device-preference/{Uri.EscapeDataString(deviceId)}/reset", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
    }
}
