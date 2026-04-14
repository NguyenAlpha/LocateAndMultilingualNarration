using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScanLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QRCode",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScanCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QrImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    QrRawResult = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    LastQrScanAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastScannedStallId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastScannedSlug = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    QrSessionExpiry = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    HasScannedQr = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QRCode_Code",
                table: "QRCode",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanLogs_DeviceId_LastQrScanAt",
                table: "ScanLogs",
                columns: new[] { "DeviceId", "LastQrScanAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QRCode");

            migrationBuilder.DropTable(
                name: "ScanLogs");
        }
    }
}
