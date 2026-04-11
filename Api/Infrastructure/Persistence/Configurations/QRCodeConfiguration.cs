using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Domain.Entities;

public class QRCodeConfiguration : IEntityTypeConfiguration<QRCode>
{
    public void Configure(EntityTypeBuilder<QRCode> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(100);

        // Index unique cho Code để kiểm tra nhanh và tránh trùng
        builder.HasIndex(x => x.Code)
               .IsUnique();

        builder.Property(x => x.Type)
               .HasMaxLength(50);

        builder.Property(x => x.Description)
               .HasMaxLength(500);

        builder.Property(x => x.QrImageUrl)
               .HasMaxLength(500);
    }
}