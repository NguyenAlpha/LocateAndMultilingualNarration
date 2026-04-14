using Mobile.Models;
using Shared.DTOs.Geo;

namespace Mobile.Services;

public interface IStallService
{
    /// <summary>
    /// Lấy danh sách gian hàng theo chiến lược cache-first (SQLite/API).
    /// </summary>
    Task<List<GeoStallDto>> GetStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách gian hàng (dùng cho StallListPage)
    /// </summary>
    Task<List<StallItem>> GetAllStallsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách gian hàng nổi bật cho trang Home
    /// </summary>
    Task<List<StallItem>> GetFeaturedStallsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy một gian hàng theo ID
    /// </summary>
    Task<StallItem?> GetStallByIdAsync(Guid stallId, CancellationToken cancellationToken = default);
}