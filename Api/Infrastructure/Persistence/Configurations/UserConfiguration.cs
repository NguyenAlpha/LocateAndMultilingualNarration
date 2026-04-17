using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("Users");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
             .HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.UserName).HasMaxLength(256);
            b.Property(x => x.NormalizedUserName).HasMaxLength(256);
            b.HasIndex(x => x.NormalizedUserName).IsUnique();

            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.NormalizedEmail).HasMaxLength(256);

            b.Property(x => x.EmailConfirmed).HasDefaultValue(false);
            b.Property(x => x.PasswordHash).HasMaxLength(256);
            b.Property(x => x.SecurityStamp).HasMaxLength(256);
            b.Property(x => x.ConcurrencyStamp).HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(32);
            b.Property(x => x.PhoneNumberConfirmed).HasDefaultValue(false);
            b.Property(x => x.DisplayName).HasMaxLength(256);
            b.Property(x => x.Sex).HasMaxLength(16);

            // DATE
            b.Property(x => x.DateOfBirth)
             .HasColumnType("date");

            b.Property(x => x.TwoFactorEnabled).HasDefaultValue(false);

            // datetimeoffset(3)
            b.Property(x => x.LockoutEnd)
             .HasColumnType("datetimeoffset(3)");

            b.Property(x => x.LockoutEnabled).HasDefaultValue(true);
            b.Property(x => x.AccessFailedCount).HasDefaultValue(0);

            // datetime2(3)
            b.Property(x => x.LastLoginAt).HasColumnType("datetime2(3)");
            b.Property(x => x.CreatedAt)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.DeletedAt).HasColumnType("datetime2(3)");

            b.Property(x => x.IsActive).HasDefaultValue(true);

            // 1-1 profiles
            b.HasOne(x => x.BusinessOwnerProfile)
             .WithOne(p => p.User)
             .HasForeignKey<BusinessOwnerProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.EmployeeProfile)
             .WithOne(p => p.User)
             .HasForeignKey<EmployeeProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
