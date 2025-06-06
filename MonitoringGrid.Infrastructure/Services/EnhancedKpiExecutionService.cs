using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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
/// Enhanced KPI execution service that supports multiple KPI types
/// </summary>
public class EnhancedKpiExecutionService : IKpiExecutionService
{
    private readonly MonitoringContext _context;
    private readonly MonitoringConfiguration _config;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnhancedKpiExecutionService> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public EnhancedKpiExecutionService(
        MonitoringContext context,
        IOptions<MonitoringConfiguration> config,
        IConfiguration configuration,
        ILogger<EnhancedKpiExecutionService> logger)
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
            _logger.LogDebug("Executing KPI {Indicator} (Type: {KpiType}) using stored procedure {SpName}", 
                kpi.Indicator, kpi.KpiType, kpi.SpName);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await ExecuteKpiByTypeAsync(kpi, cancellationToken);
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

    public decimal CalculateDeviation(decimal current, decimal historical)
    {
        if (historical == 0)
            return current == 0 ? 0 : 100;

        return Math.Abs((current - historical) / historical) * 100;
    }

    public bool ShouldTriggerAlert(KPI kpi, KpiExecutionResult result)
    {
        // For threshold-based KPIs, use threshold evaluation
        if (kpi.KpiType == "threshold")
        {
            return EvaluateThreshold(result.CurrentValue ?? 0, kpi.ThresholdValue ?? 0, kpi.ComparisonOperator ?? "gt");
        }

        // For other types, use deviation-based alerting
        if (!result.CurrentValue.HasValue || !result.HistoricalValue.HasValue)
            return false;

        var deviation = CalculateDeviation(result.CurrentValue.Value, result.HistoricalValue.Value);
        
        // Check minimum threshold if specified
        if (kpi.MinimumThreshold.HasValue && result.CurrentValue < kpi.MinimumThreshold)
            return false;

        return deviation > kpi.Deviation;
    }

    public bool EvaluateThreshold(decimal value, decimal threshold, string comparisonOperator)
    {
        return comparisonOperator.ToLower() switch
        {
            "gt" => value > threshold,
            "gte" => value >= threshold,
            "lt" => value < threshold,
            "lte" => value <= threshold,
            "eq" => Math.Abs(value - threshold) < 0.01m, // Small tolerance for decimal comparison
            _ => false
        };
    }

    private async Task<KpiExecutionResult> ExecuteKpiByTypeAsync(KPI kpi, CancellationToken cancellationToken)
    {
        switch (kpi.KpiType.ToLower())
        {
            case "success_rate":
                return await ExecuteSuccessRateKpiAsync(kpi, cancellationToken);
            
            case "transaction_volume":
                return await ExecuteTransactionVolumeKpiAsync(kpi, cancellationToken);
            
            case "threshold":
                return await ExecuteThresholdKpiAsync(kpi, cancellationToken);
            
            case "trend_analysis":
                return await ExecuteTrendAnalysisKpiAsync(kpi, cancellationToken);
            
            default:
                _logger.LogWarning("Unknown KPI type {KpiType}, falling back to default execution", kpi.KpiType);
                return await ExecuteDefaultKpiAsync(kpi, cancellationToken);
        }
    }

