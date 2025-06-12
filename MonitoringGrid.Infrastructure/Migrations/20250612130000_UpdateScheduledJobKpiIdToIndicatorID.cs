using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScheduledJobKpiIdToIndicatorID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledJobs_Indicators_KpiId",
                schema: "monitoring",
                table: "ScheduledJobs");

            // Drop the existing index
            migrationBuilder.DropIndex(
                name: "IX_ScheduledJobs_KpiId",
                schema: "monitoring",
                table: "ScheduledJobs");

            // Rename the column from KpiId to IndicatorID and change type from int to bigint
            migrationBuilder.RenameColumn(
                name: "KpiId",
                schema: "monitoring",
                table: "ScheduledJobs",
                newName: "IndicatorID");

            // Change the column type from int to bigint
            migrationBuilder.AlterColumn<long>(
                name: "IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // Create the new index with the correct name
            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs",
                column: "IndicatorID")
                .Annotation("SqlServer:Include", new[] { "IsActive" });

            // Add the foreign key constraint with the correct column name and type
            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledJobs_Indicators_IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs",
                column: "IndicatorID",
                principalSchema: "monitoring",
                principalTable: "Indicators",
                principalColumn: "IndicatorID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledJobs_Indicators_IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs");

            // Drop the new index
            migrationBuilder.DropIndex(
                name: "IX_ScheduledJobs_IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs");

            // Change the column type back from bigint to int
            migrationBuilder.AlterColumn<int>(
                name: "IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            // Rename the column back from IndicatorID to KpiId
            migrationBuilder.RenameColumn(
                name: "IndicatorID",
                schema: "monitoring",
                table: "ScheduledJobs",
                newName: "KpiId");

            // Create the old index
            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_KpiId",
                schema: "monitoring",
                table: "ScheduledJobs",
                column: "KpiId")
                .Annotation("SqlServer:Include", new[] { "IsActive" });

            // Add the old foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledJobs_Indicators_KpiId",
                schema: "monitoring",
                table: "ScheduledJobs",
                column: "KpiId",
                principalSchema: "monitoring",
                principalTable: "Indicators",
                principalColumn: "IndicatorID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
