using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityThreatAndUserTwoFactorSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlacklistedTokens",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlacklistedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlacklistedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityThreats",
                schema: "auth",
                columns: table => new
                {
                    ThreatId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThreatType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThreatData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityThreats", x => x.ThreatId);
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactorSettings",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RecoveryCodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnabledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactorSettings", x => x.UserId);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_BlacklistedAt",
                schema: "auth",
                table: "BlacklistedTokens",
                column: "BlacklistedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_Expires_Blacklisted",
                schema: "auth",
                table: "BlacklistedTokens",
                columns: new[] { "ExpiresAt", "BlacklistedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_ExpiresAt",
                schema: "auth",
                table: "BlacklistedTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_TokenHash",
                schema: "auth",
                table: "BlacklistedTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityThreats_DetectedAt",
                schema: "auth",
                table: "SecurityThreats",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityThreats_IpAddress",
                schema: "auth",
                table: "SecurityThreats",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityThreats_IsResolved",
                schema: "auth",
                table: "SecurityThreats",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityThreats_Severity",
                schema: "auth",
                table: "SecurityThreats",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityThreats_ThreatType",
                schema: "auth",
                table: "SecurityThreats",
                column: "ThreatType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityThreats_UserId",
                schema: "auth",
                table: "SecurityThreats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorSettings_EnabledAt",
                schema: "auth",
                table: "UserTwoFactorSettings",
                column: "EnabledAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorSettings_IsEnabled",
                schema: "auth",
                table: "UserTwoFactorSettings",
                column: "IsEnabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistedTokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "SecurityThreats",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "UserTwoFactorSettings",
                schema: "auth");

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "success_rate",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2750), new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2751) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "threshold",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2758), new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2758) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "transaction_volume",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2754), new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2755) });

            migrationBuilder.UpdateData(
                schema: "monitoring",
                table: "KpiTypes",
                keyColumn: "KpiTypeId",
                keyValue: "trend_analysis",
                columns: new[] { "CreatedDate", "ModifiedDate" },
                values: new object[] { new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2761), new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2761) });
        }
    }
}
