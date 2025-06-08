using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MonitoringGrid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiExecutionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.AddColumn<string>(
                name: "ComparisonOperator",
                schema: "monitoring",
                table: "KPIs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionContext",
                schema: "monitoring",
                table: "KPIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionStartTime",
                schema: "monitoring",
                table: "KPIs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentlyRunning",
                schema: "monitoring",
                table: "KPIs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KpiType",
                schema: "monitoring",
                table: "KPIs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "success_rate");

            migrationBuilder.AddColumn<int>(
                name: "LastMinutes",
                schema: "monitoring",
                table: "KPIs",
                type: "int",
                nullable: false,
                defaultValue: 1440);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleConfiguration",
                schema: "monitoring",
                table: "KPIs",
                type: "NVARCHAR(MAX)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdValue",
                schema: "monitoring",
                table: "KPIs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlertSent",
                schema: "monitoring",
                table: "HistoricalData",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConnectionString",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseName",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeviationPercent",
                schema: "monitoring",
                table: "HistoricalData",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutedBy",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionContext",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionMethod",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutionTimeMs",
                schema: "monitoring",
                table: "HistoricalData",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HistoricalValue",
                schema: "monitoring",
                table: "HistoricalData",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                schema: "monitoring",
                table: "HistoricalData",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RawResponse",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerName",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldAlert",
                schema: "monitoring",
                table: "HistoricalData",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SqlCommand",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SqlParameters",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "monitoring",
                table: "HistoricalData",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "monitoring",
                table: "Config",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                schema: "monitoring",
                table: "Config",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsEncrypted",
                schema: "monitoring",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadOnly",
                schema: "monitoring",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "KpiTypes",
                schema: "monitoring",
                columns: table => new
                {
                    KpiTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequiredFields = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    DefaultStoredProcedure = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiTypes", x => x.KpiTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "auth",
                columns: table => new
                {
                    PermissionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSystemPermission = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "auth",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledJobs",
                schema: "monitoring",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KpiId = table.Column<int>(type: "int", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    JobGroup = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "KPI_JOBS"),
                    TriggerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TriggerGroup = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "KPI_TRIGGERS"),
                    CronExpression = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextFireTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousFireTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobs", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_ScheduledJobs_KPIs_KpiId",
                        column: x => x.KpiId,
                        principalSchema: "monitoring",
                        principalTable: "KPIs",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditEvents",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Resource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPasswordChange = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "auth",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "auth",
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "auth",
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPasswords",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasswords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPasswords_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "auth",
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "monitoring",
                table: "KpiTypes",
                columns: new[] { "KpiTypeId", "CreatedDate", "DefaultStoredProcedure", "Description", "IsActive", "ModifiedDate", "Name", "RequiredFields" },
                values: new object[,]
                {
                    { "success_rate", new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2750), "monitoring.usp_MonitorTransactions", "Monitors success percentages and compares them against historical averages. Ideal for tracking transaction success rates, API response rates, login success rates, and other percentage-based metrics.", true, new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2751), "Success Rate Monitoring", "[\"deviation\", \"lastMinutes\"]" },
                    { "threshold", new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2758), "monitoring.usp_MonitorThreshold", "Simple threshold-based monitoring that triggers alerts when values cross specified limits. Useful for monitoring system resources, queue lengths, error counts, response times, and other absolute value metrics.", true, new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2758), "Threshold Monitoring", "[\"thresholdValue\", \"comparisonOperator\"]" },
                    { "transaction_volume", new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2754), "monitoring.usp_MonitorTransactionVolume", "Tracks transaction counts and compares them to historical patterns. Perfect for detecting unusual spikes or drops in activity, monitoring daily transactions, API calls, user registrations, and other count-based metrics.", true, new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2755), "Transaction Volume Monitoring", "[\"deviation\", \"minimumThreshold\", \"lastMinutes\"]" },
                    { "trend_analysis", new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2761), "monitoring.usp_MonitorTrends", "Analyzes trends over time to detect gradual changes or patterns. Excellent for capacity planning, performance degradation detection, user behavior analysis, and early warning systems for emerging issues.", true, new DateTime(2025, 6, 8, 5, 20, 10, 397, DateTimeKind.Utc).AddTicks(2761), "Trend Analysis", "[\"deviation\", \"lastMinutes\"]" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_KpiType",
                schema: "monitoring",
                table: "KPIs",
                column: "KpiType")
                .Annotation("SqlServer:Include", new[] { "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_KPIs_ComparisonOperator",
                schema: "monitoring",
                table: "KPIs",
                sql: "ComparisonOperator IS NULL OR ComparisonOperator IN ('gt', 'gte', 'lt', 'lte', 'eq')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_KPIs_KpiType",
                schema: "monitoring",
                table: "KPIs",
                sql: "KpiType IN ('success_rate', 'transaction_volume', 'threshold', 'trend_analysis')");

            migrationBuilder.CreateIndex(
                name: "IX_KpiTypes_IsActive",
                schema: "monitoring",
                table: "KpiTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KpiTypes_Name",
                schema: "monitoring",
                table: "KpiTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsActive",
                schema: "auth",
                table: "Permissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                schema: "auth",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action",
                schema: "auth",
                table: "Permissions",
                columns: new[] { "Resource", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Active_Expires",
                schema: "auth",
                table: "RefreshTokens",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                schema: "auth",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "auth",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "auth",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                schema: "auth",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsActive",
                schema: "auth",
                table: "Roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                schema: "auth",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_JobName_JobGroup",
                schema: "monitoring",
                table: "ScheduledJobs",
                columns: new[] { "JobName", "JobGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_KpiId",
                schema: "monitoring",
                table: "ScheduledJobs",
                column: "KpiId")
                .Annotation("SqlServer:Include", new[] { "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_NextFireTime",
                schema: "monitoring",
                table: "ScheduledJobs",
                column: "NextFireTime",
                filter: "IsActive = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_TriggerName_TriggerGroup",
                schema: "monitoring",
                table: "ScheduledJobs",
                columns: new[] { "TriggerName", "TriggerGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_EventType",
                table: "SecurityAuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_EventType_Timestamp",
                table: "SecurityAuditEvents",
                columns: new[] { "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_Timestamp",
                table: "SecurityAuditEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_UserId",
                table: "SecurityAuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_UserId_Timestamp",
                table: "SecurityAuditEvents",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswords_UserId",
                schema: "auth",
                table: "UserPasswords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswords_UserId_Active",
                schema: "auth",
                table: "UserPasswords",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "auth",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                schema: "auth",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "auth",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                schema: "auth",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIs_KpiTypes_KpiType",
                schema: "monitoring",
                table: "KPIs",
                column: "KpiType",
                principalSchema: "monitoring",
                principalTable: "KpiTypes",
                principalColumn: "KpiTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KPIs_KpiTypes_KpiType",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropTable(
                name: "KpiTypes",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "ScheduledJobs",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "SecurityAuditEvents");

            migrationBuilder.DropTable(
                name: "UserPasswords",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_KPIs_KpiType",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KPIs_ComparisonOperator",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KPIs_KpiType",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "ComparisonOperator",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "ExecutionContext",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "ExecutionStartTime",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "IsCurrentlyRunning",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "KpiType",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "LastMinutes",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "ScheduleConfiguration",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "ThresholdValue",
                schema: "monitoring",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "AlertSent",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ConnectionString",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "DatabaseName",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "DeviationPercent",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ExecutedBy",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ExecutionContext",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ExecutionMethod",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ExecutionTimeMs",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "HistoricalValue",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "IsSuccessful",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "RawResponse",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ServerName",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "ShouldAlert",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "SqlCommand",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "SqlParameters",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "monitoring",
                table: "HistoricalData");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "monitoring",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                schema: "monitoring",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                schema: "monitoring",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "IsReadOnly",
                schema: "monitoring",
                table: "Config");
        }
    }
}
