using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Extensions
{
    public static class DevicePreferenceQueryExtensions
    {
        /// <summary>
        /// Lấy DevicePreference theo DeviceId (có tracking để update/insert).
        /// Trả về null nếu chưa có bản ghi cho device này.
        /// </summary>
        public static Task<DevicePreference?> GetByDeviceIdAsync(
            this IQueryable<DevicePreference> query,
            string deviceId,
            CancellationToken ct = default)
            => query.FirstOrDefaultAsync(x => x.DeviceId == deviceId, ct);

        /// <summary>
        /// Lấy DevicePreference theo DeviceId (AsNoTracking, chỉ đọc).
        /// Trả về null nếu chưa có bản ghi cho device này.
        /// </summary>
        public static Task<DevicePreference?> GetByDeviceIdReadOnlyAsync(
            this IQueryable<DevicePreference> query,
            string deviceId,
            CancellationToken ct = default)
            => query.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DeviceId == deviceId, ct);
    }
}
