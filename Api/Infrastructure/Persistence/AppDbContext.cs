using Api.Domain.Entities;
using LocateAndMultilingualNarration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Api.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Language> Languages => Set<Language>();
        public DbSet<BusinessOwnerProfile> BusinessOwnerProfiles => Set<BusinessOwnerProfile>();
        public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<Stall> Stalls => Set<Stall>();
        public DbSet<StallGeoFence> StallGeoFences => Set<StallGeoFence>();
        public DbSet<StallLocation> StallLocations => Set<StallLocation>();
        public DbSet<StallMedia> StallMedia => Set<StallMedia>();
        public DbSet<StallNarrationContent> StallNarrationContents => Set<StallNarrationContent>();
        public DbSet<NarrationAudio> NarrationAudios => Set<NarrationAudio>();
        public DbSet<TtsVoiceProfile> TtsVoiceProfiles => Set<TtsVoiceProfile>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<DevicePreference> DevicePreferences => Set<DevicePreference>();
        public DbSet<ScanLog> ScanLogs => Set<ScanLog>();
        public DbSet<SubscriptionOrder> SubscriptionOrders => Set<SubscriptionOrder>();
        public DbSet<DeviceLocationLog> DeviceLocationLogs => Set<DeviceLocationLog>();
        public DbSet<Tour> Tours => Set<Tour>();
        public DbSet<TourStop> TourStops => Set<TourStop>();

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    // Áp dụng toàn bộ configuration trong assembly hiện tại
        //    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        //    base.OnModelCreating(modelBuilder);
        //}
        // Thêm DbSet
        public DbSet<QrCode> QrCodes { get; set; } = null!;

        // Trong OnModelCreating:
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Khôi phục apply toàn bộ EntityTypeConfiguration để tránh thiếu key mapping (ví dụ UserRole).
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // OLD CODE (kept for reference): modelBuilder.ApplyConfiguration(new QrCodeConfiguration());
            // QrCodeConfiguration đã được apply khi quét assembly.

            // Các ApplyConfiguration khác...
        }
    }
}
