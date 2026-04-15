using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTtsStatusToNarrationContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TtsError",
                table: "StallNarrationContents",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsStatus",
                table: "StallNarrationContents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateIndex(
                name: "IX_StallNarrationContents_TtsStatus",
                table: "StallNarrationContents",
                column: "TtsStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StallNarrationContents_TtsStatus",
                table: "StallNarrationContents");

            migrationBuilder.DropColumn(
                name: "TtsError",
                table: "StallNarrationContents");

            migrationBuilder.DropColumn(
                name: "TtsStatus",
                table: "StallNarrationContents");
        }
    }
}
