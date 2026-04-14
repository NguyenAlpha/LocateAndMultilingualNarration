using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAndDeviceLocationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Businesses",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlanExpiresAt",
                table: "Businesses",
                type: "datetimeoffset(3)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceLocationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    AccuracyMeters = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLocationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "MockCard"),
                    CardLastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PlanStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    PlanEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionOrders_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLocationLogs_CapturedAtUtc",
                table: "DeviceLocationLogs",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLocationLogs_DeviceId",
                table: "DeviceLocationLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrders_BusinessId",
                table: "SubscriptionOrders",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceLocationLogs");

            migrationBuilder.DropTable(
                name: "SubscriptionOrders");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "PlanExpiresAt",
                table: "Businesses");
        }
    }
}
