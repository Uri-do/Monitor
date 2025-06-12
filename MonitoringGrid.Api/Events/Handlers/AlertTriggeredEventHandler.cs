using MonitoringGrid.Api.Events;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Api.Events.Handlers;

/// <summary>
/// Handler for alert triggered events
/// </summary>
public class AlertTriggeredEventHandler : DomainEventNotificationHandler<AlertTriggeredEvent>
{
    private readonly ILogger<AlertTriggeredEventHandler> _logger;

    public AlertTriggeredEventHandler(ILogger<AlertTriggeredEventHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task HandleDomainEvent(AlertTriggeredEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Handling alert triggered event - Alert {AlertId} for KPI {KpiId}, Severity: {Severity}",
            domainEvent.AlertId, domainEvent.IndicatorId, domainEvent.Severity);

        try
        {
            // Log alert details for audit
            LogAlertDetails(domainEvent);

            // Update alert metrics
            await UpdateAlertMetricsAsync(domainEvent);

            // TODO: Send real-time notification to dashboard when SignalR is properly integrated
            _logger.LogDebug("Real-time notification would be sent for alert {AlertId}", domainEvent.AlertId);

            _logger.LogInformation("Successfully handled alert triggered event for Alert {AlertId}", domainEvent.AlertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling alert triggered event for Alert {AlertId}", domainEvent.AlertId);
            throw;
        }
    }



    private void LogAlertDetails(AlertTriggeredEvent domainEvent)
    {
        _logger.LogWarning("ALERT TRIGGERED - ID: {AlertId}, KPI: {KpiId}, Severity: {Severity}, " +
            "Current: {CurrentValue}, Historical: {HistoricalValue}, Deviation: {Deviation:F2}%",
            domainEvent.AlertId,
            domainEvent.IndicatorId,
            domainEvent.Severity,
            domainEvent.CurrentValue,
            domainEvent.HistoricalValue,
            domainEvent.Deviation);
    }

    private async Task UpdateAlertMetricsAsync(AlertTriggeredEvent domainEvent)
    {
        // Update alert metrics for monitoring and reporting
        // This could include:
        // 1. Incrementing alert counters
        // 2. Updating severity distribution
        // 3. Recording alert frequency
        // 4. Updating dashboard statistics

        _logger.LogDebug("Updating alert metrics for severity {Severity}", domainEvent.Severity);

        // In a real implementation, you might:
        // 1. Update Prometheus metrics
        // 2. Send to monitoring systems
        // 3. Update dashboard counters
        // 4. Record in time-series database

        await Task.CompletedTask;
    }
}
