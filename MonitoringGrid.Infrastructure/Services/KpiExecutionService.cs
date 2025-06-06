using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;
using Polly;
using System.Data;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service responsible for executing KPI stored procedures and calculating deviations
/// </summary>
public class KpiExecutionService : IKpiExecutionService
{
    private readonly MonitoringContext _context;
    private readonly MonitoringConfiguration _config;
    private readonly ILogger<KpiExecutionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAsyncPolicy _retryPolicy;

    public KpiExecutionService(
        MonitoringContext context,
        IOptions<MonitoringConfiguration> config,
        IConfiguration configuration,
        ILogger<KpiExecutionService> logger)
    {
        _context = context;
        _config = config.Value;
        _configuration = configuration;
        _logger = logger;

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: _config.AlertRetryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<KpiExecutionResult> ExecuteKpiAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing KPI {Indicator} using stored procedure {SpName}", kpi.Indicator, kpi.SpName);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await ExecuteStoredProcedureAsync(kpi, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute KPI {Indicator}: {Message}", kpi.Indicator, ex.Message);
            
            return new KpiExecutionResult
            {
                Key = kpi.Indicator,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow
            };
        }
    }

    private async Task<KpiExecutionResult> ExecuteStoredProcedureAsync(KPI kpi, CancellationToken cancellationToken)
    {
        // Determine connection string based on stored procedure location
        var connectionString = GetConnectionStringForStoredProcedure(kpi.SpName);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand(kpi.SpName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        // Add input parameters
        command.Parameters.AddWithValue("@ForLastMinutes", kpi.Frequency);

        // Check if this is a result set based stored procedure (like stats.stp_MonitorTransactions)
        if (IsResultSetBasedStoredProcedure(kpi.SpName))
        {
            return await ExecuteResultSetBasedStoredProcedureAsync(command, kpi, cancellationToken);
        }
        else
        {
            return await ExecuteOutputParameterBasedStoredProcedureAsync(command, kpi, cancellationToken);
        }
    }

    private async Task<KpiExecutionResult> ExecuteOutputParameterBasedStoredProcedureAsync(SqlCommand command, KPI kpi, CancellationToken cancellationToken)
    {
        // Add output parameters
        var keyParam = command.Parameters.Add("@Key", SqlDbType.NVarChar, 255);
        keyParam.Direction = ParameterDirection.Output;

        var currentParam = command.Parameters.Add("@CurrentValue", SqlDbType.Decimal);
        currentParam.Direction = ParameterDirection.Output;
        currentParam.Precision = 18;
        currentParam.Scale = 2;

        var historicalParam = command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal);
        historicalParam.Direction = ParameterDirection.Output;
        historicalParam.Precision = 18;
        historicalParam.Scale = 2;

        await command.ExecuteNonQueryAsync(cancellationToken);

        // Extract results
        var key = keyParam.Value?.ToString() ?? kpi.Indicator;
        var currentValue = currentParam.Value != DBNull.Value ? (decimal)currentParam.Value : 0;
        var historicalValue = historicalParam.Value != DBNull.Value ? (decimal)historicalParam.Value : 0;

        var deviationPercent = CalculateDeviation(currentValue, historicalValue);

        var result = new KpiExecutionResult
        {
            Key = key,
            CurrentValue = currentValue,
            HistoricalValue = historicalValue,
            DeviationPercent = deviationPercent,
            ExecutionTime = DateTime.UtcNow
        };

        result.ShouldAlert = ShouldTriggerAlert(kpi, result);

        // Store historical data
        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        _logger.LogDebug("KPI {Indicator} executed successfully: Current={Current}, Historical={Historical}, Deviation={Deviation}%",
            kpi.Indicator, currentValue, historicalValue, deviationPercent);

        return result;
    }

    private async Task<KpiExecutionResult> ExecuteResultSetBasedStoredProcedureAsync(SqlCommand command, KPI kpi, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing result set based stored procedure {SpName}", kpi.SpName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        decimal currentValue = 0;
        decimal historicalValue = 0;
        string key = kpi.Indicator;

        var result = new KpiExecutionResult
        {
            Key = key,
            CurrentValue = currentValue,
            HistoricalValue = historicalValue,
            DeviationPercent = 0,
            ExecutionTime = DateTime.UtcNow
        };

        // Process the result set based on the stored procedure type
        if (kpi.SpName.Contains("stp_MonitorTransactions"))
        {
            var transactionStats = await ProcessTransactionMonitoringResultSet(reader, cancellationToken);
            currentValue = transactionStats.SuccessRate;
            historicalValue = transactionStats.HistoricalSuccessRate;
            key = "TransactionSuccessRate";

            // Store detailed execution information
            var executionDetails = new StringBuilder();
            executionDetails.AppendLine("=== TRANSACTION MONITORING RESULTS ===");

            foreach (var detail in transactionStats.TransactionDetails)
            {
                executionDetails.AppendLine($"📊 {detail.TransactionType}: {detail.SuccessfulCount}/{detail.TotalCount} successful ({detail.SuccessRate:F1}%)");
            }

            executionDetails.AppendLine($"🎯 OVERALL: {transactionStats.SuccessfulTransactions}/{transactionStats.TotalTransactions} successful ({transactionStats.SuccessRate:F2}%)");
            var deviation = transactionStats.HistoricalSuccessRate > 0
                ? Math.Abs((transactionStats.SuccessRate - transactionStats.HistoricalSuccessRate) / transactionStats.HistoricalSuccessRate) * 100
                : 0;
            executionDetails.AppendLine($"📈 Historical: {transactionStats.HistoricalSuccessRate:F1}%, Deviation: {deviation:F2}%");

            result.ExecutionDetails = executionDetails.ToString();
            // Create detailed execution information including raw results
            var rawResultsText = new StringBuilder();
            rawResultsText.AppendLine("=== RAW STORED PROCEDURE RESULTS ===");
            rawResultsText.AppendLine("ItemName\tTotal\tSuccessful\tSuccessRate");

            foreach (var rawResult in transactionStats.RawResults)
            {
                rawResultsText.AppendLine($"{rawResult["ItemName"]}\t{rawResult["Total"]}\t{rawResult["Successful"]}\t{rawResult["SuccessRate"]:F2}%");
            }

            rawResultsText.AppendLine("=== END RAW RESULTS ===");

            // Store in ExecutionDetails for display
            result.ExecutionDetails += Environment.NewLine + rawResultsText.ToString();

            result.Metadata = new Dictionary<string, object>
            {
                ["TotalTransactions"] = transactionStats.TotalTransactions,
                ["SuccessfulTransactions"] = transactionStats.SuccessfulTransactions,
                ["TransactionBreakdown"] = transactionStats.TransactionDetails.Select(d => new
                {
                    Type = d.TransactionType,
                    Total = d.TotalCount,
                    Successful = d.SuccessfulCount,
                    SuccessRate = d.SuccessRate
                }).ToList(),
                ["RawStoredProcedureResults"] = transactionStats.RawResults,
                ["RawResultsFormatted"] = rawResultsText.ToString()
            };
        }
        else
        {
            // Generic result set processing - take first numeric column as current value
            var genericStats = await ProcessGenericResultSet(reader, cancellationToken);
            currentValue = genericStats.CurrentValue;
            historicalValue = genericStats.HistoricalValue;
            key = genericStats.Key;
        }

        var deviationPercent = CalculateDeviation(currentValue, historicalValue);

        // Update the result with final values
        result.Key = key;
        result.CurrentValue = currentValue;
        result.HistoricalValue = historicalValue;
        result.DeviationPercent = deviationPercent;

        result.ShouldAlert = ShouldTriggerAlert(kpi, result);

        // Store historical data
        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        _logger.LogDebug("KPI {Indicator} executed successfully: Current={Current}, Historical={Historical}, Deviation={Deviation}%",
            kpi.Indicator, currentValue, historicalValue, deviationPercent);

        return result;
    }

    public decimal CalculateDeviation(decimal current, decimal historical)
    {
        if (historical == 0)
        {
            return current == 0 ? 0 : 100; // If both are 0, no deviation; if only historical is 0, 100% deviation
        }

        return Math.Abs((current - historical) / historical) * 100;
    }

    public bool ShouldTriggerAlert(KPI kpi, KpiExecutionResult result)
    {
        if (!result.IsSuccessful)
            return false;

        // Check absolute threshold if configured
        if (_config.EnableAbsoluteThresholds && kpi.MinimumThreshold.HasValue)
        {
            if (result.CurrentValue < kpi.MinimumThreshold.Value)
            {
                _logger.LogDebug("KPI {Indicator} below minimum threshold: {Current} < {Threshold}",
                    kpi.Indicator, result.CurrentValue, kpi.MinimumThreshold.Value);
                return true;
            }
        }

        // Check percentage deviation if historical comparison is enabled
        if (_config.EnableHistoricalComparison)
        {
            if (result.DeviationPercent > kpi.Deviation)
            {
                _logger.LogDebug("KPI {Indicator} exceeds deviation threshold: {Deviation}% > {Threshold}%",
                    kpi.Indicator, result.DeviationPercent, kpi.Deviation);
                return true;
            }
        }

        return false;
    }

    private string GetConnectionStringForStoredProcedure(string spName)
    {
        // If the stored procedure is in the stats schema, use the main database connection
        if (spName.StartsWith("[stats].") || spName.StartsWith("stats."))
        {
            // Get the MainDatabase connection string from configuration
            var mainConnectionString = _configuration.GetConnectionString("MainDatabase");
            if (!string.IsNullOrEmpty(mainConnectionString))
            {
                _logger.LogDebug("Using MainDatabase connection string for stored procedure {SpName}", spName);
                return mainConnectionString;
            }
            else
            {
                _logger.LogWarning("MainDatabase connection string not found, falling back to monitoring database for {SpName}", spName);
            }
        }

        // Check if this is a monitoring stored procedure that makes cross-database queries
        var crossDatabaseProcedures = new[]
        {
            "monitoring.usp_MonitorTransactions",
            "monitoring.usp_MonitorSettlementCompanies",
            "monitoring.usp_MonitorCountryDeposits",
            "monitoring.usp_MonitorWhiteLabelPerformance"
        };

        if (crossDatabaseProcedures.Any(proc => spName.Contains(proc)))
        {
            // These procedures need access to ProgressPlayDBTest, so use MainDatabase connection
            var mainConnectionString = _configuration.GetConnectionString("MainDatabase");
            if (!string.IsNullOrEmpty(mainConnectionString))
            {
                _logger.LogDebug("Using MainDatabase connection string for cross-database stored procedure {SpName}", spName);
                return mainConnectionString;
            }
            else
            {
                _logger.LogWarning("MainDatabase connection string not found for cross-database procedure {SpName}, falling back to monitoring database", spName);
            }
        }

        // Default to monitoring database
        return _context.Database.GetConnectionString();
    }

    private bool IsResultSetBasedStoredProcedure(string spName)
    {
        // List of stored procedures that return result sets instead of output parameters
        var resultSetBasedProcedures = new[]
        {
            "stp_MonitorTransactions",
            "[stats].[stp_MonitorTransactions]",
            "stats.stp_MonitorTransactions"
        };

        return resultSetBasedProcedures.Any(proc => spName.Contains(proc));
    }

    private async Task<TransactionMonitoringResult> ProcessTransactionMonitoringResultSet(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new TransactionMonitoringResult();
        var transactionDetails = new List<TransactionTypeDetail>();
        var rawResults = new List<Dictionary<string, object>>();

        _logger.LogInformation("=== STORED PROCEDURE RESULTS ===");

        if (await reader.ReadAsync(cancellationToken))
        {
            // Process the transaction monitoring result set
            // Expected columns: ItemName, Total, Successful
            int totalTransactions = 0;
            int successfulTransactions = 0;

            do
            {
                var itemName = reader.GetString("ItemName");
                var total = reader.GetInt32("Total");
                var successful = reader.GetInt32("Successful");
                var successRate = total > 0 ? (decimal)successful / total * 100 : 100;

                totalTransactions += total;
                successfulTransactions += successful;

                // Store detailed transaction info
                var detail = new TransactionTypeDetail
                {
                    TransactionType = itemName,
                    TotalCount = total,
                    SuccessfulCount = successful,
                    SuccessRate = successRate
                };
                transactionDetails.Add(detail);

                // Capture raw result row
                var rawRow = new Dictionary<string, object>
                {
                    ["ItemName"] = itemName,
                    ["Total"] = total,
                    ["Successful"] = successful,
                    ["SuccessRate"] = successRate,
                    ["FailedCount"] = total - successful
                };
                rawResults.Add(rawRow);

                // Log each transaction type with detailed info
                _logger.LogInformation("📊 {ItemName}: {Successful}/{Total} successful ({SuccessRate:F1}%)",
                    itemName, successful, total, successRate);

            } while (await reader.ReadAsync(cancellationToken));

            // Calculate overall success rate as percentage
            result.SuccessRate = totalTransactions > 0 ? (decimal)successfulTransactions / totalTransactions * 100 : 100;
            result.TotalTransactions = totalTransactions;
            result.SuccessfulTransactions = successfulTransactions;
            result.TransactionDetails = transactionDetails;
            result.RawResults = rawResults;

            // For now, use a baseline of 95% as historical success rate
            // In a real implementation, this would come from historical data
            result.HistoricalSuccessRate = 95.0m;

            _logger.LogInformation("🎯 OVERALL SUMMARY: {Successful}/{Total} transactions successful ({SuccessRate:F2}%)",
                successfulTransactions, totalTransactions, result.SuccessRate);
            var logDeviation = result.HistoricalSuccessRate > 0
                ? Math.Abs((result.SuccessRate - result.HistoricalSuccessRate) / result.HistoricalSuccessRate) * 100
                : 0;
            _logger.LogInformation("📈 Historical baseline: {HistoricalRate:F1}%, Deviation: {Deviation:F2}%",
                result.HistoricalSuccessRate, logDeviation);
        }
        else
        {
            _logger.LogWarning("⚠️ No transaction data returned from stored procedure");

            // Set default values when no data is returned
            result.SuccessRate = 0;
            result.TotalTransactions = 0;
            result.SuccessfulTransactions = 0;
            result.HistoricalSuccessRate = 95.0m; // Keep baseline for comparison
            result.TransactionDetails = new List<TransactionTypeDetail>();
        }

        _logger.LogInformation("=== END STORED PROCEDURE RESULTS ===");
        return result;
    }

    private async Task<GenericResultSetResult> ProcessGenericResultSet(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new GenericResultSetResult
        {
            Key = "GenericMetric",
            CurrentValue = 0,
            HistoricalValue = 0
        };

        if (await reader.ReadAsync(cancellationToken))
        {
            // Try to find numeric columns and use the first one as current value
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldType = reader.GetFieldType(i);
                if (fieldType == typeof(int) || fieldType == typeof(decimal) || fieldType == typeof(double) || fieldType == typeof(float))
                {
                    var value = Convert.ToDecimal(reader.GetValue(i));
                    result.CurrentValue = value;
                    result.Key = reader.GetName(i);
                    break;
                }
            }

            // For generic result sets, use a simple baseline for historical comparison
            result.HistoricalValue = result.CurrentValue * 0.9m; // Assume 10% lower as historical baseline
        }

        return result;
    }

    private async Task StoreHistoricalDataAsync(KPI kpi, KpiExecutionResult result, CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = GetConnectionStringForStoredProcedure(kpi.SpName);
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

            var historicalData = new HistoricalData
            {
                KpiId = kpi.KpiId,
                Timestamp = result.ExecutionTime,
                Value = result.CurrentValue,
                Period = kpi.Frequency,
                MetricKey = result.Key,

                // Comprehensive audit information
                ExecutedBy = "System", // TODO: Get from current user context
                ExecutionMethod = "API", // TODO: Determine execution method
                SqlCommand = $"EXEC {kpi.SpName} @Frequency = {kpi.Frequency}",
                SqlParameters = $"@Frequency = {kpi.Frequency}",
                RawResponse = result.ExecutionDetails,
                ExecutionTimeMs = result.ExecutionTimeMs,
                ConnectionString = MaskConnectionString(connectionString),
                DatabaseName = connectionStringBuilder.InitialCatalog,
                ServerName = connectionStringBuilder.DataSource,
                IsSuccessful = string.IsNullOrEmpty(result.ErrorMessage),
                ErrorMessage = result.ErrorMessage,
                DeviationPercent = result.DeviationPercent,
                HistoricalValue = result.HistoricalValue,
                ShouldAlert = result.ShouldAlert,
                AlertSent = false, // TODO: Track if alert was actually sent
                SessionId = null, // TODO: Get from HTTP context
                UserAgent = null, // TODO: Get from HTTP context
                IpAddress = null, // TODO: Get from HTTP context
                ExecutionContext = result.Metadata != null ?
                    System.Text.Json.JsonSerializer.Serialize(result.Metadata) : null
            };

            _context.HistoricalData.Add(historicalData);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("📊 Stored comprehensive audit data for KPI {Indicator}: Value={Value}, Success={Success}, Database={Database}",
                kpi.Indicator, result.CurrentValue, historicalData.IsSuccessful, historicalData.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to store historical data for KPI {Indicator}: {Message}",
                kpi.Indicator, ex.Message);
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.Password = "***MASKED***";
            if (!string.IsNullOrEmpty(builder.UserID))
            {
                builder.UserID = builder.UserID.Substring(0, Math.Min(3, builder.UserID.Length)) + "***";
            }
            return builder.ToString();
        }
        catch
        {
            return "***MASKED CONNECTION STRING***";
        }
    }

    public async Task<bool> ValidateKpiStoredProcedureAsync(string spName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.ROUTINES 
                WHERE ROUTINE_TYPE = 'PROCEDURE' 
                AND ROUTINE_NAME = @SpName";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SpName", spName);

            var count = (int)await command.ExecuteScalarAsync(cancellationToken);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate stored procedure {SpName}: {Message}", spName, ex.Message);
            return false;
        }
    }
}

/// <summary>
/// Result from processing transaction monitoring stored procedure
/// </summary>
internal class TransactionMonitoringResult
{
    public decimal SuccessRate { get; set; }
    public decimal HistoricalSuccessRate { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public List<TransactionTypeDetail> TransactionDetails { get; set; } = new();
    public List<Dictionary<string, object>> RawResults { get; set; } = new();
}

/// <summary>
/// Details for a specific transaction type
/// </summary>
internal class TransactionTypeDetail
{
    public string TransactionType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessfulCount { get; set; }
    public decimal SuccessRate { get; set; }
}

/// <summary>
/// Result from processing generic result set
/// </summary>
internal class GenericResultSetResult
{
    public string Key { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
}
