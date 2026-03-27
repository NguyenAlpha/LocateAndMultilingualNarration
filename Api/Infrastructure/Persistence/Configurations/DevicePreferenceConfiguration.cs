using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class DevicePreferenceConfiguration : IEntityTypeConfiguration<DevicePreference>
    {
        public void Configure(EntityTypeBuilder<DevicePreference> b)
        {
            b.ToTable("DevicePreferences");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.DeviceId)
             .IsRequired()
             .HasMaxLength(128);
            b.HasIndex(x => x.DeviceId).IsUnique();

            b.Property(x => x.Voice)
             .HasMaxLength(64);

            b.Property(x => x.SpeechRate)
             .HasPrecision(4, 2)
             .HasDefaultValue(1.0m);

            b.Property(x => x.AutoPlay)
             .HasDefaultValue(true);

            b.Property(x => x.Platform)
             .HasMaxLength(32);

            b.Property(x => x.DeviceModel)
             .HasMaxLength(128);

            b.Property(x => x.Manufacturer)
             .HasMaxLength(128);

            b.Property(x => x.OsVersion)
             .HasMaxLength(64);

            b.Property(x => x.FirstSeenAt)
             .HasColumnType("datetimeoffset");

            b.Property(x => x.LastSeenAt)
             .HasColumnType("datetimeoffset");

            b.HasOne(x => x.Language)
             .WithMany(l => l.DevicePreferences)
             .HasForeignKey(x => x.LanguageId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
