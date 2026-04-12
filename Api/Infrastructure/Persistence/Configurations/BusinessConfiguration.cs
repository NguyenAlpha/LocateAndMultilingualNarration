using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    // nói với EF Core rằng: “Mình đang viết cấu hình cho entity Business”
    // EF sẽ gọi Configure(...) khi build mô hình (model) lúc khởi tạo DbContext
    public class BusinessConfiguration : IEntityTypeConfiguration<Business>
    {
        public void Configure(EntityTypeBuilder<Business> b)
        {
            // Chỉ định tên bảng trong CSDL
            b.ToTable("Businesses");

            // Chỉ định các thuộc tính của entity
            b.HasKey(x => x.Id); // Khóa chính là Id
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()"); // Tự động sinh Id mới

            b.Property(x => x.Name).HasMaxLength(256).IsRequired(); // Tên doanh nghiệp là bắt buộc và có độ dài tối đa 256 ký tự
            b.Property(x => x.TaxCode).HasMaxLength(32); // Mã số thuế có độ dài tối đa 32 ký tự, có thể null
            b.Property(x => x.ContactEmail).HasMaxLength(256); // Email liên hệ có độ dài tối đa 256 ký tự, có thể null
            b.Property(x => x.ContactPhone).HasMaxLength(32); // Số điện thoại liên hệ có độ dài tối đa 32 ký tự, có thể null

            b.Property(x => x.CreatedAt) // Ngày tạo sẽ được tự động gán giá trị UTC hiện tại
             .HasColumnType("datetimeoffset(3)") // Sử dụng kiểu dữ liệu datetimeoffset với độ chính xác 3 chữ số
             .HasDefaultValueSql("SYSUTCDATETIME()"); // Tự động gán giá trị UTC hiện tại
            b.Property(x => x.IsActive).HasDefaultValue(true); // Mặc định IsActive là true

            b.Property(x => x.Plan).HasMaxLength(16).HasDefaultValue("Free").IsRequired();
            b.Property(x => x.PlanExpiresAt).HasColumnType("datetimeoffset(3)");

            b.HasOne(x => x.OwnerUser) // Thiết lập quan hệ với User
             .WithMany(u => u.Businesses) // Một User có thể sở hữu nhiều Business
             .HasForeignKey(x => x.OwnerUserId) // Khóa ngoại OwnerUserId trong Business
             .OnDelete(DeleteBehavior.SetNull); // Cho phép chuyển giao/chưa gán
        }
    }
}
