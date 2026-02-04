using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Domain.Entities;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> b)
        {
            b.ToTable("Roles");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(256).IsRequired();
            b.Property(x => x.ConcurrencyStamp).HasMaxLength(256);

            b.HasIndex(x => x.Name).IsUnique();
            b.HasIndex(x => x.NormalizedName).IsUnique();
        }
    }
}
