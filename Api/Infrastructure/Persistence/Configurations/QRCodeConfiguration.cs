using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Domain.Entities;
using LocateAndMultilingualNarration.Domain.Entities;

public class QRCodeConfiguration : IEntityTypeConfiguration<QrCode>
{
    public void Configure(EntityTypeBuilder<QrCode> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(100);

        // Index unique cho Code để kiểm tra nhanh và tránh trùng
        builder.HasIndex(x => x.Code)
               .IsUnique();

        // OLD CODE (kept for reference)
        // builder.Property(x => x.Type)
        //        .HasMaxLength(50);

        // OLD CODE (kept for reference)
        // builder.Property(x => x.Description)
        //        .HasMaxLength(500);

        // OLD CODE (kept for reference)
        // builder.Property(x => x.QrImageUrl)
        //        .HasMaxLength(500);

        builder.Property(x => x.Note)
               .HasMaxLength(500);
    }
}