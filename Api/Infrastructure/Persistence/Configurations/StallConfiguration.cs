using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class StallConfiguration : IEntityTypeConfiguration<Stall>
    {
        public void Configure(EntityTypeBuilder<Stall> b)
        {
            b.ToTable("Stalls");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Description).HasMaxLength(256);
            b.Property(x => x.Slug).HasMaxLength(256).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique();

            b.Property(x => x.ContactEmail).HasMaxLength(256);
            b.Property(x => x.ContactPhone).HasMaxLength(16);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            // datetimeoffset default SYSDATETIMEOFFSET()
            b.Property(x => x.CreatedAt)
             .HasColumnType("datetimeoffset")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetimeoffset");

            b.HasOne(x => x.Business)
             .WithMany(biz => biz.Stalls)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
