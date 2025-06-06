using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingAlertLogColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns to AlertLogs table
            migrationBuilder.AddColumn<string>(
                name: "Subject",
                schema: "monitoring",
                table: "AlertLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "monitoring",
                table: "AlertLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                schema: "monitoring",
                table: "AlertLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the added columns
            migrationBuilder.DropColumn(
                name: "Subject",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                schema: "monitoring",
                table: "AlertLogs");
        }
    }
}
