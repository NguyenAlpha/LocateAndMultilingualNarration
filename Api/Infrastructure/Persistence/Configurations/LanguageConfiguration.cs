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
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasData(
                new Language
                {
                    Id = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                    Name = "English",
                    Code = "en",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                    Name = "Vietnamese",
                    Code = "vi",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                    Name = "Japanese",
                    Code = "ja",
                    IsActive = true
                }
            );
        }
    }
}
