using Shared.DTOs.Tours;

namespace Mobile.Services;

/// <summary>
/// Cung cấp danh sách tour và chi tiết tour cho Mobile theo chiến lược cache-first (memory).
/// Tour được Admin tạo từ Web → Mobile chỉ đọc, không ghi.
/// </summary>
public interface ITourService
{
    /// <summary>
    /// Lấy danh sách tour đang hoạt động. Cache memory 10 phút.
    /// </summary>
    Task<List<TourListItemDto>> GetToursAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết 1 tour kèm danh sách stops đã sắp xếp. Cache từng tour 10 phút.
    /// </summary>
    Task<TourDetailDto?> GetTourDetailAsync(Guid tourId, bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa memory cache — gọi sau khi detect tour data thay đổi.
    /// </summary>
    void InvalidateCache();
}
