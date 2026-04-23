using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Extensions
{
    public static class TourQueryExtensions
    {
        /// <summary>
        /// Lấy Tour theo Id (có tracking).
        /// </summary>
        public static Task<Tour?> GetByIdAsync(
            this IQueryable<Tour> query,
            Guid id,
            CancellationToken ct = default)
            => query.FirstOrDefaultAsync(t => t.Id == id, ct);

        /// <summary>
        /// Lấy Tour theo Id (AsNoTracking).
        /// </summary>
        public static Task<Tour?> GetByIdReadOnlyAsync(
            this IQueryable<Tour> query,
            Guid id,
            CancellationToken ct = default)
            => query.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id, ct);

        /// <summary>
        /// Kiểm tra Name đã tồn tại chưa. Truyền excludeId để bỏ qua bản ghi hiện tại khi update.
        /// </summary>
        public static Task<bool> NameExistsAsync(
            this IQueryable<Tour> query,
            string name,
            Guid? excludeId = null,
            CancellationToken ct = default)
            => excludeId.HasValue
                ? query.AnyAsync(t => t.Name == name && t.Id != excludeId.Value, ct)
                : query.AnyAsync(t => t.Name == name, ct);
    }
}
