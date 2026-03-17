using Api.Domain.Entities;
using Api.Infrastructure.Persistence;
using BCrypt.Net;

namespace TestAPI
{
    public static class TestDataSeeder
    {
        public static void SeedRoles(AppDbContext context)
        {
            if (context.Roles.Any())
            {
                return;
            }

            context.Roles.AddRange(
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "BusinessOwner",
                    NormalizedName = "BUSINESSOWNER",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    NormalizedName = "USER",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });

            context.SaveChanges();
        }

        public static void SeedLanguages(AppDbContext context)
        {
            if (context.Languages.Any())
            {
                return;
            }

            context.Languages.AddRange(
                new Language
                {
                    Id = Guid.NewGuid(),
                    Name = "Vietnamese",
                    Code = "vi",
                    IsActive = true
                },
                new Language
                {
                    Id = Guid.NewGuid(),
                    Name = "English",
                    Code = "en",
                    IsActive = true
                },
                new Language
                {
                    Id = Guid.NewGuid(),
                    Name = "Japanese",
                    Code = "ja",
                    IsActive = false
                });

            context.SaveChanges();
        }

        public static User SeedUserWithRole(AppDbContext context, string email, string userName, string password, string roleName)
        {
            var existing = context.Users.FirstOrDefault(u => u.NormalizedEmail == email.ToUpperInvariant());
            if (existing != null)
            {
                return existing;
            }

            var role = context.Roles.First(r => r.NormalizedName == roleName.ToUpperInvariant());

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                PhoneNumber = "0900000000",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            context.SaveChanges();

            return user;
        }

        public static Business SeedBusiness(AppDbContext context, Guid ownerUserId, string name)
        {
            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = name,
                OwnerUserId = ownerUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            context.Businesses.Add(business);
            context.SaveChanges();

            return business;
        }
    }
}
