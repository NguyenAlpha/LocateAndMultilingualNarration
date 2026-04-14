using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QRCode",
                table: "QRCode");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "QRCode");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "QRCode");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "QRCode");

            migrationBuilder.DropColumn(
                name: "ScanCount",
                table: "QRCode");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "QRCode");

            migrationBuilder.RenameTable(
                name: "QRCode",
                newName: "QrCode");

            migrationBuilder.RenameColumn(
                name: "QrImageUrl",
                table: "QrCode",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                table: "QrCode",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "GeneratedAt",
                table: "QrCode",
                newName: "ExpiryAt");

            migrationBuilder.RenameIndex(
                name: "IX_QRCode_Code",
                table: "QrCode",
                newName: "IX_QrCode_Code");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "QrCode",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UsedByDeviceId",
                table: "QrCode",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_QrCode",
                table: "QrCode",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QrCode",
                table: "QrCode");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "QrCode");

            migrationBuilder.DropColumn(
                name: "UsedByDeviceId",
                table: "QrCode");

            migrationBuilder.RenameTable(
                name: "QrCode",
                newName: "QRCode");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                table: "QRCode",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "QRCode",
                newName: "QrImageUrl");

            migrationBuilder.RenameColumn(
                name: "ExpiryAt",
                table: "QRCode",
                newName: "GeneratedAt");

            migrationBuilder.RenameIndex(
                name: "IX_QrCode_Code",
                table: "QRCode",
                newName: "IX_QRCode_Code");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "QRCode",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "QRCode",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "QRCode",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ScanCount",
                table: "QRCode",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "QRCode",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QRCode",
                table: "QRCode",
                column: "Id");
        }
    }
}
