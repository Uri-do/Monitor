using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAverageOfCurrHourToBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ConfigValue",
                schema: "monitoring",
                table: "Config",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "ConfigKey",
                schema: "monitoring",
                table: "Config",
                newName: "Key");

            migrationBuilder.AddColumn<long>(
                name: "IndicatorId",
                schema: "monitoring",
                table: "HistoricalData",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Indicators",
                schema: "monitoring",
                columns: table => new
                {
                    IndicatorId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndicatorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IndicatorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IndicatorDesc = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CollectorId = table.Column<int>(type: "int", nullable: false),
                    CollectorItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ScheduleConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    ThresholdType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThresholdField = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThresholdComparison = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "medium"),
                    OwnerContactId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastRun = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AverageHour = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AverageLastDays = table.Column<int>(type: "int", nullable: true),
                    AverageOfCurrHour = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsCurrentlyRunning = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExecutionStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionContext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.IndicatorId);
                    table.ForeignKey(
                        name: "FK_Indicators_Contacts_OwnerContactId",
                        column: x => x.OwnerContactId,
                        principalSchema: "monitoring",
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorContacts",
                schema: "monitoring",
                columns: table => new
                {
                    IndicatorContactID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndicatorID = table.Column<long>(type: "bigint", nullable: false),
                    ContactID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorContacts", x => x.IndicatorContactID);
                    table.ForeignKey(
                        name: "FK_IndicatorContacts_Contacts_ContactID",
                        column: x => x.ContactID,
                        principalSchema: "monitoring",
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndicatorContacts_Indicators_IndicatorID",
                        column: x => x.IndicatorID,
                        principalSchema: "monitoring",
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "success_rate",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2347), new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2347) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "threshold",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2354), new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2354) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "transaction_volume",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2351), new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2351) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "trend_analysis",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2356), new DateTime(2025, 6, 12, 12, 58, 26, 432, DateTimeKind.Utc).AddTicks(2357) });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalData_IndicatorId",
                schema: "monitoring",
                table: "HistoricalData",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorContacts_ContactId",
                schema: "monitoring",
                table: "IndicatorContacts",
                column: "ContactID");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorContacts_IndicatorId",
                schema: "monitoring",
                table: "IndicatorContacts",
                column: "IndicatorID");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorContacts_IsActive",
                schema: "monitoring",
                table: "IndicatorContacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UQ_IndicatorContacts_IndicatorId_ContactId",
                schema: "monitoring",
                table: "IndicatorContacts",
                columns: new[] { "IndicatorID", "ContactID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Collector_Item",
                schema: "monitoring",
                table: "Indicators",
                columns: new[] { "CollectorId", "CollectorItemName" });

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_CollectorId",
                schema: "monitoring",
                table: "Indicators",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_IndicatorCode",
                schema: "monitoring",
                table: "Indicators",
                column: "IndicatorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_IndicatorName",
                schema: "monitoring",
                table: "Indicators",
                column: "IndicatorName");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_IsActive",
                schema: "monitoring",
                table: "Indicators",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_LastRun",
                schema: "monitoring",
                table: "Indicators",
                column: "LastRun");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_OwnerContactId",
                schema: "monitoring",
                table: "Indicators",
                column: "OwnerContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Priority",
                schema: "monitoring",
                table: "Indicators",
                column: "Priority");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertLogs_Indicators_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs",
                column: "IndicatorId",
                principalSchema: "monitoring",
                principalTable: "Indicators",
                principalColumn: "IndicatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricalData_Indicators_IndicatorId",
                schema: "monitoring",
                table: "HistoricalData",
                column: "IndicatorId",
                principalSchema: "monitoring",
                principalTable: "Indicators",
                principalColumn: "IndicatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertLogs_Indicators_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricalData_Indicators_IndicatorId",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropTable(
                name: "IndicatorContacts",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "Indicators",
                schema: "monitoring");

            migrationBuilder.DropIndex(
                name: "IX_HistoricalData_IndicatorId",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropIndex(
                name: "IX_AlertLogs_IndicatorId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.DropColumn(
                name: "IndicatorId",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "IndicatorId",
                schema: "monitoring",
                table: "AlertLogs");

            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "monitoring",
                table: "Config",
                newName: "ConfigValue");

            migrationBuilder.RenameColumn(
                name: "Key",
                schema: "monitoring",
                table: "Config",
                newName: "ConfigKey");

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "success_rate",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6820), new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6821) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "threshold",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6826), new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6826) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "transaction_volume",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6823), new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6824) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "trend_analysis",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6828), new DateTime(2025, 6, 10, 0, 11, 14, 484, DateTimeKind.Utc).AddTicks(6829) });
        }
    }
}
