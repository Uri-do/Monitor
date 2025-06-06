using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MonitoringGrid.Api.DTOs;
using System.Data;

namespace MonitoringGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExecutionHistoryController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExecutionHistoryController> _logger;

    public ExecutionHistoryController(IConfiguration configuration, ILogger<ExecutionHistoryController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedExecutionHistoryDto>> GetExecutionHistory(
        [FromQuery] int? kpiId = null,
        [FromQuery] string? executedBy = null,
        [FromQuery] string? executionMethod = null,
        [FromQuery] bool? isSuccessful = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("MonitoringGrid");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("monitoring.usp_GetKpiExecutionHistory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            command.Parameters.AddWithValue("@KpiId", (object?)kpiId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExecutedBy", (object?)executedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExecutionMethod", (object?)executionMethod ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsSuccessful", (object?)isSuccessful ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            command.Parameters.AddWithValue("@PageNumber", pageNumber);

            using var reader = await command.ExecuteReaderAsync();
            var executions = new List<ExecutionHistoryDto>();
            int totalCount = 0;
            int totalPages = 0;

            while (await reader.ReadAsync())
            {
                totalCount = reader.GetInt32("TotalCount");
                totalPages = reader.GetInt32("TotalPages");

                var execution = new ExecutionHistoryDto
                {
                    HistoricalId = reader.GetInt64("HistoricalId"),
                    KpiId = reader.GetInt32("KpiId"),
                    Indicator = reader.GetString("Indicator"),
                    KpiOwner = reader.GetString("KpiOwner"),
                    SpName = reader.GetString("SpName"),
                    Timestamp = reader.GetDateTime("Timestamp"),
                    ExecutedBy = reader.IsDBNull("ExecutedBy") ? null : reader.GetString("ExecutedBy"),
                    ExecutionMethod = reader.IsDBNull("ExecutionMethod") ? null : reader.GetString("ExecutionMethod"),
                    CurrentValue = reader.GetDecimal("CurrentValue"),
                    HistoricalValue = reader.IsDBNull("HistoricalValue") ? null : reader.GetDecimal("HistoricalValue"),
                    DeviationPercent = reader.IsDBNull("DeviationPercent") ? null : reader.GetDecimal("DeviationPercent"),
                    Period = reader.GetInt32("Period"),
                    MetricKey = reader.GetString("MetricKey"),
                    IsSuccessful = reader.GetBoolean("IsSuccessful"),
                    ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
                    ExecutionTimeMs = reader.IsDBNull("ExecutionTimeMs") ? null : reader.GetInt32("ExecutionTimeMs"),
                    DatabaseName = reader.IsDBNull("DatabaseName") ? null : reader.GetString("DatabaseName"),
                    ServerName = reader.IsDBNull("ServerName") ? null : reader.GetString("ServerName"),
                    ShouldAlert = reader.GetBoolean("ShouldAlert"),
                    AlertSent = reader.GetBoolean("AlertSent"),
                    SessionId = reader.IsDBNull("SessionId") ? null : reader.GetString("SessionId"),
                    IpAddress = reader.IsDBNull("IpAddress") ? null : reader.GetString("IpAddress"),
                    SqlCommand = reader.IsDBNull("SqlCommand") ? null : reader.GetString("SqlCommand"),
                    RawResponse = reader.IsDBNull("RawResponse") ? null : reader.GetString("RawResponse"),
                    ExecutionContext = reader.IsDBNull("ExecutionContext") ? null : reader.GetString("ExecutionContext"),
                    PerformanceCategory = reader.GetString("PerformanceCategory"),
                    DeviationCategory = reader.GetString("DeviationCategory")
                };

                executions.Add(execution);
            }

            return Ok(new PaginatedExecutionHistoryDto
            {
                Executions = executions,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution history");
            return StatusCode(500, "Failed to retrieve execution history");
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<List<ExecutionStatsDto>>> GetExecutionStats(
        [FromQuery] int? kpiId = null,
        [FromQuery] int days = 30)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("MonitoringGrid");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("monitoring.usp_GetKpiExecutionStats", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@KpiId", (object?)kpiId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Days", days);

            using var reader = await command.ExecuteReaderAsync();
            var stats = new List<ExecutionStatsDto>();

            while (await reader.ReadAsync())
            {
                var stat = new ExecutionStatsDto
                {
                    KpiId = reader.GetInt32("KpiId"),
                    Indicator = reader.GetString("Indicator"),
                    Owner = reader.GetString("Owner"),
                    TotalExecutions = reader.GetInt32("TotalExecutions"),
                    SuccessfulExecutions = reader.GetInt32("SuccessfulExecutions"),
                    FailedExecutions = reader.GetInt32("FailedExecutions"),
                    SuccessRate = reader.GetDouble("SuccessRate"),
                    AvgExecutionTimeMs = reader.IsDBNull("AvgExecutionTimeMs") ? null : reader.GetDouble("AvgExecutionTimeMs"),
                    MinExecutionTimeMs = reader.IsDBNull("MinExecutionTimeMs") ? null : reader.GetInt32("MinExecutionTimeMs"),
                    MaxExecutionTimeMs = reader.IsDBNull("MaxExecutionTimeMs") ? null : reader.GetInt32("MaxExecutionTimeMs"),
                    AlertsTriggered = reader.GetInt32("AlertsTriggered"),
                    AlertsSent = reader.GetInt32("AlertsSent"),
                    LastExecution = reader.IsDBNull("LastExecution") ? null : reader.GetDateTime("LastExecution"),
                    UniqueExecutors = reader.GetInt32("UniqueExecutors"),
                    ExecutionMethods = reader.GetInt32("ExecutionMethods")
                };

                stats.Add(stat);
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution stats");
            return StatusCode(500, "Failed to retrieve execution statistics");
        }
    }

    [HttpGet("{historicalId}")]
    public async Task<ActionResult<ExecutionHistoryDetailDto>> GetExecutionDetail(long historicalId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("MonitoringGrid");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT hd.*, k.Indicator, k.Owner as KpiOwner, k.SpName
                FROM monitoring.HistoricalData hd
                INNER JOIN monitoring.KPIs k ON hd.KpiId = k.KpiId
                WHERE hd.HistoricalId = @HistoricalId";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@HistoricalId", historicalId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var detail = new ExecutionHistoryDetailDto
                {
                    HistoricalId = reader.GetInt64("HistoricalId"),
                    KpiId = reader.GetInt32("KpiId"),
                    Indicator = reader.GetString("Indicator"),
                    KpiOwner = reader.GetString("KpiOwner"),
                    SpName = reader.GetString("SpName"),
                    Timestamp = reader.GetDateTime("Timestamp"),
                    ExecutedBy = reader.IsDBNull("ExecutedBy") ? null : reader.GetString("ExecutedBy"),
                    ExecutionMethod = reader.IsDBNull("ExecutionMethod") ? null : reader.GetString("ExecutionMethod"),
                    CurrentValue = reader.GetDecimal("Value"),
                    HistoricalValue = reader.IsDBNull("HistoricalValue") ? null : reader.GetDecimal("HistoricalValue"),
                    DeviationPercent = reader.IsDBNull("DeviationPercent") ? null : reader.GetDecimal("DeviationPercent"),
                    Period = reader.GetInt32("Period"),
                    MetricKey = reader.GetString("MetricKey"),
                    IsSuccessful = reader.GetBoolean("IsSuccessful"),
                    ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
                    ExecutionTimeMs = reader.IsDBNull("ExecutionTimeMs") ? null : reader.GetInt32("ExecutionTimeMs"),
                    DatabaseName = reader.IsDBNull("DatabaseName") ? null : reader.GetString("DatabaseName"),
                    ServerName = reader.IsDBNull("ServerName") ? null : reader.GetString("ServerName"),
                    ShouldAlert = reader.GetBoolean("ShouldAlert"),
                    AlertSent = reader.GetBoolean("AlertSent"),
                    SessionId = reader.IsDBNull("SessionId") ? null : reader.GetString("SessionId"),
                    IpAddress = reader.IsDBNull("IpAddress") ? null : reader.GetString("IpAddress"),
                    UserAgent = reader.IsDBNull("UserAgent") ? null : reader.GetString("UserAgent"),
                    SqlCommand = reader.IsDBNull("SqlCommand") ? null : reader.GetString("SqlCommand"),
                    SqlParameters = reader.IsDBNull("SqlParameters") ? null : reader.GetString("SqlParameters"),
                    RawResponse = reader.IsDBNull("RawResponse") ? null : reader.GetString("RawResponse"),
                    ExecutionContext = reader.IsDBNull("ExecutionContext") ? null : reader.GetString("ExecutionContext"),
                    ConnectionString = reader.IsDBNull("ConnectionString") ? null : reader.GetString("ConnectionString")
                };

                return Ok(detail);
            }

            return NotFound($"Execution history with ID {historicalId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution detail for ID {HistoricalId}", historicalId);
            return StatusCode(500, "Failed to retrieve execution detail");
        }
    }
}
