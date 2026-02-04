using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> b)
        {
            b.ToTable("UserRoles");

            // Composite PK
            b.HasKey(x => new { x.UserId, x.RoleId });

            // FKs
            b.HasOne(x => x.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Role)
             .WithMany(r => r.UserRoles)
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
