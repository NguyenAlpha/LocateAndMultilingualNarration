using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Extensions
{
    public static class TtsVoiceProfileQueryExtensions
    {
        /// <summary>
        /// Kiểm tra voice profile có tồn tại và đang active không.
        /// </summary>
        public static Task<bool> IsActiveByIdAsync(
            this IQueryable<TtsVoiceProfile> query,
            Guid id,
            CancellationToken ct = default)
            => query.AnyAsync(v => v.Id == id && v.IsActive, ct);
    }
}
