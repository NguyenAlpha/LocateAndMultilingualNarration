using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class StallMediaConfiguration : IEntityTypeConfiguration<StallMedia>
    {
        public void Configure(EntityTypeBuilder<StallMedia> b)
        {
            b.ToTable("StallMedia");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.MediaUrl)
             .HasMaxLength(512)
             .IsRequired();

            b.Property(x => x.MediaType)
             .HasMaxLength(32)
             .IsRequired();

            b.Property(x => x.Caption)
             .HasMaxLength(256);

            b.Property(x => x.SortOrder)
             .HasDefaultValue(0);

            b.Property(x => x.IsActive)
             .HasDefaultValue(true);

            b.HasOne(x => x.Stall)
             .WithMany(s => s.StallMedia)
             .HasForeignKey(x => x.StallId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
