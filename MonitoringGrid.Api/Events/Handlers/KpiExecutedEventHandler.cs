using MonitoringGrid.Api.Events;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Events.Handlers;

/// <summary>
/// Handler for KPI executed events
/// </summary>
public class KpiExecutedEventHandler : DomainEventNotificationHandler<KpiExecutedEvent>
{
    private readonly ILogger<KpiExecutedEventHandler> _logger;
    private readonly MetricsService _metricsService;
    private readonly IUnitOfWork _unitOfWork;

    public KpiExecutedEventHandler(
        ILogger<KpiExecutedEventHandler> logger,
        MetricsService metricsService,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _metricsService = metricsService;
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleDomainEvent(KpiExecutedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling KPI executed event for KPI {KpiId} - {Indicator} (Success: {WasSuccessful})",
            domainEvent.KpiId, domainEvent.Indicator, domainEvent.WasSuccessful);

        try
        {
            // Update metrics
            await UpdateMetricsAsync(domainEvent);

            // Update KPI statistics
            await UpdateKpiStatisticsAsync(domainEvent, cancellationToken);

            // Log execution details
            LogExecutionDetails(domainEvent);

            _logger.LogDebug("Successfully handled KPI executed event for KPI {KpiId}", domainEvent.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling KPI executed event for KPI {KpiId}", domainEvent.KpiId);
            throw;
        }
    }

    private async Task UpdateMetricsAsync(KpiExecutedEvent domainEvent)
    {
        // Record execution metrics
        _metricsService.RecordKpiExecution(
            domainEvent.Indicator,
            domainEvent.Owner,
            0, // Duration will be recorded elsewhere
            domainEvent.WasSuccessful);

        // Update system health metrics if needed
        if (!domainEvent.WasSuccessful)
        {
            _logger.LogWarning("KPI execution failed for {Indicator}", domainEvent.Indicator);
        }

        await Task.CompletedTask;
    }

    private async Task UpdateKpiStatisticsAsync(KpiExecutedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Update KPI execution statistics
        // This could involve updating success rates, average execution times, etc.
        
        // For now, we'll just log the statistics update
        _logger.LogDebug("Updating statistics for KPI {KpiId} - Success: {WasSuccessful}, Current: {CurrentValue}, Historical: {HistoricalValue}",
            domainEvent.KpiId, domainEvent.WasSuccessful, domainEvent.CurrentValue, domainEvent.HistoricalValue);

        // In a real implementation, you might:
        // 1. Update a KPI statistics table
        // 2. Calculate rolling averages
        // 3. Update success/failure counters
        // 4. Trigger additional business logic

        await Task.CompletedTask;
    }

    private void LogExecutionDetails(KpiExecutedEvent domainEvent)
    {
        if (domainEvent.WasSuccessful)
        {
            if (domainEvent.CurrentValue.HasValue && domainEvent.HistoricalValue.HasValue)
            {
                var deviation = Math.Abs(domainEvent.CurrentValue.Value - domainEvent.HistoricalValue.Value);
                var deviationPercent = domainEvent.HistoricalValue.Value != 0 
                    ? (deviation / domainEvent.HistoricalValue.Value) * 100 
                    : 0;

                _logger.LogInformation("KPI {Indicator} executed successfully - Current: {CurrentValue}, Historical: {HistoricalValue}, Deviation: {DeviationPercent:F2}%",
                    domainEvent.Indicator, domainEvent.CurrentValue, domainEvent.HistoricalValue, deviationPercent);
            }
            else
            {
                _logger.LogInformation("KPI {Indicator} executed successfully", domainEvent.Indicator);
            }
        }
        else
        {
            _logger.LogWarning("KPI {Indicator} execution failed: {ErrorMessage}",
                domainEvent.Indicator, domainEvent.ErrorMessage ?? "Unknown error");
        }
    }
}
