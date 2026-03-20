using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class VisitorLocationLogConfiguration : IEntityTypeConfiguration<VisitorLocationLog>
    {
        public void Configure(EntityTypeBuilder<VisitorLocationLog> b)
        {
            b.ToTable("VisitorLocationLogs");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Latitude)
             .HasPrecision(9, 6)
             .IsRequired();

            b.Property(x => x.Longitude)
             .HasPrecision(9, 6)
             .IsRequired();

            b.Property(x => x.AccuracyMeters)
             .HasPrecision(10, 2);

            b.Property(x => x.CapturedAtUtc)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(x => x.User)
             .WithMany(u => u.VisitorLocationLogs)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
