using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

public class DeviceLocationLogConfiguration : IEntityTypeConfiguration<DeviceLocationLog>
{
    public void Configure(EntityTypeBuilder<DeviceLocationLog> builder)
    {
        builder.ToTable("DeviceLocationLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Latitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(x => x.Longitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(x => x.AccuracyMeters)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.CapturedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => x.CapturedAtUtc);
    }
}
