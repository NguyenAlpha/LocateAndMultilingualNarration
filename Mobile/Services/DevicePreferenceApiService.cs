using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.DevicePreferences;

namespace Mobile.Services;

public interface IDevicePreferenceApiService
{
    Task<DevicePreferenceDetailDto?> GetAsync(string deviceId, CancellationToken ct = default);
    Task<DevicePreferenceDetailDto?> UpsertAsync(DevicePreferenceUpsertDto dto, CancellationToken ct = default);
    Task SavePreferencesAsync(DevicePreferencesRequest request, CancellationToken ct = default);
}

public class DevicePreferenceApiService : IDevicePreferenceApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string BaseUrl = "http://10.0.2.2:5299";

    public DevicePreferenceApiService(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    public async Task<DevicePreferenceDetailDto?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/device-preference/{Uri.EscapeDataString(deviceId)}", ct);
            Console.WriteLine($"[DEBUG] GET /api/device-preference/{{deviceId}} => {(int)response.StatusCode} {response.StatusCode}");
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(cancellationToken: ct);
            return result?.Data;
        }
        catch { return null; }
    }

    public async Task<DevicePreferenceDetailDto?> UpsertAsync(DevicePreferenceUpsertDto dto, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/device-preference", dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(cancellationToken: ct);
            return result?.Data;
        }
        catch { return null; }
    }

    public async Task SavePreferencesAsync(DevicePreferencesRequest request, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        Console.WriteLine($"[DEBUG] POST /api/device-preferences DeviceId={request.DeviceId}, LanguageId={request.LanguageId}, Voice={request.Voice}");
        var response = await client.PostAsJsonAsync($"{BaseUrl}/api/device-preferences", request, ct);
        Console.WriteLine($"[DEBUG] POST /api/device-preferences => {(int)response.StatusCode} {response.StatusCode}");
        response.EnsureSuccessStatusCode();
    }
}
