using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "monitoring");

            migrationBuilder.CreateTable(
                name: "Config",
                schema: "monitoring",
                columns: table => new
                {
                    ConfigKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config", x => x.ConfigKey);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                schema: "monitoring",
                columns: table => new
                {
                    ContactId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.ContactId);
                });

            migrationBuilder.CreateTable(
                name: "KPIs",
                schema: "monitoring",
                columns: table => new
                {
                    KpiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Indicator = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Deviation = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SpName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastRun = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CooldownMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    MinimumThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIs", x => x.KpiId);
                    table.CheckConstraint("CK_KPIs_Priority", "Priority IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "SystemStatus",
                schema: "monitoring",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedKpis = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AlertsSent = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemStatus", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "AlertLogs",
                schema: "monitoring",
                columns: table => new
                {
                    AlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KpiId = table.Column<int>(type: "int", nullable: false),
                    TriggerTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentVia = table.Column<byte>(type: "tinyint", nullable: false),
                    SentTo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HistoricalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeviationPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolvedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertLogs", x => x.AlertId);
                    table.CheckConstraint("CK_AlertLogs_SentVia", "SentVia IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_AlertLogs_KPIs_KpiId",
                        column: x => x.KpiId,
                        principalSchema: "monitoring",
                        principalTable: "KPIs",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalData",
                schema: "monitoring",
                columns: table => new
                {
                    HistoricalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KpiId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Period = table.Column<int>(type: "int", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalData", x => x.HistoricalId);
                    table.ForeignKey(
                        name: "FK_HistoricalData_KPIs_KpiId",
                        column: x => x.KpiId,
                        principalSchema: "monitoring",
                        principalTable: "KPIs",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiContacts",
                schema: "monitoring",
                columns: table => new
                {
                    KpiId = table.Column<int>(type: "int", nullable: false),
                    ContactId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiContacts", x => new { x.KpiId, x.ContactId });
                    table.ForeignKey(
                        name: "FK_KpiContacts_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "monitoring",
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KpiContacts_KPIs_KpiId",
                        column: x => x.KpiId,
                        principalSchema: "monitoring",
                        principalTable: "KPIs",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_IsResolved",
                schema: "monitoring",
                table: "AlertLogs",
                column: "IsResolved");

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

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_TriggerTime",
                schema: "monitoring",
                table: "AlertLogs",
                column: "TriggerTime");

            migrationBuilder.CreateIndex(
                name: "IX_Config_ModifiedDate",
                schema: "monitoring",
                table: "Config",
                column: "ModifiedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Email",
                schema: "monitoring",
                table: "Contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_IsActive",
                schema: "monitoring",
                table: "Contacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Name",
                schema: "monitoring",
                table: "Contacts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalData_KpiId",
                schema: "monitoring",
                table: "HistoricalData",
                column: "KpiId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalData_KpiId_MetricKey_Period",
                schema: "monitoring",
                table: "HistoricalData",
                columns: new[] { "KpiId", "MetricKey", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalData_KpiId_Timestamp",
                schema: "monitoring",
                table: "HistoricalData",
                columns: new[] { "KpiId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalData_Timestamp",
                schema: "monitoring",
                table: "HistoricalData",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_KpiContacts_ContactId",
                schema: "monitoring",
                table: "KpiContacts",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_Indicator",
                schema: "monitoring",
                table: "KPIs",
                column: "Indicator",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_IsActive",
                schema: "monitoring",
                table: "KPIs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_LastRun",
                schema: "monitoring",
                table: "KPIs",
                column: "LastRun");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_Owner",
                schema: "monitoring",
                table: "KPIs",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_SystemStatus_LastHeartbeat",
                schema: "monitoring",
                table: "SystemStatus",
                column: "LastHeartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_SystemStatus_ServiceName",
                schema: "monitoring",
                table: "SystemStatus",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemStatus_Status",
                schema: "monitoring",
                table: "SystemStatus",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertLogs",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "Config",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "HistoricalData",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "KpiContacts",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "SystemStatus",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "Contacts",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "KPIs",
                schema: "monitoring");
        }
    }
}
