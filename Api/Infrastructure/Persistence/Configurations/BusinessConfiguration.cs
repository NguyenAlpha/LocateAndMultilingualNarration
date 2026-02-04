using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class BusinessConfiguration : IEntityTypeConfiguration<Business>
    {
        public void Configure(EntityTypeBuilder<Business> b)
        {
            b.ToTable("Businesses");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.TaxCode).HasMaxLength(32);
            b.Property(x => x.ContactEmail).HasMaxLength(256);
            b.Property(x => x.ContactPhone).HasMaxLength(32);

            b.Property(x => x.CreatedAt)
             .HasColumnType("datetimeoffset(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasOne(x => x.OwnerUser)
             .WithMany(u => u.Businesses)
             .HasForeignKey(x => x.OwnerUserId)
             .OnDelete(DeleteBehavior.SetNull); // Cho phép chuyển giao/chưa gán
        }
    }
}
