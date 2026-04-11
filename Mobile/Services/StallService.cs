using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mobile.LocalDb;
using Mobile.Models;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;

namespace Mobile.Services;

/// <summary>
/// Cung cấp dữ liệu gian hàng cho UI theo chiến lược cache-first: ưu tiên đọc local trước, sau đó mới gọi API khi cần.
/// </summary>
public interface IStallService
{
    /// <summary>
    /// Lấy toàn bộ danh sách gian hàng, có hỗ trợ cache.
    /// </summary>
    /// <param name="forceRefresh">Giá trị <c>true</c> để bỏ qua cache và gọi API mới.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Danh sách gian hàng đã được tải từ cache hoặc từ API.</returns>
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy một gian hàng cụ thể theo ID.
    /// </summary>
    /// <param name="stallId">Mã gian hàng cần tìm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Gian hàng tương ứng nếu tìm thấy; ngược lại trả về <c>null</c>.</returns>
    Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation của IStallService — Cache-First pattern:
///   1. Đọc từ SQLite (LocalStallRepository) → trả về ngay, render bản đồ tức thì
///   2. Nếu SQLite trống và có mạng → gọi API trực tiếp
///   3. Offline + SQLite trống → fallback mock
/// </summary>
public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ILocalStallRepository _localRepo;
    private readonly ILogger<StallService> _logger;

    // Cấu hình JsonSerializer để giải mã dữ liệu API.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/geo/stalls";

    public StallService(IHttpClientFactory httpClientFactory,IDeviceService deviceService, ILocalStallRepository localRepo, ILogger<StallService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _localRepo = localRepo;
        _logger = logger;
    }

    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Cache-First: đọc SQLite trước (nhanh, không cần mạng)
        if (!forceRefresh)
        {
            // Lấy dữ liệu đã lưu sẵn trong máy.
            var cached = await _localRepo.GetAllAsync();
            if (cached.Count > 0)
            {
                _logger.LogDebug("[StallService][GetStallsAsync]: {Count} stall từ SQLite", cached.Count);

                // Chuyển dữ liệu local sang DTO để UI dùng trực tiếp.
                return cached.Select(ToDto).ToList();
            }
        }

        // SQLite trống hoặc forceRefresh: kiểm tra mạng
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            // Không có mạng → fallback SQLite (dù forceRefresh = true)
            var offline = await _localRepo.GetAllAsync();
            if (offline.Count > 0)
            {
                _logger.LogWarning("[StallService][GetStallsAsync]: offline, fallback {Count} stall từ SQLite", offline.Count);
                return [.. offline.Select(ToDto)];
            }

            _logger.LogWarning("[StallService][GetStallsAsync]: offline và SQLite trống, trả về danh sách rỗng");
            return [];
        }

        try
        {
            // Lấy deviceId để API trả về dữ liệu đúng theo thiết bị/ngữ cảnh người dùng.
            var deviceId = _deviceService.GetOrCreateDeviceId();
            // Tạo URL gọi API danh sách gian hàng.
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var url = $"{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";

            _logger.LogInformation("[StallService][GetStallsAsync]: gọi API {Url}", url);
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[StallService][GetStallsAsync]: API trả về {StatusCode}, fallback SQLite", (int)response.StatusCode);
                var fallback = await _localRepo.GetAllAsync();
                return [.. fallback.Select(ToDto)];
            }

            // Giải mã dữ liệu JSON từ API.
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var dtos = (await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken))?.Data ?? [];

            _logger.LogInformation("[StallService][GetStallsAsync]: tải thành công {Count} gian hàng từ API", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            // Nếu lỗi bất ngờ thì trả về rỗng để tránh làm sập UI.
            _logger.LogError(ex, "[StallService][GetStallsAsync]: exception");
            return [];
        }
    }

    public async Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default)
    {
        // Tái sử dụng GetStallsAsync để lấy danh sách rồi tìm theo ID.
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.StallId.ToString().Equals(stallId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceId = _deviceService.GetOrCreateDeviceId();
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var url = $"/api/stalls?deviceId={Uri.EscapeDataString(deviceId)}";

            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: gọi API {Url}", url);
            
            using var response = await client.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[StallService][GetFeaturedStallsAsync]: API trả về {StatusCode}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<ApiResult<List<StallItem>>>(stream, JsonOptions, cancellationToken);
            var stalls = result?.Data ?? [];

            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: tải thành công {Count} gian hàng", stalls.Count);
            return stalls.Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService][GetFeaturedStallsAsync]: exception");
            return [];
        }
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180);
    

    /// <summary>
    /// Chuyển <see cref="LocalStall"/> sang <see cref="GeoStallDto"/>, ưu tiên audio cục bộ nếu đã tải sẵn.
    /// </summary>
    /// <param name="s">Dữ liệu local của gian hàng.</param>
    /// <returns>DTO gian hàng để UI sử dụng.</returns>
    private static GeoStallDto ToDto(LocalStall s) => new()
    {
        StallId = Guid.Parse(s.StallId),
        StallName = s.StallName,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        RadiusMeters = s.RadiusMeters,
        NarrationContent = s.NarrationContentId is null ? null : new GeoStallNarrationContentDto
        {
            Id = Guid.Parse(s.NarrationContentId),
            Title = s.NarrationTitle ?? string.Empty,
            Description = s.NarrationDescription,
            ScriptText = s.NarrationScriptText ?? string.Empty,
            // Ưu tiên file local đã tải, fallback về URL remote
            AudioUrl = s.LocalAudioPath ?? s.AudioUrl
        }
    };
}
