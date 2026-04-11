using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Extensions
{
    public static class UserQueryExtensions
    {
        /// <summary>
        /// Kiểm tra email đã được đăng ký chưa (so sánh qua NormalizedEmail).
        /// </summary>
        public static Task<bool> EmailExistsAsync(
            this IQueryable<User> query,
            string email,
            CancellationToken ct = default)
            => query.AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);

        /// <summary>
        /// Kiểm tra username đã tồn tại chưa (so sánh qua NormalizedUserName).
        /// </summary>
        public static Task<bool> UserNameExistsAsync(
            this IQueryable<User> query,
            string userName,
            CancellationToken ct = default)
            => query.AnyAsync(u => u.NormalizedUserName == userName.ToUpperInvariant(), ct);
    }
}
