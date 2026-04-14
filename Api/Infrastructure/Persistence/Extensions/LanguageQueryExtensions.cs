using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Extensions
{
    public static class LanguageQueryExtensions
    {
        /// <summary>
        /// Lấy Code của một Language theo Id. Trả về null nếu không tìm thấy.
        /// </summary>
        public static Task<string?> GetCodeByIdAsync(
            this IQueryable<Language> query,
            Guid id,
            CancellationToken ct = default)
            => query.AsNoTracking()
                    .Where(l => l.Id == id)
                    .Select(l => l.Code)
                    .FirstOrDefaultAsync(ct);

        /// <summary>
        /// Lấy dictionary LanguageId → Code cho một tập Id. Bỏ qua Id không tồn tại.
        /// </summary>
        public static Task<Dictionary<Guid, string>> GetCodeDictionaryAsync(
            this IQueryable<Language> query,
            IEnumerable<Guid> ids,
            CancellationToken ct = default)
            => query.AsNoTracking()
                    .Where(l => ids.Contains(l.Id))
                    .ToDictionaryAsync(l => l.Id, l => l.Code, ct);

        /// <summary>
        /// Lấy Language đang active theo Id. Trả về null nếu không tồn tại hoặc không active.
        /// </summary>
        public static Task<Language?> GetActiveByIdAsync(
            this IQueryable<Language> query,
            Guid id,
            CancellationToken ct = default)
            => query.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);

        /// <summary>
        /// Kiểm tra Code đã tồn tại chưa. Truyền excludeId để bỏ qua bản ghi hiện tại khi update.
        /// </summary>
        public static Task<bool> CodeExistsAsync(
            this IQueryable<Language> query,
            string code,
            Guid? excludeId = null,
            CancellationToken ct = default)
            => excludeId.HasValue
                ? query.AnyAsync(l => l.Code == code && l.Id != excludeId.Value, ct)
                : query.AnyAsync(l => l.Code == code, ct);
    }
}
