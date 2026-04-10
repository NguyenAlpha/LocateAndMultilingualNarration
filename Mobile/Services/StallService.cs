using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
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
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default);
    Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation của IStallService — Cache-First pattern.
/// </summary>
public class StallService : IStallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDeviceService _deviceService;
    private readonly ILocalStallRepository _localRepo;
    private readonly IServiceProvider _serviceProvider;
    private ISyncService? _resolvedSyncService;
    private readonly ILogger<StallService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string BaseUrl = "http://10.0.2.2:5299";
    private const string StallsEndpoint = "/api/geo/stalls";

    public StallService(
        IHttpClientFactory httpClientFactory,
        IDeviceService deviceService,
        ILocalStallRepository localRepo,
        IServiceProvider serviceProvider,
        ILogger<StallService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _deviceService = deviceService;
        _localRepo = localRepo;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách gian hàng theo chiến lược cache-first.
    /// </summary>
    public async Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh)
        {
            var cached = await _localRepo.GetAllAsync();
            if (cached.Count > 0)
            {
                _logger.LogDebug("[StallService][GetStallsAsync]: {Count} stall từ SQLite", cached.Count);

                if (ShouldSync())
                {
                    _ = TriggerBackgroundSyncSafelyAsync();
                }

                return cached.Select(ToDto).ToList();
            }
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            var offline = await _localRepo.GetAllAsync();
            if (offline.Count > 0)
            {
                _logger.LogWarning("[StallService][GetStallsAsync]: offline, fallback {Count} stall từ SQLite", offline.Count);
                return offline.Select(ToDto).ToList();
            }
            _logger.LogWarning("[StallService][GetStallsAsync]: offline và SQLite trống");
            return [];
        }

        try
        {
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var client = _httpClientFactory.CreateClient();
            var url = $"{BaseUrl}{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";

            _logger.LogInformation("[StallService][GetStallsAsync]: gọi API {Url}", url);

            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[StallService][GetStallsAsync]: API trả về {StatusCode}, fallback SQLite", (int)response.StatusCode);
                var fallback = await _localRepo.GetAllAsync();
                return fallback.Select(ToDto).ToList();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var dtos = (await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, cancellationToken))?.Data ?? [];

            _logger.LogInformation("[StallService][GetStallsAsync]: tải thành công {Count} gian hàng từ API", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService][GetStallsAsync]: exception");
            return [];
        }
    }

    public async Task<GeoStallDto?> GetStallByIdAsync(string stallId, CancellationToken cancellationToken = default)
    {
        var stalls = await GetStallsAsync(false, cancellationToken);
        return stalls.FirstOrDefault(x => x.StallId.ToString().Equals(stallId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lấy danh sách gian hàng nổi bật cho MainPage với khoảng cách thực tế và ảnh đúng.
    /// </summary>
    public async Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: Bắt đầu lấy featured stalls với dữ liệu mới");

            var geoStalls = await GetStallsAsync(forceRefresh: true, cancellationToken);

            if (geoStalls.Count == 0)
            {
                _logger.LogWarning("[StallService][GetFeaturedStallsAsync]: Không có stall nào từ API");
                return [];
            }

            var userLocation = await GetCurrentUserLocationAsync();

            var featured = new List<StallItem>();

            foreach (var dto in geoStalls.Take(6))
            {
                double distanceKm = 0;

                if (userLocation != null && dto.Latitude != 0 && dto.Longitude != 0)
                {
                    distanceKm = CalculateDistance(
                        userLocation.Latitude,
                        userLocation.Longitude,
                        dto.Latitude,
                        dto.Longitude);
                }

                var item = new StallItem
                {
                    Id = dto.StallId,
                    Name = string.IsNullOrWhiteSpace(dto.StallName) ? "Không có tên" : dto.StallName,
                    Description = GetSafeDescription(dto),
                    ImageUrl = GetSafeImageUrl(dto),
                    DistanceInKm = distanceKm
                };

                featured.Add(item);
            }

            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: Trả về {Count} gian hàng nổi bật", featured.Count);
            return featured;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService][GetFeaturedStallsAsync]: Lỗi khi lấy featured stalls");
            return [];
        }
    }

    private static string GetSafeDescription(GeoStallDto dto)
    {
        if (dto.NarrationContent == null)
            return "Chưa có mô tả";

        return FirstNonEmpty(
            dto.NarrationContent.Description,
            dto.NarrationContent.ScriptText,
            dto.NarrationContent.Title,
            "Chưa có mô tả"
        );
    }

    private static string GetSafeImageUrl(GeoStallDto dto)
    {
        // Hiện tại GeoStallNarrationContentDto chưa có ImageUrl
        // Nếu sau này backend thêm thì mở comment dòng dưới
        // if (dto.NarrationContent?.ImageUrl != null) return dto.NarrationContent.ImageUrl;

        return "https://via.placeholder.com/300x200?text=No+Image";
    }
    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        return string.Empty;
    }

    private async Task<Location?> GetCurrentUserLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    return null;
            }

            return await Geolocation.GetLastKnownLocationAsync()
                ?? await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });
        }
        catch
        {
            return null;
        }
    }

    public double CalculateDistance(double userLat, double userLng, double stallLat, double stallLng)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = ToRadians(stallLat - userLat);
        var dLon = ToRadians(stallLng - userLng);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(userLat)) * Math.Cos(ToRadians(stallLat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);

    // ====================================================================
    // OLD CODE (kept for reference) - Không xóa phần này
    // ====================================================================
    /*
    public async Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: Bắt đầu lấy featured stalls");

            var stalls = await GetStallsAsync(forceRefresh: false, cancellationToken);
            if (stalls.Count == 0)
            {
                _logger.LogWarning("[StallService][GetFeaturedStallsAsync]: Không có stall từ GetStallsAsync");
                return [];
            }

            var featured = stalls.Select(dto => new StallItem
            {
                Id = dto.StallId,
                Name = dto.StallName ?? "Không có tên",
                Slug = dto.Slug ?? string.Empty,
                Description = dto.NarrationContent?.Description ?? "Chưa có mô tả",
                ImageUrl = GetFirstImageUrl(dto) ?? "https://via.placeholder.com/300x200?text=No+Image",
                BusinessName = dto.BusinessName ?? string.Empty,
                IsActive = dto.IsActive,
                DistanceInKm = 0.0
            }).Take(6).ToList();

            _logger.LogInformation("[StallService][GetFeaturedStallsAsync]: Trả về {Count} gian hàng nổi bật", featured.Count);
            return featured;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StallService][GetFeaturedStallsAsync]: Lỗi khi lấy featured stalls");
            return [];
        }
    }

    private static string? GetFirstImageUrl(GeoStallDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.NarrationContent?.ImageUrl))
            return dto.NarrationContent.ImageUrl;

        return null;
    }

    public double CalculateDistance(double userLat, double userLng, double stallLat, double stallLng)
    {
        const double R = 6371;
        var dLat = ToRadians(stallLat - userLat);
        var dLon = ToRadians(stallLng - userLng);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(userLat)) * Math.Cos(ToRadians(stallLat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180);
    */

    private bool ShouldSync()
    {
        _resolvedSyncService ??= _serviceProvider.GetService<ISyncService>();
        if (_resolvedSyncService is null)
            return false;

        return _resolvedSyncService.LastSyncedAt is null
            || DateTime.UtcNow - _resolvedSyncService.LastSyncedAt.Value > TimeSpan.FromMinutes(3);
    }

    private async Task TriggerBackgroundSyncSafelyAsync()
    {
        try
        {
            _resolvedSyncService ??= _serviceProvider.GetService<ISyncService>();
            if (_resolvedSyncService is null) return;
            await _resolvedSyncService.SyncAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TriggerBackgroundSyncSafelyAsync: lỗi khi sync ngầm");
        }
    }

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
            AudioUrl = s.LocalAudioPath ?? s.AudioUrl
        }
    };
}