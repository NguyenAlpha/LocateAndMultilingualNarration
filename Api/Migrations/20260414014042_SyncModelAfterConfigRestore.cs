using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterConfigRestore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QrCode",
                table: "QrCode");

            migrationBuilder.RenameTable(
                name: "QrCode",
                newName: "QrCodes");

            migrationBuilder.RenameIndex(
                name: "IX_QrCode_Code",
                table: "QrCodes",
                newName: "IX_QrCodes_Code");

            migrationBuilder.AlterColumn<bool>(
                name: "IsUsed",
                table: "QrCodes",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "QrCodes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QrCodes",
                table: "QrCodes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_IsUsed_ExpiryAt",
                table: "QrCodes",
                columns: new[] { "IsUsed", "ExpiryAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QrCodes",
                table: "QrCodes");

            migrationBuilder.DropIndex(
                name: "IX_QrCodes_IsUsed_ExpiryAt",
                table: "QrCodes");

            migrationBuilder.RenameTable(
                name: "QrCodes",
                newName: "QrCode");

            migrationBuilder.RenameIndex(
                name: "IX_QrCodes_Code",
                table: "QrCode",
                newName: "IX_QrCode_Code");

            migrationBuilder.AlterColumn<bool>(
                name: "IsUsed",
                table: "QrCode",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "QrCode",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QrCode",
                table: "QrCode",
                column: "Id");
        }
    }
}
