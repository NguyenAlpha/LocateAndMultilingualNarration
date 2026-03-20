using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class StallLocationConfiguration : IEntityTypeConfiguration<StallLocation>
    {
        public void Configure(EntityTypeBuilder<StallLocation> b)
        {
            b.ToTable("StallLocations");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Latitude)
             .HasPrecision(9, 6)
             .IsRequired();

            b.Property(x => x.Longitude)
             .HasPrecision(9, 6)
             .IsRequired();

            b.Property(x => x.RadiusMeters)
             .HasPrecision(10, 2);

            b.Property(x => x.Address)
             .HasMaxLength(256);

            b.Property(x => x.MapProviderPlaceId)
             .HasMaxLength(128);

            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetimeoffset");

            b.Property(x => x.IsActive)
             .HasDefaultValue(true);

            b.HasOne(x => x.Stall)
             .WithMany(s => s.StallLocations)
             .HasForeignKey(x => x.StallId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
