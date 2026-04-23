using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class TourConfiguration : IEntityTypeConfiguration<Tour>
    {
        public void Configure(EntityTypeBuilder<Tour> b)
        {
            b.ToTable("Tours");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1024);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.Property(x => x.CreatedAt)
             .HasColumnType("datetimeoffset")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetimeoffset");

            b.HasIndex(x => x.IsActive);

            b.HasOne(x => x.CreatedByUser)
             .WithMany()
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
