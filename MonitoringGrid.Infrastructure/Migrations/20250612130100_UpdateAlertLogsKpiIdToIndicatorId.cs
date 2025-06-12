using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAlertLogsKpiIdToIndicatorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign key constraints and indexes
            migrationBuilder.DropForeignKey(
                name: "FK_AlertLogs_KPIs_KpiId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropIndex(
                name: "IX_AlertLogs_KpiId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropIndex(
                name: "IX_AlertLogs_KpiId_TriggerTime",
                schema: "monitoring",
                table: "AlertLogs");

            // Rename the column from KpiId to IndicatorId and change type from int to bigint
            migrationBuilder.RenameColumn(
                name: "KpiId",
                schema: "monitoring",
                table: "AlertLogs",
                newName: "IndicatorId");

            // Change the column type from int to bigint to match Indicator.IndicatorID
            migrationBuilder.AlterColumn<long>(
                name: "IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // Create new indexes with correct column name
            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_IndicatorId_TriggerTime",
                schema: "monitoring",
                table: "AlertLogs",
                columns: new[] { "IndicatorId", "TriggerTime" });

            // Add foreign key constraint to Indicators table
            migrationBuilder.AddForeignKey(
                name: "FK_AlertLogs_Indicators_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                column: "IndicatorId",
                principalSchema: "monitoring",
                principalTable: "Indicators",
                principalColumn: "IndicatorID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new foreign key constraint and indexes
            migrationBuilder.DropForeignKey(
                name: "FK_AlertLogs_Indicators_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropIndex(
                name: "IX_AlertLogs_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropIndex(
                name: "IX_AlertLogs_IndicatorId_TriggerTime",
                schema: "monitoring",
                table: "AlertLogs");

            // Change the column type back from bigint to int
            migrationBuilder.AlterColumn<int>(
                name: "IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            // Rename the column back from IndicatorId to KpiId
            migrationBuilder.RenameColumn(
                name: "IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                newName: "KpiId");

            // Create old indexes
            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_KpiId",
                schema: "monitoring",
                table: "AlertLogs",
                column: "KpiId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_KpiId_TriggerTime",
                schema: "monitoring",
                table: "AlertLogs",
                columns: new[] { "KpiId", "TriggerTime" });

            // Add old foreign key constraint to KPIs table
            migrationBuilder.AddForeignKey(
                name: "FK_AlertLogs_KPIs_KpiId",
                schema: "monitoring",
                table: "AlertLogs",
                column: "KpiId",
                principalSchema: "monitoring",
                principalTable: "KPIs",
                principalColumn: "KpiId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
