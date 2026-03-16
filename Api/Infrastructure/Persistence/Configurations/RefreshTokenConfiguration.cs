using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.ToTable("RefreshTokens");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
             .HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.TokenHash)
             .HasMaxLength(256)
             .IsRequired();

            b.HasIndex(x => x.TokenHash)
             .IsUnique();

            b.HasIndex(x => new { x.UserId, x.RevokedAtUtc });

            b.Property(x => x.DeviceId)
             .HasMaxLength(128);

            b.Property(x => x.CreatedByIp)
             .HasMaxLength(64);

            b.Property(x => x.CreatedAtUtc)
             .HasColumnType("datetime2(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property(x => x.ExpiresAtUtc)
             .HasColumnType("datetime2(3)");

            b.Property(x => x.RevokedAtUtc)
             .HasColumnType("datetime2(3)");

            b.HasOne(x => x.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
