using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LocateAndMultilingualNarration.Domain.Entities;

namespace LocateAndMultilingualNarration.Domain.Entities;

/// <summary>
/// Cấu hình Fluent API cho entity QrCode.
/// </summary>
public class QrCodeConfiguration : IEntityTypeConfiguration<QrCode>
{
    public void Configure(EntityTypeBuilder<QrCode> builder)
    {
        builder.ToTable("QrCodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("GETUTCDATE()");   // Mặc định là thời gian UTC hiện tại

        builder.Property(x => x.ValidDays)
               .IsRequired();

        builder.Property(x => x.IsUsed)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.UsedAt)
               .HasDefaultValue(null);

        builder.Property(x => x.UsedByDeviceId)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(x => x.Note)
               .HasMaxLength(500)
               .IsRequired(false);

        // Index để tra cứu nhanh khi validate QR
        builder.HasIndex(x => x.Code)
               .IsUnique();

        // Index hỗ trợ query theo trạng thái
        builder.HasIndex(x => x.IsUsed);
    }
}