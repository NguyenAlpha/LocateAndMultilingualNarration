using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameDevicePreferenceVoiceToVoiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Voice",
                table: "DevicePreferences");

            migrationBuilder.AddColumn<Guid>(
                name: "VoiceId",
                table: "DevicePreferences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePreferences_VoiceId",
                table: "DevicePreferences",
                column: "VoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DevicePreferences_TtsVoiceProfiles_VoiceId",
                table: "DevicePreferences",
                column: "VoiceId",
                principalTable: "TtsVoiceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevicePreferences_TtsVoiceProfiles_VoiceId",
                table: "DevicePreferences");

            migrationBuilder.DropIndex(
                name: "IX_DevicePreferences_VoiceId",
                table: "DevicePreferences");

            migrationBuilder.DropColumn(
                name: "VoiceId",
                table: "DevicePreferences");

            migrationBuilder.AddColumn<string>(
                name: "Voice",
                table: "DevicePreferences",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
