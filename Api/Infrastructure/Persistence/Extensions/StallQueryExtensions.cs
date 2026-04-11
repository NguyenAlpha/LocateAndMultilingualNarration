using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Extensions
{
    public static class StallQueryExtensions
    {
        /// <summary>
        /// Lấy Stall theo Id (có tracking để update/delete).
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        public static Task<Stall?> GetByIdAsync(
            this IQueryable<Stall> query,
            Guid id,
            CancellationToken ct = default)
            => query.FirstOrDefaultAsync(s => s.Id == id, ct);

        /// <summary>
        /// Lấy Stall theo Id (AsNoTracking, chỉ đọc).
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        public static Task<Stall?> GetByIdReadOnlyAsync(
            this IQueryable<Stall> query,
            Guid id,
            CancellationToken ct = default)
            => query.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id, ct);

        /// <summary>
        /// Kiểm tra Slug đã tồn tại chưa. Truyền excludeId để bỏ qua bản ghi hiện tại khi update.
        /// </summary>
        public static Task<bool> SlugExistsAsync(
            this IQueryable<Stall> query,
            string slug,
            Guid? excludeId = null,
            CancellationToken ct = default)
            => excludeId.HasValue
                ? query.AnyAsync(s => s.Slug == slug && s.Id != excludeId.Value, ct)
                : query.AnyAsync(s => s.Slug == slug, ct);
    }
}
