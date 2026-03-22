using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations
{
    public class TtsVoiceProfileConfiguration : IEntityTypeConfiguration<TtsVoiceProfile>
    {
        public void Configure(EntityTypeBuilder<TtsVoiceProfile> b)
        {
            b.ToTable("TtsVoiceProfiles");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            b.Property(x => x.DisplayName)
             .HasMaxLength(128)
             .IsRequired();

            b.Property(x => x.Description)
             .HasMaxLength(256);

            b.Property(x => x.VoiceName)
             .HasMaxLength(128);

            b.Property(x => x.Style)
             .HasMaxLength(64);

            b.Property(x => x.Role)
             .HasMaxLength(64);

            b.Property(x => x.Provider)
             .HasMaxLength(64);

            b.Property(x => x.IsDefault)
             .HasDefaultValue(false);

            b.Property(x => x.IsActive)
             .HasDefaultValue(true);

            b.Property(x => x.Priority)
             .HasDefaultValue(0);

            b.HasOne(x => x.Language)
             .WithMany(l => l.TtsVoiceProfiles)
             .HasForeignKey(x => x.LanguageId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.NarrationAudios)
             .WithOne(a => a.TtsVoiceProfile)
             .HasForeignKey(a => a.TtsVoiceProfileId)
             .OnDelete(DeleteBehavior.Restrict);

             b.HasData(
                 new TtsVoiceProfile
                 {
                     Id = new Guid("11111111-1111-1111-1111-111111111111"),
                     LanguageId = new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                     DisplayName = "Nam Minh",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "vi-VN-NamMinhNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 0
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-2222-2222-2222-222222222222"),
                     LanguageId = new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                     DisplayName = "Hoài My",
                     Description = "Giọng nữ, rõ ràng",
                     VoiceName = "vi-VN-HoaiMyNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-3333-2222-2222-222222222222"),
                     LanguageId = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                     DisplayName = "Ava",
                     Description = "Giọng nữ, thân thiện",
                     VoiceName = "en-US-AvaMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-1234-2222-2222-222222222222"),
                     LanguageId = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                     DisplayName = "Andrew",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "en-US-AndrewMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0001-2222-2222-222222222222"),
                     LanguageId = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                     DisplayName = "Amanda",
                     Description = "Giọng nữ, rõ ràng",
                     VoiceName = "en-US-AmandaMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0002-2222-2222-222222222222"),
                     LanguageId = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                     DisplayName = "Adam",
                     Description = "Giọng nam, ấm áp",
                     VoiceName = "en-US-AdamMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0003-2222-2222-222222222222"),
                     LanguageId = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                     DisplayName = "Emma",
                     Description = "Giọng nữ, vui vẻ",
                     VoiceName = "en-US-EmmaMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0004-2222-2222-222222222222"),
                     LanguageId = new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                     DisplayName = "Phoebe",
                     Description = "Giọng nữ, trẻ trung",
                     VoiceName = "en-US-PhoebeMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0005-2222-2222-222222222222"),
                     LanguageId = new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                     DisplayName = "Nanami",
                     Description = "Giọng nữ, vui vẻ",
                     VoiceName = "ja-JP-NanamiNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0006-2222-2222-222222222222"),
                     LanguageId = new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                     DisplayName = "Keita",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "ja-JP-KeitaNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0007-2222-2222-222222222222"),
                     LanguageId = new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                     DisplayName = "Xiaochen",
                     Description = "Giọng nữ, thân thiện",
                     VoiceName = "zh-CN-XiaochenMultilingualNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0008-2222-2222-222222222222"),
                     LanguageId = new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                     DisplayName = "Yunxi",
                     Description = "Giọng nam, vui vẻ",
                     VoiceName = "zh-CN-YunxiNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0009-2222-2222-222222222222"),
                     LanguageId = new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                     DisplayName = "Xiaoxiao",
                     Description = "Giọng nữ, ám áp",
                     VoiceName = "zh-CN-XiaoxiaoNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0010-2222-2222-222222222222"),
                     LanguageId = new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                     DisplayName = "Yunjian",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "zh-CN-YunjianNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0011-2222-2222-222222222222"),
                     LanguageId = new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"),
                     DisplayName = "Denise",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "fr-FR-DeniseNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0012-2222-2222-222222222222"),
                     LanguageId = new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"),
                     DisplayName = "Henri",
                     Description = "Giọng nam, mạnh mẽ",
                     VoiceName = "fr-FR-HenriNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0013-2222-2222-222222222222"),
                     LanguageId = new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"),
                     DisplayName = "Svetlana",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "ru-RU-SvetlanaNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0014-2222-2222-222222222222"),
                     LanguageId = new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"),
                     DisplayName = "Dmitry",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "ru-RU-DmitryNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0015-2222-2222-222222222222"),
                     LanguageId = new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"),
                     DisplayName = "Katja",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "de-DE-KatjaNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0016-2222-2222-222222222222"),
                     LanguageId = new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"),
                     DisplayName = "Conrad",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "de-DE-ConradNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0017-2222-2222-222222222222"),
                     LanguageId = new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"),
                     DisplayName = "Sun-Hi",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "ko-KR-SunHiNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0018-2222-2222-222222222222"),
                     LanguageId = new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"),
                     DisplayName = "InJoon",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "ko-KR-InJoonNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0019-2222-2222-222222222222"),
                     LanguageId = new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"),
                     DisplayName = "Elvira",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "es-ES-ElviraNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0020-2222-2222-222222222222"),
                     LanguageId = new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"),
                     DisplayName = "Alvaro",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "es-ES-AlvaroNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0021-2222-2222-222222222222"),
                     LanguageId = new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"),
                     DisplayName = "Elsa",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "it-IT-ElsaNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0022-2222-2222-222222222222"),
                     LanguageId = new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"),
                     DisplayName = "Diego",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "it-IT-DiegoNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0023-2222-2222-222222222222"),
                     LanguageId = new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"),
                     DisplayName = "Premwadee",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "th-TH-PremwadeeNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0024-2222-2222-222222222222"),
                     LanguageId = new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"),
                     DisplayName = "Niwat",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "th-TH-NiwatNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0025-2222-2222-222222222222"),
                     LanguageId = new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"),
                     DisplayName = "Gadis",
                     Description = "Giọng nữ, trung tính",
                     VoiceName = "id-ID-GadisNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = true,
                     IsActive = true,
                     Priority = 1
                 },
                 new TtsVoiceProfile
                 {
                     Id = new Guid("22222222-0026-2222-2222-222222222222"),
                     LanguageId = new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"),
                     DisplayName = "Ardi",
                     Description = "Giọng nam, trung tính",
                     VoiceName = "id-ID-ArdiNeural",
                     Style = null,
                     Role = null,
                     Provider = "AzureTts",
                     IsDefault = false,
                     IsActive = true,
                     Priority = 1
                 }
             );
        }
    }
}
