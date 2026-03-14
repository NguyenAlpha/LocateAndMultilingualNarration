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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Áp dụng toàn bộ configuration trong assembly hiện tại
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
