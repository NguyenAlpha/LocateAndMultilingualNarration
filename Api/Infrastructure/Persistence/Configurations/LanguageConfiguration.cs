using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class LanguageConfiguration : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> b)
        {
            b.ToTable("Languages");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.Name).HasMaxLength(64).IsRequired();
            b.Property(x => x.Code).HasMaxLength(16).IsRequired();
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.Code).IsUnique();
        }
    }
}
