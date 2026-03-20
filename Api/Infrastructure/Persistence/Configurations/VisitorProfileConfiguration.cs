using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class VisitorProfileConfiguration : IEntityTypeConfiguration<VisitorProfile>
    {
        public void Configure(EntityTypeBuilder<VisitorProfile> b)
        {
            b.ToTable("VisitorProfiles");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            // UNIQUE FK -> Users(Id)
            b.HasIndex(x => x.UserId).IsUnique();

            b.Property(x => x.CreatedAt)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(x => x.User)
             .WithOne(u => u.VisitorProfile)
             .HasForeignKey<VisitorProfile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Language)
             .WithMany(l => l.VisitorProfiles)
             .HasForeignKey(x => x.LanguageId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
