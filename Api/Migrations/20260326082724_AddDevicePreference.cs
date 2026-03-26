using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDevicePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevicePreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Voice = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SpeechRate = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 1.0m),
                    AutoPlay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DeviceModel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OsVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevicePreferences_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePreferences_DeviceId",
                table: "DevicePreferences",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePreferences_LanguageId",
                table: "DevicePreferences",
                column: "LanguageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevicePreferences");
        }
    }
}
