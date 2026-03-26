using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageDisplayNameAndFlagCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Languages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagCode",
                table: "Languages",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("06f82b17-b175-4fcf-cf56-c1de1f402c65"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Español", "es" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "English", "us" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("17a93c28-c286-40d0-d067-d2ef20513d76"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Italiano", "it" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("28ba4d39-d397-41e1-e178-e3f031624e87"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "ภาษาไทย", "th" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Tiếng Việt", "vn" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("39cb5e4a-e4a8-42f2-f289-f40142735f98"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Bahasa Indonesia", "id" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "日本語", "jp" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "中文", "cn" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Français", "fr" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Русский", "ru" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("e4d609f5-9f53-4dad-ad34-afbc9d2e0a43"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "Deutsch", "de" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("f5e71a06-a064-4ebe-be45-b0cd0e3f1b54"),
                columns: new[] { "DisplayName", "FlagCode" },
                values: new object[] { "한국어", "kr" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "FlagCode",
                table: "Languages");
        }
    }
}
