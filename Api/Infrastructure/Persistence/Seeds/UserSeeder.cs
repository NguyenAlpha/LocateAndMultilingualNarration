using Api.Domain.Entities;

namespace Api.Infrastructure.Persistence.Seeds
{
    public static class UserSeeder
    {
        // Thay đổi password mặc định sau khi deploy lần đầu
        private const string DefaultAdminPassword = "Admin@123";

        public static async Task SeedAsync(AppDbContext db)
        {
            await SeedAdminAsync(db);
        }

        private static async Task SeedAdminAsync(AppDbContext db)
        {
            const string normalizedUser = "ADMIN";

            if (db.Users.Any(u => u.NormalizedUserName == normalizedUser))
                return;

            var adminRole = db.Roles.First(r => r.NormalizedName == "ADMIN");

            var now  = DateTime.UtcNow;
            var user = new User
            {
                Id                 = Guid.NewGuid(),
                UserName           = "admin",
                NormalizedUserName = normalizedUser,
                Email              = "admin@system.local",
                NormalizedEmail    = "ADMIN@SYSTEM.LOCAL",
                EmailConfirmed     = true,
                PasswordHash       = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword),
                SecurityStamp      = Guid.NewGuid().ToString(),
                ConcurrencyStamp   = Guid.NewGuid().ToString(),
                DisplayName        = "System Administrator",
                IsActive           = true,
                LockoutEnabled     = false,
                CreatedAt          = now,
                UpdatedAt          = now
            };

            db.Users.Add(user);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });

            await db.SaveChangesAsync();
        }
    }
}
