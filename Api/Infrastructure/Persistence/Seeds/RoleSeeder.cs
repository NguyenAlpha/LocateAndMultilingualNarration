using Api.Domain.Entities;

namespace Api.Infrastructure.Persistence.Seeds
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            string[] roleNames = ["Admin", "BusinessOwner"];

            foreach (var name in roleNames)
            {
                var normalized = name.ToUpperInvariant();
                if (!db.Roles.Any(r => r.NormalizedName == normalized))
                {
                    db.Roles.Add(new Role
                    {
                        Id               = Guid.NewGuid(),
                        Name             = name,
                        NormalizedName   = normalized,
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
