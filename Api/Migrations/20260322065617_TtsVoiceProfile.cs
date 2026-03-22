using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class TtsVoiceProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NarrationAudios_NarrationContentId",
                table: "NarrationAudios");

            migrationBuilder.AddColumn<Guid>(
                name: "TtsVoiceProfileId",
                table: "NarrationAudios",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TtsVoiceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    VoiceName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Style = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TtsVoiceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TtsVoiceProfiles_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                column: "Code",
                value: "en-US");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                column: "Code",
                value: "vi-VN");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                column: "Code",
                value: "ja-JP");

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"), "es-ES", true, "Spanish" },
                    { new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"), "it-IT", true, "Italian" },
                    { new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"), "th-TH", true, "Thai" },
                    { new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"), "id-ID", true, "Indonesian" },
                    { new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"), "zh-CN", true, "Chinese" },
                    { new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"), "fr-FR", true, "French" },
                    { new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"), "ru-RU", true, "Russian" },
                    { new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"), "de-DE", true, "German" },
                    { new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"), "ko-KR", true, "Korean" }
                });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Giọng nam, trung tính", "Nam Minh", true, true, new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"), "AzureTts", null, null, "vi-VN-NamMinhNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[,]
                {
                    { new Guid("22222222-0001-2222-2222-222222222222"), "Giọng nữ, rõ ràng", "Amanda", true, new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"), 1, "AzureTts", null, null, "en-US-AmandaMultilingualNeural" },
                    { new Guid("22222222-0002-2222-2222-222222222222"), "Giọng nam, ấm áp", "Adam", true, new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"), 1, "AzureTts", null, null, "en-US-AdamMultilingualNeural" },
                    { new Guid("22222222-0003-2222-2222-222222222222"), "Giọng nữ, vui vẻ", "Emma", true, new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"), 1, "AzureTts", null, null, "en-US-EmmaMultilingualNeural" },
                    { new Guid("22222222-0004-2222-2222-222222222222"), "Giọng nữ, trẻ trung", "Phoebe", true, new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"), 1, "AzureTts", null, null, "en-US-PhoebeMultilingualNeural" }
                });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0005-2222-2222-222222222222"), "Giọng nữ, vui vẻ", "Nanami", true, true, new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"), 1, "AzureTts", null, null, "ja-JP-NanamiNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[,]
                {
                    { new Guid("22222222-0006-2222-2222-222222222222"), "Giọng nam, trung tính", "Keita", true, new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"), 1, "AzureTts", null, null, "ja-JP-KeitaNeural" },
                    { new Guid("22222222-1234-2222-2222-222222222222"), "Giọng nam, trung tính", "Andrew", true, new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"), 1, "AzureTts", null, null, "en-US-AndrewMultilingualNeural" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Giọng nữ, rõ ràng", "Hoài My", true, new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"), 1, "AzureTts", null, null, "vi-VN-HoaiMyNeural" }
                });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-3333-2222-2222-222222222222"), "Giọng nữ, thân thiện", "Ava", true, true, new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"), 1, "AzureTts", null, null, "en-US-AvaMultilingualNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[,]
                {
                    { new Guid("22222222-0007-2222-2222-222222222222"), "Giọng nữ, thân thiện", "Xiaochen", true, new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"), 1, "AzureTts", null, null, "zh-CN-XiaochenMultilingualNeural" },
                    { new Guid("22222222-0008-2222-2222-222222222222"), "Giọng nam, vui vẻ", "Yunxi", true, new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"), 1, "AzureTts", null, null, "zh-CN-YunxiNeural" }
                });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0009-2222-2222-222222222222"), "Giọng nữ, ám áp", "Xiaoxiao", true, true, new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"), 1, "AzureTts", null, null, "zh-CN-XiaoxiaoNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0010-2222-2222-222222222222"), "Giọng nam, trung tính", "Yunjian", true, new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"), 1, "AzureTts", null, null, "zh-CN-YunjianNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0011-2222-2222-222222222222"), "Giọng nữ, trung tính", "Denise", true, true, new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"), 1, "AzureTts", null, null, "fr-FR-DeniseNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0012-2222-2222-222222222222"), "Giọng nam, mạnh mẽ", "Henri", true, new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"), 1, "AzureTts", null, null, "fr-FR-HenriNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0013-2222-2222-222222222222"), "Giọng nữ, trung tính", "Svetlana", true, true, new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"), 1, "AzureTts", null, null, "ru-RU-SvetlanaNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0014-2222-2222-222222222222"), "Giọng nam, trung tính", "Dmitry", true, new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"), 1, "AzureTts", null, null, "ru-RU-DmitryNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0015-2222-2222-222222222222"), "Giọng nữ, trung tính", "Katja", true, true, new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"), 1, "AzureTts", null, null, "de-DE-KatjaNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0016-2222-2222-222222222222"), "Giọng nam, trung tính", "Conrad", true, new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"), 1, "AzureTts", null, null, "de-DE-ConradNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0017-2222-2222-222222222222"), "Giọng nữ, trung tính", "Sun-Hi", true, true, new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"), 1, "AzureTts", null, null, "ko-KR-SunHiNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0018-2222-2222-222222222222"), "Giọng nam, trung tính", "InJoon", true, new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"), 1, "AzureTts", null, null, "ko-KR-InJoonNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0019-2222-2222-222222222222"), "Giọng nữ, trung tính", "Elvira", true, true, new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"), 1, "AzureTts", null, null, "es-ES-ElviraNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0020-2222-2222-222222222222"), "Giọng nam, trung tính", "Alvaro", true, new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"), 1, "AzureTts", null, null, "es-ES-AlvaroNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0021-2222-2222-222222222222"), "Giọng nữ, trung tính", "Elsa", true, true, new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"), 1, "AzureTts", null, null, "it-IT-ElsaNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0022-2222-2222-222222222222"), "Giọng nam, trung tính", "Diego", true, new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"), 1, "AzureTts", null, null, "it-IT-DiegoNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0023-2222-2222-222222222222"), "Giọng nữ, trung tính", "Premwadee", true, true, new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"), 1, "AzureTts", null, null, "th-TH-PremwadeeNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0024-2222-2222-222222222222"), "Giọng nam, trung tính", "Niwat", true, new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"), 1, "AzureTts", null, null, "th-TH-NiwatNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "IsDefault", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0025-2222-2222-222222222222"), "Giọng nữ, trung tính", "Gadis", true, true, new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"), 1, "AzureTts", null, null, "id-ID-GadisNeural" });

            migrationBuilder.InsertData(
                table: "TtsVoiceProfiles",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "LanguageId", "Priority", "Provider", "Role", "Style", "VoiceName" },
                values: new object[] { new Guid("22222222-0026-2222-2222-222222222222"), "Giọng nam, trung tính", "Ardi", true, new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"), 1, "AzureTts", null, null, "id-ID-ArdiNeural" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrationAudios_NarrationContentId_TtsVoiceProfileId",
                table: "NarrationAudios",
                columns: new[] { "NarrationContentId", "TtsVoiceProfileId" },
                unique: true,
                filter: "[TtsVoiceProfileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NarrationAudios_TtsVoiceProfileId",
                table: "NarrationAudios",
                column: "TtsVoiceProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TtsVoiceProfiles_LanguageId",
                table: "TtsVoiceProfiles",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_NarrationAudios_TtsVoiceProfiles_TtsVoiceProfileId",
                table: "NarrationAudios",
                column: "TtsVoiceProfileId",
                principalTable: "TtsVoiceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NarrationAudios_TtsVoiceProfiles_TtsVoiceProfileId",
                table: "NarrationAudios");

            migrationBuilder.DropTable(
                name: "TtsVoiceProfiles");

            migrationBuilder.DropIndex(
                name: "IX_NarrationAudios_NarrationContentId_TtsVoiceProfileId",
                table: "NarrationAudios");

            migrationBuilder.DropIndex(
                name: "IX_NarrationAudios_TtsVoiceProfileId",
                table: "NarrationAudios");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"));

            migrationBuilder.DropColumn(
                name: "TtsVoiceProfileId",
                table: "NarrationAudios");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                column: "Code",
                value: "en");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                column: "Code",
                value: "vi");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                column: "Code",
                value: "ja");

            migrationBuilder.CreateIndex(
                name: "IX_NarrationAudios_NarrationContentId",
                table: "NarrationAudios",
                column: "NarrationContentId");
        }
    }
}
