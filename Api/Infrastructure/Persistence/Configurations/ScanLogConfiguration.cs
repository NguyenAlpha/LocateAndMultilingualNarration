using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class ScanLogConfiguration : IEntityTypeConfiguration<ScanLog>
    {
        public void Configure(EntityTypeBuilder<ScanLog> b)
        {
            b.ToTable("ScanLogs");

            b.HasKey(x => x.Id);

            // Khóa chính tăng tự động theo Identity để phù hợp dữ liệu Mobile SQLite.
            b.Property(x => x.Id)
             .UseIdentityColumn();

            b.Property(x => x.DeviceId)
             .IsRequired()
             .HasMaxLength(128);

            b.Property(x => x.QrRawResult)
             .IsRequired()
             .HasMaxLength(2048);

            b.Property(x => x.LastQrScanAt)
             .HasColumnType("datetime2(3)");

            b.Property(x => x.LastScannedSlug)
             .HasMaxLength(256);

            b.Property(x => x.QrSessionExpiry)
             .HasColumnType("datetime2(3)");

            b.Property(x => x.HasScannedQr)
             .HasDefaultValue(true);

            // Index composite tối ưu truy vấn theo thiết bị và thời điểm quét.
            b.HasIndex(x => new { x.DeviceId, x.LastQrScanAt })
             .HasDatabaseName("IX_ScanLogs_DeviceId_LastQrScanAt");
        }
    }
}
