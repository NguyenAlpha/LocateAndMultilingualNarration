using Api.Domain.Entities;
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
        public DbSet<VisitorProfile> VisitorProfiles => Set<VisitorProfile>();
        public DbSet<Language> Languages => Set<Language>();
        public DbSet<BusinessOwnerProfile> BusinessOwnerProfiles => Set<BusinessOwnerProfile>();
        public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<Stall> Stalls => Set<Stall>();
        public DbSet<StallGeoFence> StallGeoFences => Set<StallGeoFence>();
        public DbSet<StallLocation> StallLocations => Set<StallLocation>();
        public DbSet<VisitorPreference> VisitorPreferences => Set<VisitorPreference>();
        public DbSet<StallMedia> StallMedia => Set<StallMedia>();
        public DbSet<StallNarrationContent> StallNarrationContents => Set<StallNarrationContent>();
        public DbSet<NarrationAudio> NarrationAudios => Set<NarrationAudio>();
        public DbSet<TtsVoiceProfile> TtsVoiceProfiles => Set<TtsVoiceProfile>();
        public DbSet<VisitorLocationLog> VisitorLocationLogs => Set<VisitorLocationLog>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<DevicePreference> DevicePreferences => Set<DevicePreference>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Áp dụng toàn bộ configuration trong assembly hiện tại
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
