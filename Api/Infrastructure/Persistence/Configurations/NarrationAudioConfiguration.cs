using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class NarrationAudioConfiguration : IEntityTypeConfiguration<NarrationAudio>
    {
        public void Configure(EntityTypeBuilder<NarrationAudio> b)
        {
            b.ToTable("NarrationAudios");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.AudioUrl)
             .HasMaxLength(512);

            b.Property(x => x.BlobId)
             .HasMaxLength(128);

            b.Property(x => x.Voice)
             .HasMaxLength(64);

            b.Property(x => x.Provider)
             .HasMaxLength(64);

            b.Property(x => x.IsTts)
             .HasDefaultValue(false);

            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetimeoffset");

            b.HasOne(x => x.NarrationContent)
             .WithMany(nc => nc.NarrationAudios)
             .HasForeignKey(x => x.NarrationContentId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.NarrationContentId, x.TtsVoiceProfileId })
             .IsUnique();
        }
    }
}
