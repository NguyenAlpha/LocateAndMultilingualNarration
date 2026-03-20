using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class VisitorPreferenceConfiguration : IEntityTypeConfiguration<VisitorPreference>
    {
        public void Configure(EntityTypeBuilder<VisitorPreference> b)
        {
            b.ToTable("VisitorPreferences");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.HasIndex(x => x.UserId).IsUnique();

            b.Property(x => x.Voice)
             .HasMaxLength(64);

            b.Property(x => x.SpeechRate)
             .HasPrecision(4, 2);

            b.Property(x => x.AutoPlay)
             .HasDefaultValue(false);

            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetimeoffset");

            b.HasOne(x => x.User)
             .WithOne(u => u.VisitorPreference)
             .HasForeignKey<VisitorPreference>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Language)
             .WithMany(l => l.VisitorPreferences)
             .HasForeignKey(x => x.LanguageId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
