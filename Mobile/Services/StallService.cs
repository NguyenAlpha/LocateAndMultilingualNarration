using System.Text.Json;
using Microsoft.Extensions.Logging;
// Kiểm tra trạng thái kết nối mạng (có Internet hay không)
using Microsoft.Maui.Networking;
// DTO bọc response chung của API: { success, data, message }
using Shared.DTOs.Common;
// DTO thông tin gian hàng kèm tọa độ địa lý
using Shared.DTOs.Geo;

namespace Mobile.Services;

/// <summary>
/// Contract (hợp đồng) cho StallService — các class khác chỉ phụ thuộc vào interface này,
/// không phụ thuộc trực tiếp vào StallService, giúp dễ test và thay thế implementation.
/// </summary>
public interface IStallService
{
    /// <summary>Lấy toàn bộ danh sách gian hàng, có hỗ trợ cache.</summary>
    /// <param name="forceRefresh">true = bỏ qua cache, gọi API mới (mặc định false)</param>
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>Lấy một gian hàng cụ thể theo ID (tìm trong cache/danh sách đã tải).</summary>
    Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation của IStallService.
/// Chiến lược lấy dữ liệu theo thứ tự ưu tiên:
///   1. Cache còn hạn (< 5 phút) và không forceRefresh → trả về ngay
///   2. Không có mạng → trả về cache cũ (nếu có) hoặc dữ liệu mock
///   3. Có mạng → gọi API, cập nhật cache → trả về
///   4. API lỗi / exception → fallback về cache cũ hoặc dữ liệu mock
/// </summary>
public class StallService : IStallService
{
    // Factory tạo HttpClient — dùng factory thay vì new HttpClient() để tránh socket exhaustion
    private readonly IHttpClientFactory _httpClientFactory;

    // Lấy DeviceId để gửi kèm request (backend dùng để nhận dạng thiết bị)
    private readonly IDeviceService _deviceService;

    private readonly ILogger<StallService> _logger;

    // Thời gian cache hợp lệ — sau 5 phút cache hết hạn, lần gọi tiếp theo sẽ fetch API mới
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // Danh sách gian hàng đang được cache trong bộ nhớ
    private List<GeoStallDto>? _cachedStalls;

    // Thời điểm lần cuối fetch thành công từ API (UTC)
    private DateTime _lastFetchUtc;

    // Cấu hình JSON: JsonSerializerDefaults.Web = camelCase, case-insensitive, number từ string...
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Địa chỉ backend — 10.0.2.2 là alias của localhost trên Android Emulator
    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/geo/stalls";

    public StallService(IHttpClientFactory httpClientFactory, IDeviceService deviceService, ILogger<StallService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách gian hàng với chiến lược cache + offline fallback.
    /// </summary>
    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // 1. Trả về cache nếu: không yêu cầu refresh, cache có dữ liệu, và chưa hết hạn
        if (!forceRefresh && _cachedStalls is { Count: > 0 } && DateTime.UtcNow - _lastFetchUtc < CacheDuration)
        {
            _logger.LogDebug("GetStallsAsync: trả về cache ({Count} gian hàng)", _cachedStalls.Count);
            return _cachedStalls;
        }

        // 2. Không có Internet → dùng dữ liệu cũ hoặc mock để app vẫn hoạt động được
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            var offlineSource = _cachedStalls != null ? "cache cũ" : "mock";
            _logger.LogWarning("GetStallsAsync: không có Internet, dùng {Source}", offlineSource);
            return _cachedStalls ?? GetMockStalls();
        }

        try
        {
            // 3. Lấy DeviceId và gọi API
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var client = _httpClientFactory.CreateClient();

            // Gửi deviceId qua query string — Uri.EscapeDataString đảm bảo ký tự đặc biệt được encode
            var url = $"{BaseUrl}{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";
            _logger.LogInformation("GetStallsAsync: gọi API {Url}", url);
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // API trả về lỗi (4xx, 5xx) → fallback
                var statusCode = (int)response.StatusCode;
                var errorSource = _cachedStalls != null ? "cache cũ" : "mock";
                _logger.LogWarning("GetStallsAsync: API trả về {StatusCode}, dùng {Source}", statusCode, errorSource);
                return _cachedStalls ?? GetMockStalls();
            }

            // Deserialize stream trực tiếp (không đọc vào string trước) — hiệu quả hơn về memory
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken);
            var dtos = result?.Data ?? [];

            // Nếu API trả về rỗng → dùng mock để trang không bị trống hoàn toàn
            _cachedStalls = dtos.Count == 0 ? GetMockStalls() : dtos;
            _lastFetchUtc = DateTime.UtcNow; // Cập nhật thời điểm fetch thành công

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var isMock = dtos.Count == 0 ? " (mock)" : "";
                _logger.LogInformation("GetStallsAsync: tải thành công {Count} gian hàng{IsMock}", _cachedStalls.Count, isMock);
            }
            return _cachedStalls;
        }
        catch (Exception ex)
        {
            // 4. Mọi exception (timeout, parse lỗi...) → fallback về cache hoặc mock
            _logger.LogError(ex, "GetStallsAsync: exception, dùng {Source}",
                _cachedStalls != null ? "cache cũ" : "mock");
            return _cachedStalls ?? GetMockStalls();
        }
    }

    /// <summary>
    /// Tìm gian hàng theo ID — tận dụng cache từ GetStallsAsync thay vì gọi API riêng.
    /// OrdinalIgnoreCase: so sánh không phân biệt hoa thường.
    /// </summary>
    public async Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.StallId.ToString().Equals(stallId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Dữ liệu mock dùng khi không có mạng hoặc API chưa có dữ liệu.
    /// Giúp dev/test app mà không cần backend đang chạy.
    /// Tọa độ mock nằm gần khu vực trung tâm TP.HCM (Q1).
    /// </summary>
    private static List<GeoStallDto> GetMockStalls() =>
    [
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000001"), StallName = "Bánh Mì Ông 3",   Latitude = 10.777534, Longitude = 106.710669, RadiusMeters = 50, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3" },
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000002"), StallName = "Phở Bò Bình Dân", Latitude = 10.7782,   Longitude = 106.7115,   RadiusMeters = 40, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3" },
        new GeoStallDto { StallId = Guid.Parse("00000000-0000-0000-0000-000000000003"), StallName = "Kem Trái Cây",     Latitude = 10.7769,   Longitude = 106.7098,   RadiusMeters = 30, AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3" }
    ];
}
