using System.Net.Http.Json;
using Mobile.Models;
using Shared.DTOs.Common;

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
    Task<Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto?> GetAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Tạo mới hoặc cập nhật cấu hình thiết bị lên API.
    /// </summary>
    /// <param name="dto">Dữ liệu cấu hình cần lưu.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Dữ liệu cấu hình sau khi lưu nếu thành công; ngược lại <c>null</c>.</returns>
    Task<Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto?> UpsertAsync(Shared.DTOs.DevicePreferences.DevicePreferenceUpsertDto dto, CancellationToken ct = default);
    Task SavePreferencesAsync(Shared.DTOs.DevicePreferences.DevicePreferencesRequest request, CancellationToken ct = default);

    // OLD CODE (kept for reference): service cũ chỉ expose GetAsync(deviceId)/UpsertAsync(sharedDto).
    // API mới cho ProfilePage: tự lấy deviceId hiện tại và trả về ApiResult wrapper.
    Task<DevicePreferenceDetailDto?> GetByDeviceIdAsync(CancellationToken ct = default);
    Task<ApiResult<DevicePreferenceDetailDto>> UpsertAsync(DevicePreferenceUpsertDto dto, CancellationToken ct = default);
}

/// <summary>
/// Triển khai gọi API để đọc/ghi cấu hình thiết bị.
/// </summary>
public class DevicePreferenceApiService : IDevicePreferenceApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private const string BaseUrl = "http://10.0.2.2:5299";

    /// <summary>
    /// Khởi tạo service với <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory dùng để tạo HttpClient.</param>
    public DevicePreferenceApiService(IHttpClientFactory httpClientFactory, IDeviceService deviceService)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
    }

    /// <summary>
    /// Lấy cấu hình của thiết bị theo deviceId.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Thông tin cấu hình thiết bị nếu lấy thành công; ngược lại <c>null</c>.</returns>
    public async Task<Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            // Tạo client để gọi endpoint lấy cấu hình theo deviceId.
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/device-preference/{Uri.EscapeDataString(deviceId)}", ct);
            Console.WriteLine($"[DEBUG] GET /api/device-preference/{{deviceId}} => {(int)response.StatusCode} {response.StatusCode}");
            // Nếu server trả lỗi thì không xử lý tiếp.
            if (!response.IsSuccessStatusCode) return null;
            // Đọc JSON và lấy phần data từ ApiResult.
            var result = await response.Content.ReadFromJsonAsync<ApiResult<Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto>>(cancellationToken: ct);
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
    public async Task<Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto?> UpsertAsync(Shared.DTOs.DevicePreferences.DevicePreferenceUpsertDto dto, CancellationToken ct = default)
    {
        try
        {
            // Tạo client để gửi dữ liệu cấu hình lên server.
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/device-preference", dto, ct);
            // Nếu server không chấp nhận request thì dừng lại.
            if (!response.IsSuccessStatusCode) return null;
            // Đọc lại kết quả đã lưu từ API.
            var result = await response.Content.ReadFromJsonAsync<ApiResult<Shared.DTOs.DevicePreferences.DevicePreferenceDetailDto>>(cancellationToken: ct);
            return result?.Data;
        }
        catch { return null; }
    }

    public async Task SavePreferencesAsync(Shared.DTOs.DevicePreferences.DevicePreferencesRequest request, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        Console.WriteLine($"[DEBUG] POST /api/device-preferences DeviceId={request.DeviceId}, LanguageId={request.LanguageId}, Voice={request.Voice}");
        var response = await client.PostAsJsonAsync($"{BaseUrl}/api/device-preferences", request, ct);
        Console.WriteLine($"[DEBUG] POST /api/device-preferences => {(int)response.StatusCode} {response.StatusCode}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<DevicePreferenceDetailDto?> GetByDeviceIdAsync(CancellationToken ct = default)
    {
        try
        {
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var data = await GetAsync(deviceId, ct);
            if (data is null)
                return null;

            return new DevicePreferenceDetailDto
            {
                DeviceId = data.DeviceId,
                LanguageCode = data.LanguageCode,
                LanguageName = data.LanguageName,
                Voice = data.Voice,
                SpeechRate = data.SpeechRate,
                AutoPlay = data.AutoPlay,
                LastSeenAt = data.LastSeenAt
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResult<DevicePreferenceDetailDto>> UpsertAsync(DevicePreferenceUpsertDto dto, CancellationToken ct = default)
    {
        try
        {
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var deviceInfo = _deviceService.GetDeviceInfo();

            if (string.IsNullOrWhiteSpace(dto.LanguageCode))
            {
                return ApiResult<DevicePreferenceDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.Validation,
                    Message = "Thiếu LanguageCode để lưu DevicePreference"
                });
            }

            // OLD CODE (kept for reference): chỉ dùng LanguageCode có sẵn từ caller.
            var sharedDto = new Shared.DTOs.DevicePreferences.DevicePreferenceUpsertDto
            {
                DeviceId = deviceId,
                LanguageCode = dto.LanguageCode,
                Voice = dto.Voice,
                SpeechRate = dto.SpeechRate ?? 1.0m,
                AutoPlay = dto.AutoPlay ?? true,
                Platform = dto.Platform ?? deviceInfo.Platform,
                DeviceModel = dto.DeviceModel ?? deviceInfo.DeviceModel,
                Manufacturer = dto.Manufacturer ?? deviceInfo.Manufacturer,
                OsVersion = dto.OsVersion ?? deviceInfo.OsVersion
            };

            var data = await UpsertAsync(sharedDto, ct);
            if (data is null)
            {
                return ApiResult<DevicePreferenceDetailDto>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.ServerError,
                    Message = "Không thể lưu cấu hình thiết bị"
                });
            }

            return ApiResult<DevicePreferenceDetailDto>.FromData(new DevicePreferenceDetailDto
            {
                DeviceId = data.DeviceId,
                LanguageCode = data.LanguageCode,
                LanguageName = data.LanguageName,
                Voice = data.Voice,
                SpeechRate = data.SpeechRate,
                AutoPlay = data.AutoPlay,
                LastSeenAt = data.LastSeenAt
            });
        }
        catch (Exception ex)
        {
            return ApiResult<DevicePreferenceDetailDto>.FromError(new ErrorDetail
            {
                Code = ErrorCode.ServerError,
                Message = ex.Message
            });
        }
    }
}