    private async Task<KpiExecutionResult> ExecuteSuccessRateKpiAsync(KPI kpi, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("MonitoringGrid");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand(kpi.SpName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        // Add parameters
        command.Parameters.AddWithValue("@ForLastMinutes", kpi.LastMinutes);
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
        command.Parameters.Add("@CurrentValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;
        command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = new KpiExecutionResult
        {
            Key = command.Parameters["@Key"].Value?.ToString() ?? kpi.Indicator,
            CurrentValue = Convert.ToDecimal(command.Parameters["@CurrentValue"].Value ?? 0),
            HistoricalValue = Convert.ToDecimal(command.Parameters["@HistoricalValue"].Value ?? 0),
            ExecutionTime = DateTime.UtcNow
        };

        result.DeviationPercent = CalculateDeviation(result.CurrentValue ?? 0, result.HistoricalValue ?? 0);
        result.ShouldAlert = ShouldTriggerAlert(kpi, result);

        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        return result;
    }

    private async Task<KpiExecutionResult> ExecuteTransactionVolumeKpiAsync(KPI kpi, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("MonitoringGrid");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand("monitoring.usp_MonitorTransactionVolume", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        // Add parameters
        command.Parameters.AddWithValue("@ForLastMinutes", kpi.LastMinutes);
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
        command.Parameters.Add("@CurrentValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;
        command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = new KpiExecutionResult
        {
            Key = command.Parameters["@Key"].Value?.ToString() ?? kpi.Indicator,
            CurrentValue = Convert.ToDecimal(command.Parameters["@CurrentValue"].Value ?? 0),
            HistoricalValue = Convert.ToDecimal(command.Parameters["@HistoricalValue"].Value ?? 0),
            ExecutionTime = DateTime.UtcNow
        };

        result.DeviationPercent = CalculateDeviation(result.CurrentValue ?? 0, result.HistoricalValue ?? 0);
        result.ShouldAlert = ShouldTriggerAlert(kpi, result);

        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        return result;
    }

    private async Task<KpiExecutionResult> ExecuteThresholdKpiAsync(KPI kpi, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("MonitoringGrid");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand("monitoring.usp_MonitorThreshold", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        // Add parameters
        command.Parameters.AddWithValue("@ForLastMinutes", kpi.LastMinutes);
        command.Parameters.AddWithValue("@ThresholdValue", kpi.ThresholdValue ?? 0);
        command.Parameters.AddWithValue("@ComparisonOperator", kpi.ComparisonOperator ?? "gt");
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
        command.Parameters.Add("@CurrentValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;
        command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = new KpiExecutionResult
        {
            Key = command.Parameters["@Key"].Value?.ToString() ?? kpi.Indicator,
            CurrentValue = Convert.ToDecimal(command.Parameters["@CurrentValue"].Value ?? 0),
            HistoricalValue = Convert.ToDecimal(command.Parameters["@HistoricalValue"].Value ?? 0),
            ExecutionTime = DateTime.UtcNow
        };

        // For threshold KPIs, evaluate the threshold condition
        result.ShouldAlert = EvaluateThreshold(
            result.CurrentValue ?? 0, 
            kpi.ThresholdValue ?? 0, 
            kpi.ComparisonOperator ?? "gt");

        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        return result;
    }

    private async Task<KpiExecutionResult> ExecuteTrendAnalysisKpiAsync(KPI kpi, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("MonitoringGrid");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand("monitoring.usp_MonitorTrends", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        // Add parameters
        command.Parameters.AddWithValue("@ForLastMinutes", kpi.LastMinutes);
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
        command.Parameters.Add("@CurrentValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;
        command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = new KpiExecutionResult
        {
            Key = command.Parameters["@Key"].Value?.ToString() ?? kpi.Indicator,
            CurrentValue = Convert.ToDecimal(command.Parameters["@CurrentValue"].Value ?? 0),
            HistoricalValue = Convert.ToDecimal(command.Parameters["@HistoricalValue"].Value ?? 0),
            ExecutionTime = DateTime.UtcNow
        };

        result.DeviationPercent = CalculateDeviation(result.CurrentValue ?? 0, result.HistoricalValue ?? 0);
        result.ShouldAlert = ShouldTriggerAlert(kpi, result);

        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        return result;
    }

    private async Task<KpiExecutionResult> ExecuteDefaultKpiAsync(KPI kpi, CancellationToken cancellationToken)
    {
        // Fall back to the original execution method
        var connectionString = _configuration.GetConnectionString("MonitoringGrid");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand(kpi.SpName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _config.DatabaseTimeoutSeconds
        };

        command.Parameters.AddWithValue("@ForLastMinutes", kpi.LastMinutes);
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
        command.Parameters.Add("@CurrentValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;
        command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal).Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = new KpiExecutionResult
        {
            Key = command.Parameters["@Key"].Value?.ToString() ?? kpi.Indicator,
            CurrentValue = Convert.ToDecimal(command.Parameters["@CurrentValue"].Value ?? 0),
            HistoricalValue = Convert.ToDecimal(command.Parameters["@HistoricalValue"].Value ?? 0),
            ExecutionTime = DateTime.UtcNow
        };

        result.DeviationPercent = CalculateDeviation(result.CurrentValue ?? 0, result.HistoricalValue ?? 0);
        result.ShouldAlert = ShouldTriggerAlert(kpi, result);

        await StoreHistoricalDataAsync(kpi, result, cancellationToken);

        return result;
    }

    private async Task StoreHistoricalDataAsync(KPI kpi, KpiExecutionResult result, CancellationToken cancellationToken)
    {
        try
        {
            var historicalData = new HistoricalData
            {
                KpiId = kpi.KpiId,
                Timestamp = result.ExecutionTime,
                Value = result.CurrentValue ?? 0,
                Period = kpi.LastMinutes,
                MetricKey = result.Key
            };

            _context.HistoricalData.Add(historicalData);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store historical data for KPI {KpiId}", kpi.KpiId);
        }
    }
}
