using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.DevicePreferences;

namespace Mobile.Services;

/// <summary>
/// Cung cấp API để lấy và cập nhật cấu hình ngôn ngữ/giọng đọc của thiết bị.
/// </summary>
public interface IDevicePreferenceApiService
{
    /// <summary>
    /// Lấy cấu hình của thiết bị theo deviceId.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Thông tin cấu hình thiết bị nếu lấy thành công; ngược lại <c>null</c>.</returns>
    Task<DevicePreferenceDetailDto?> GetAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Tạo mới hoặc cập nhật cấu hình thiết bị lên API.
    /// </summary>
    /// <param name="dto">Dữ liệu cấu hình cần lưu.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Dữ liệu cấu hình sau khi lưu nếu thành công; ngược lại <c>null</c>.</returns>
    Task<DevicePreferenceDetailDto?> UpsertAsync(DevicePreferenceUpsertDto dto, CancellationToken ct = default);
}

/// <summary>
/// Triển khai gọi API để đọc/ghi cấu hình thiết bị.
/// </summary>
public class DevicePreferenceApiService : IDevicePreferenceApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string BaseUrl = "http://10.0.2.2:5299";

    /// <summary>
    /// Khởi tạo service với <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory dùng để tạo HttpClient.</param>
    public DevicePreferenceApiService(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Lấy cấu hình của thiết bị theo deviceId.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Thông tin cấu hình thiết bị nếu lấy thành công; ngược lại <c>null</c>.</returns>
    public async Task<DevicePreferenceDetailDto?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            // Tạo client để gọi endpoint lấy cấu hình theo deviceId.
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/device-preference/{Uri.EscapeDataString(deviceId)}", ct);
            // Nếu server trả lỗi thì không xử lý tiếp.
            if (!response.IsSuccessStatusCode) return null;
            // Đọc JSON và lấy phần data từ ApiResult.
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(cancellationToken: ct);
            return result?.Data;
        }
        catch { return null; }
    }

    /// <summary>
    /// Tạo mới hoặc cập nhật cấu hình thiết bị lên API.
    /// </summary>
    /// <param name="dto">Dữ liệu cấu hình cần lưu.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Dữ liệu cấu hình sau khi lưu nếu thành công; ngược lại <c>null</c>.</returns>
    public async Task<DevicePreferenceDetailDto?> UpsertAsync(DevicePreferenceUpsertDto dto, CancellationToken ct = default)
    {
        try
        {
            // Tạo client để gửi dữ liệu cấu hình lên server.
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/device-preference", dto, ct);
            // Nếu server không chấp nhận request thì dừng lại.
            if (!response.IsSuccessStatusCode) return null;
            // Đọc lại kết quả đã lưu từ API.
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(cancellationToken: ct);
            return result?.Data;
        }
        catch { return null; }
    }
}
