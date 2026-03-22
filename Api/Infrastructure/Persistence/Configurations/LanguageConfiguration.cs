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
                    Code = "en-US",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                    Name = "Vietnamese",
                    Code = "vi-VN",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                    Name = "Japanese",
                    Code = "ja-JP",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                    Name = "Chinese",
                    Code = "zh-CN",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"),
                    Name = "French",
                    Code = "fr-FR",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"),
                    Name = "Russian",
                    Code = "ru-RU",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"),
                    Name = "German",
                    Code = "de-DE",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"),
                    Name = "Korean",
                    Code = "ko-KR",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"),
                    Name = "Spanish",
                    Code = "es-ES",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"),
                    Name = "Italian",
                    Code = "it-IT",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"),
                    Name = "Thai",
                    Code = "th-TH",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"),
                    Name = "Indonesian",
                    Code = "id-ID",
                    IsActive = true
                }
            );
        }
    }
}
