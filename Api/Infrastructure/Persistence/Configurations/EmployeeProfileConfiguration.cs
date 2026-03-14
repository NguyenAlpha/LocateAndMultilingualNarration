using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
    {
        public void Configure(EntityTypeBuilder<EmployeeProfile> b)
        {
            b.ToTable("EmployeeProfiles");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.HasIndex(x => x.UserId).IsUnique();

            b.Property(x => x.Department).HasMaxLength(256);
            b.Property(x => x.Position).HasMaxLength(256);

            b.Property(x => x.CreatedAt)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(x => x.User)
             .WithOne(u => u.EmployeeProfile)
             .HasForeignKey<EmployeeProfile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
