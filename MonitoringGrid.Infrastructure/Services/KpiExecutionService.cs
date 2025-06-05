using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;
using Polly;
using System.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service responsible for executing KPI stored procedures and calculating deviations
/// </summary>
public class KpiExecutionService : IKpiExecutionService
{
    private readonly MonitoringContext _context;
    private readonly MonitoringConfiguration _config;
    private readonly ILogger<KpiExecutionService> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public KpiExecutionService(
        MonitoringContext context,
        IOptions<MonitoringConfiguration> config,
        ILogger<KpiExecutionService> logger)
    {
        _context = context;
        _config = config.Value;
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
        var connectionString = _context.Database.GetConnectionString();
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand(kpi.SpName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        // Add input parameters
        command.Parameters.AddWithValue("@ForLastMinutes", kpi.Frequency);

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
