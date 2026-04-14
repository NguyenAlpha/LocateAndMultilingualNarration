using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class SubscriptionOrderConfiguration : IEntityTypeConfiguration<SubscriptionOrder>
    {
        public void Configure(EntityTypeBuilder<SubscriptionOrder> b)
        {
            b.ToTable("SubscriptionOrders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Plan).HasMaxLength(16).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();
            b.Property(x => x.Amount).HasColumnType("decimal(18,0)");
            b.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("VND").IsRequired();
            b.Property(x => x.PaymentMethod).HasMaxLength(32).HasDefaultValue("MockCard").IsRequired();
            b.Property(x => x.CardLastFour).HasMaxLength(4);

            b.Property(x => x.CreatedAt)
             .HasColumnType("datetimeoffset(3)")
             .HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.PlanStartAt).HasColumnType("datetimeoffset(3)");
            b.Property(x => x.PlanEndAt).HasColumnType("datetimeoffset(3)");

            b.HasOne(x => x.Business)
             .WithMany()
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
