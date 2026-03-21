using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class StallNarrationContentConfiguration : IEntityTypeConfiguration<StallNarrationContent>
    {
        public void Configure(EntityTypeBuilder<StallNarrationContent> b)
        {
            b.ToTable("StallNarrationContents");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Title)
             .HasMaxLength(128)
             .IsRequired();

            b.Property(x => x.Description)
             .HasMaxLength(256);

            b.Property(x => x.ScriptText)
             .HasColumnType("nvarchar(max)")
             .IsRequired();

            b.Property(x => x.IsActive)
             .HasDefaultValue(true);

            b.Property(x => x.UpdatedAt)
             .HasColumnType("datetimeoffset");

            b.HasOne(x => x.Stall)
             .WithMany(s => s.StallNarrationContents)
             .HasForeignKey(x => x.StallId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Language)
             .WithMany(l => l.StallNarrationContents)
             .HasForeignKey(x => x.LanguageId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
