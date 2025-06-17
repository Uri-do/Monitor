using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;
using System.Diagnostics;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service implementation for indicator execution operations
/// Replaces KpiExecutionService
/// </summary>
public class IndicatorExecutionService : IIndicatorExecutionService
{
    private readonly MonitoringContext _context;
    private readonly IIndicatorService _indicatorService;
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly ILogger<IndicatorExecutionService> _logger;

    public IndicatorExecutionService(
        MonitoringContext context,
        IIndicatorService indicatorService,
        IProgressPlayDbService progressPlayDbService,
        ILogger<IndicatorExecutionService> logger)
    {
        _context = context;
        _indicatorService = indicatorService;
        _progressPlayDbService = progressPlayDbService;
        _logger = logger;
    }

    public async Task<IndicatorExecutionResult> ExecuteIndicatorAsync(long indicatorId, string executionContext = "Manual",
        bool saveResults = true, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var executionTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Executing indicator {IndicatorId} with context {ExecutionContext}", 
                indicatorId, executionContext);

            // Get the indicator
            var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                return new IndicatorExecutionResult
                {
                    ExecutionId = 0,
                    IndicatorId = indicatorId,
                    IndicatorName = "Unknown",
                    WasSuccessful = false,
                    ErrorMessage = $"Indicator {indicatorId} not found",
                    ExecutionDuration = stopwatch.Elapsed,
                    StartTime = executionTime,
                    ExecutionContext = executionContext
                };
            }

            var indicator = indicatorResult.Value;

            // Check if indicator is active
            if (!indicator.IsActive)
            {
                return new IndicatorExecutionResult
                {
                    ExecutionId = 0,
                    IndicatorId = indicatorId,
                    IndicatorName = indicator.IndicatorName,
                    WasSuccessful = false,
                    ErrorMessage = "Indicator is not active",
                    ExecutionDuration = stopwatch.Elapsed,
                    StartTime = executionTime,
                    ExecutionContext = executionContext
                };
            }

            // Mark indicator as running
            indicator.StartExecution(executionContext);
            if (saveResults)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            try
            {
                // Check if collector is configured
                if (indicator.CollectorID == 0)
                {
                    return CreateFailureResult(indicator, "No collector configured for this indicator",
                        stopwatch.Elapsed, executionTime, executionContext);
                }

                // Execute the collector stored procedure to get current data
                _logger.LogDebug("Executing collector {CollectorId} stored procedure for indicator {IndicatorId}",
                    indicator.CollectorID, indicator.IndicatorID);

                var rawData = await _progressPlayDbService.ExecuteCollectorStoredProcedureAsync(
                    indicator.CollectorID,
                    indicator.LastMinutes,
                    cancellationToken);

                _logger.LogDebug("Collector {CollectorId} returned {Count} items: {Items}",
                    indicator.CollectorID, rawData.Count,
                    string.Join(", ", rawData.Select(d => $"'{d.ItemName}'")));

                _logger.LogDebug("Looking for CollectorItemName: '{CollectorItemName}' in indicator {IndicatorId}",
                    indicator.CollectorItemName, indicator.IndicatorID);

                // Find the specific item we're monitoring
                var itemData = rawData.FirstOrDefault(d => d.ItemName == indicator.CollectorItemName);
                if (itemData == null)
                {
                    var availableItems = string.Join(", ", rawData.Select(d => $"'{d.ItemName}'"));
                    var errorMessage = $"Item '{indicator.CollectorItemName}' not found in collector results. Available items: [{availableItems}]";

                    _logger.LogWarning("Indicator {IndicatorId} execution failed: {ErrorMessage}",
                        indicator.IndicatorID, errorMessage);

                    return CreateFailureResult(indicator, errorMessage,
                        stopwatch.Elapsed, executionTime, executionContext);
                }

                // Get the current value based on threshold field
                var currentValue = GetValueByField(itemData, indicator.ThresholdField);

                // Get historical average if needed for threshold type
                decimal? historicalAverage = null;
                if (indicator.ThresholdType == "volume_average" && indicator.CollectorID > 0)
                {
                    historicalAverage = await _progressPlayDbService.GetCollectorItemAverageAsync(
                        indicator.CollectorID,
                        indicator.CollectorItemName,
                        indicator.ThresholdField,
                        DateTime.UtcNow.Hour,
                        indicator.AverageLastDays.HasValue ? DateTime.UtcNow.AddDays(-indicator.AverageLastDays.Value) : null,
                        cancellationToken);
                }

                // Evaluate threshold
                var thresholdBreached = EvaluateThreshold(indicator, currentValue, historicalAverage);

                // Convert DTOs to Models for the result
                var rawDataModels = rawData.Select(dto => new Core.Models.CollectorStatisticDto
                {
                    StatisticID = 0, // Not available from DTO
                    ItemName = dto.ItemName ?? string.Empty,
                    StatisticDate = dto.Timestamp,
                    Total = (int)(dto.Total ?? 0),
                    Marked = (int)(dto.Marked ?? 0),
                    MarkedPercent = dto.MarkedPercent ?? 0,
                    CreatedDate = dto.Timestamp
                }).ToList();

                var result = new IndicatorExecutionResult
                {
                    ExecutionId = 0, // Will be set when saved to database
                    IndicatorId = indicatorId,
                    IndicatorName = indicator.IndicatorName,
                    WasSuccessful = true,
                    Value = currentValue,
                    ThresholdValue = indicator.ThresholdValue,
                    ThresholdBreached = thresholdBreached,
                    RawData = rawDataModels,
                    ExecutionDuration = stopwatch.Elapsed,
                    StartTime = executionTime,
                    ExecutionContext = executionContext
                };

                // Save results if requested
                if (saveResults)
                {
                    await SaveExecutionResultsAsync(indicator, result, cancellationToken);
                    
                    // Trigger alert if threshold breached
                    if (thresholdBreached)
                    {
                        await TriggerAlertAsync(indicator, result, cancellationToken);
                    }
                }

                _logger.LogInformation("Successfully executed indicator {IndicatorId}: {IndicatorName}, " +
                    "Value: {CurrentValue}, Threshold Breached: {ThresholdBreached}", 
                    indicatorId, indicator.IndicatorName, currentValue, thresholdBreached);

                return result;
            }
            finally
            {
                // Mark indicator as completed
                indicator.CompleteExecution();
                if (saveResults)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute indicator {IndicatorId}", indicatorId);

            var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(indicatorId, cancellationToken);
            var indicator = indicatorResult.IsSuccess ? indicatorResult.Value : null;
            return CreateFailureResult(indicator, ex.Message, stopwatch.Elapsed, executionTime, executionContext);
        }
    }

    public async Task<List<IndicatorExecutionResult>> ExecuteIndicatorsAsync(List<long> indicatorIds,
        string executionContext = "Scheduled", bool saveResults = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing {Count} indicators with context {ExecutionContext}", 
            indicatorIds.Count, executionContext);

        var results = new List<IndicatorExecutionResult>();

        foreach (var indicatorId in indicatorIds)
        {
            var result = await ExecuteIndicatorAsync(indicatorId, executionContext, saveResults, cancellationToken);
            results.Add(result);
        }

        _logger.LogInformation("Completed execution of {Count} indicators, {SuccessCount} successful", 
            results.Count, results.Count(r => r.WasSuccessful));

        return results;
    }

    public async Task<IndicatorExecutionResult> TestIndicatorAsync(long indicatorId, int? overrideLastMinutes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Testing indicator {IndicatorId}", indicatorId);

        // Get the indicator and temporarily override LastMinutes if provided
        var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(indicatorId, cancellationToken);
        if (indicatorResult.IsSuccess && overrideLastMinutes.HasValue)
        {
            indicatorResult.Value.LastMinutes = overrideLastMinutes.Value;
        }

        // Execute without saving results
        return await ExecuteIndicatorAsync(indicatorId, "Test", saveResults: false, cancellationToken);
    }

    public async Task<List<IndicatorExecutionResult>> ExecuteDueIndicatorsAsync(string executionContext = "Scheduled",
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing due indicators with context {ExecutionContext}", executionContext);

        var dueIndicatorsResult = await _indicatorService.GetDueIndicatorsAsync(null, cancellationToken);
        if (!dueIndicatorsResult.IsSuccess)
        {
            _logger.LogWarning("Failed to get due indicators: {Error}", dueIndicatorsResult.Error.Message);
            return new List<IndicatorExecutionResult>();
        }

        var indicatorIds = dueIndicatorsResult.Value.Select(i => i.IndicatorID).ToList();

        _logger.LogInformation("Found {Count} due indicators for execution", indicatorIds.Count);

        return await ExecuteIndicatorsAsync(indicatorIds, executionContext, saveResults: true, cancellationToken);
    }

    public async Task<IndicatorExecutionStatus> GetIndicatorExecutionStatusAsync(long indicatorId,
        CancellationToken cancellationToken = default)
    {
        var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(indicatorId, cancellationToken);
        if (!indicatorResult.IsSuccess)
        {
            return new IndicatorExecutionStatus
            {
                IndicatorID = indicatorId,
                IndicatorName = "Unknown",
                Status = "error"
            };
        }

        var indicator = indicatorResult.Value;
        return new IndicatorExecutionStatus
        {
            IndicatorID = indicatorId,
            IndicatorName = indicator.IndicatorName,
            IsCurrentlyRunning = indicator.IsCurrentlyRunning,
            ExecutionStartTime = indicator.ExecutionStartTime,
            ExecutionContext = indicator.ExecutionContext,
            ExecutionDuration = indicator.GetExecutionDuration(),
            LastRun = indicator.LastRun,
            NextRun = indicator.GetNextRunTime(),
            Status = GetIndicatorStatus(indicator)
        };
    }

    public async Task<bool> CancelIndicatorExecutionAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess || !indicatorResult.Value.IsCurrentlyRunning)
            {
                return false;
            }

            var indicator = indicatorResult.Value;
            indicator.CompleteExecution();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled execution of indicator {IndicatorId}: {IndicatorName}",
                indicatorId, indicator.IndicatorName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel indicator execution {IndicatorId}", indicatorId);
            return false;
        }
    }

    public Task<List<IndicatorExecutionHistory>> GetIndicatorExecutionHistoryAsync(long indicatorId, int days = 30,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement with new IndicatorsExecutionHistory table
        // For now, return empty list since HistoricalData table is obsolete
        return Task.FromResult(new List<IndicatorExecutionHistory>());
    }

    private static decimal GetValueByField(Core.DTOs.CollectorStatisticDto data, string field)
    {
        return field.ToLower() switch
        {
            "total" => data.Total ?? 0,
            "marked" => data.Marked ?? 0,
            "markedpercent" => data.MarkedPercent ?? 0,
            _ => data.Total ?? 0
        };
    }

    private static bool EvaluateThreshold(Core.Entities.Indicator indicator, decimal currentValue, decimal? historicalAverage)
    {
        var thresholdValue = indicator.ThresholdType == "volume_average" && historicalAverage.HasValue
            ? historicalAverage.Value
            : indicator.ThresholdValue;

        return indicator.ThresholdComparison.ToLower() switch
        {
            "gt" => currentValue > thresholdValue,
            "gte" => currentValue >= thresholdValue,
            "lt" => currentValue < thresholdValue,
            "lte" => currentValue <= thresholdValue,
            "eq" => currentValue == thresholdValue,
            _ => false
        };
    }

    private static string GetIndicatorStatus(Core.Entities.Indicator indicator)
    {
        if (indicator.IsCurrentlyRunning)
            return "running";

        if (!indicator.IsActive)
            return "inactive";

        if (indicator.LastRun == null)
            return "never_run";

        if (indicator.IsDue())
            return "due";

        return "idle";
    }

    private static IndicatorExecutionResult CreateFailureResult(Core.Entities.Indicator? indicator, string errorMessage,
        TimeSpan duration, DateTime executionTime, string executionContext)
    {
        return new IndicatorExecutionResult
        {
            ExecutionId = 0,
            IndicatorId = indicator?.IndicatorID ?? 0L,
            IndicatorName = indicator?.IndicatorName ?? "Unknown",
            WasSuccessful = false,
            ErrorMessage = errorMessage,
            ExecutionDuration = duration,
            StartTime = executionTime,
            ExecutionContext = executionContext
        };
    }

    private async Task SaveExecutionResultsAsync(Core.Entities.Indicator indicator, IndicatorExecutionResult result, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Update indicator last run
            indicator.LastRun = result.StartTime;
            indicator.LastRunResult = result.WasSuccessful ? "Success" : result.ErrorMessage;

            // TODO: Save to new IndicatorsExecutionHistory table
            // Historical data saving is temporarily disabled since HistoricalData table is obsolete

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save execution results for indicator {IndicatorId}", indicator.IndicatorID);
        }
    }

    private async Task TriggerAlertAsync(Core.Entities.Indicator indicator, IndicatorExecutionResult result, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Create alert log
            var alertLog = new AlertLog
            {
                IndicatorId = indicator.IndicatorID,
                TriggerTime = result.StartTime,
                Message = $"Indicator '{indicator.IndicatorName}' threshold breached. " +
                         $"Current value: {result.Value}, Threshold: {result.ThresholdValue}",
                CurrentValue = result.Value,
                HistoricalValue = null, // TODO: Add historical value to IndicatorExecutionResult
                IsResolved = false,
                SentTo = indicator.OwnerContact?.Email ?? "Unknown",
                SentVia = 2 // Email
            };

            // Temporarily disable alert creation to fix database issue
            // _context.AlertLogs.Add(alertLog);

            // Note: Domain events will be raised by the indicator entity itself
            // when we call UpdateIndicatorAsync, so we don't need to add them here

            // await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Alert triggered for indicator {IndicatorId}: {IndicatorName}, " +
                "Value: {CurrentValue}, Threshold: {ThresholdValue}",
                indicator.IndicatorID, indicator.IndicatorName, result.Value, result.ThresholdValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger alert for indicator {IndicatorId}", indicator.IndicatorID);
        }
    }

    private static string GetSeverityFromPriority(string priority)
    {
        return priority.ToLower() switch
        {
            "high" => "Critical",
            "medium" => "Warning",
            "low" => "Info",
            _ => "Warning"
        };
    }
}
