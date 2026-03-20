using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class StallGeoFenceConfiguration : IEntityTypeConfiguration<StallGeoFence>
    {
        public void Configure(EntityTypeBuilder<StallGeoFence> b)
        {
            b.ToTable("StallGeoFences");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.PolygonJson)
             .HasColumnType("nvarchar(max)")
             .IsRequired();

            b.HasOne(x => x.Stall)
             .WithMany(s => s.StallGeoFences)
             .HasForeignKey(x => x.StallId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
