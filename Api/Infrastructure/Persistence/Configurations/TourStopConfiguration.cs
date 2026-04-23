using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class TourStopConfiguration : IEntityTypeConfiguration<TourStop>
    {
        public void Configure(EntityTypeBuilder<TourStop> b)
        {
            b.ToTable("TourStops");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Order).IsRequired();
            b.Property(x => x.Note).HasMaxLength(256);

            b.Property(x => x.CreatedAt)
             .HasColumnType("datetimeoffset")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            b.HasIndex(x => new { x.TourId, x.Order });
            b.HasIndex(x => new { x.TourId, x.StallId }).IsUnique();

            b.HasOne(x => x.Tour)
             .WithMany(t => t.Stops)
             .HasForeignKey(x => x.TourId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Stall)
             .WithMany()
             .HasForeignKey(x => x.StallId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
