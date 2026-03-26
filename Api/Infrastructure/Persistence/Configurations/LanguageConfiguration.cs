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
            b.Property(x => x.DisplayName).HasMaxLength(64);
            b.Property(x => x.Code).HasMaxLength(16).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.FlagCode).HasMaxLength(8);

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasData(
                new Language
                {
                    Id = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                    Name = "English",
                    DisplayName = "English",
                    Code = "en-US",
                    FlagCode = "us",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                    Name = "Vietnamese",
                    DisplayName = "Tiếng Việt",
                    Code = "vi-VN",
                    FlagCode = "vn",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                    Name = "Japanese",
                    DisplayName = "日本語",
                    Code = "ja-JP",
                    FlagCode = "jp",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                    Name = "Chinese",
                    DisplayName = "中文",
                    Code = "zh-CN",
                    FlagCode = "cn",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"),
                    Name = "French",
                    DisplayName = "Français",
                    Code = "fr-FR",
                    FlagCode = "fr",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"),
                    Name = "Russian",
                    DisplayName = "Русский",
                    Code = "ru-RU",
                    FlagCode = "ru",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"),
                    Name = "German",
                    DisplayName = "Deutsch",
                    Code = "de-DE",
                    FlagCode = "de",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"),
                    Name = "Korean",
                    DisplayName = "한국어",
                    Code = "ko-KR",
                    FlagCode = "kr",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"),
                    Name = "Spanish",
                    DisplayName = "Español",
                    Code = "es-ES",
                    FlagCode = "es",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"),
                    Name = "Italian",
                    DisplayName = "Italiano",
                    Code = "it-IT",
                    FlagCode = "it",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"),
                    Name = "Thai",
                    DisplayName = "ภาษาไทย",
                    Code = "th-TH",
                    FlagCode = "th",
                    IsActive = true
                },
                new Language
                {
                    Id = new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"),
                    Name = "Indonesian",
                    DisplayName = "Bahasa Indonesia",
                    Code = "id-ID",
                    FlagCode = "id",
                    IsActive = true
                }
            );
        }
    }
}
