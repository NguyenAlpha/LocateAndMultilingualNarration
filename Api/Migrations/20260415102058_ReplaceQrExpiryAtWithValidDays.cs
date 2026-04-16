using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceQrExpiryAtWithValidDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QrCodes_IsUsed_ExpiryAt",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ExpiryAt",
                table: "QrCodes");

            migrationBuilder.AddColumn<int>(
                name: "ValidDays",
                table: "QrCodes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_IsUsed",
                table: "QrCodes",
                column: "IsUsed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QrCodes_IsUsed",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ValidDays",
                table: "QrCodes");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryAt",
                table: "QrCodes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_IsUsed_ExpiryAt",
                table: "QrCodes",
                columns: new[] { "IsUsed", "ExpiryAt" });
        }
    }
}
