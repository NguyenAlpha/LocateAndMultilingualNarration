using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class BusinessOwnerProfileConfiguration : IEntityTypeConfiguration<BusinessOwnerProfile>
    {
        public void Configure(EntityTypeBuilder<BusinessOwnerProfile> b)
        {
            b.ToTable("BusinessOwnerProfiles");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.HasIndex(x => x.UserId).IsUnique();

            b.Property(x => x.OwnerName).HasMaxLength(256);
            b.Property(x => x.ContactInfo).HasMaxLength(256);
            b.Property(x => x.CreatedAt)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(x => x.User)
             .WithOne(u => u.BusinessOwnerProfile)
             .HasForeignKey<BusinessOwnerProfile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
