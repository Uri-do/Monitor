using MonitoringGrid.Api.Events;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Events.Handlers;

/// <summary>
/// Handler for KPI threshold breached events - triggers alerts
/// </summary>
public class KpiThresholdBreachedEventHandler : DomainEventNotificationHandler<KpiThresholdBreachedEvent>
{
    private readonly ILogger<KpiThresholdBreachedEventHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public KpiThresholdBreachedEventHandler(
        ILogger<KpiThresholdBreachedEventHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleDomainEvent(KpiThresholdBreachedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("KPI threshold breached for {Indicator} - Current: {CurrentValue}, Historical: {HistoricalValue}, Deviation: {Deviation:F2}%, Severity: {Severity}",
            domainEvent.Indicator, domainEvent.CurrentValue, domainEvent.HistoricalValue, domainEvent.Deviation, domainEvent.Severity);

        try
        {
            // Create alert log entry
            await CreateAlertLogAsync(domainEvent, cancellationToken);

            // Determine if we should send notifications based on severity
            var severityEnum = ParseSeverity(domainEvent.Severity);
            if (ShouldSendNotification(severityEnum))
            {
                await TriggerNotificationAsync(domainEvent, cancellationToken);
            }

            _logger.LogInformation("Successfully handled KPI threshold breached event for KPI {KpiId}", domainEvent.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling KPI threshold breached event for KPI {KpiId}", domainEvent.KpiId);
            throw;
        }
    }

    private async Task CreateAlertLogAsync(KpiThresholdBreachedEvent domainEvent, CancellationToken cancellationToken)
    {
        var alertRepository = _unitOfWork.Repository<AlertLog>();

        var alertLog = new AlertLog
        {
            KpiId = domainEvent.KpiId,
            TriggerTime = domainEvent.OccurredOn,
            CurrentValue = domainEvent.CurrentValue,
            HistoricalValue = domainEvent.HistoricalValue,
            DeviationPercent = domainEvent.Deviation,
            Message = $"KPI {domainEvent.Indicator} threshold breached. Current value {domainEvent.CurrentValue} deviates {domainEvent.Deviation:F2}% from historical value {domainEvent.HistoricalValue}.",
            IsResolved = false,
            SentTo = "System", // Default value
            SentVia = 2 // Email
        };

        await alertRepository.AddAsync(alertLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created alert log entry {AlertId} for KPI {KpiId} threshold breach", 
            alertLog.AlertId, domainEvent.KpiId);
    }

    private async Task TriggerNotificationAsync(KpiThresholdBreachedEvent domainEvent, CancellationToken cancellationToken)
    {
        // In a real implementation, this would:
        // 1. Look up notification preferences for the KPI owner
        // 2. Send email/SMS notifications based on severity
        // 3. Create dashboard notifications
        // 4. Potentially escalate to management for critical alerts

        _logger.LogInformation("Triggering {Severity} notification for KPI {Indicator} threshold breach",
            domainEvent.Severity, domainEvent.Indicator);

        // Simulate notification sending
        await Task.Delay(100, cancellationToken);

        _logger.LogDebug("Notification sent for KPI {KpiId} threshold breach", domainEvent.KpiId);
    }

    private static AlertSeverity ParseSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "low" => AlertSeverity.Low,
            "medium" => AlertSeverity.Medium,
            "high" => AlertSeverity.High,
            "critical" => AlertSeverity.Critical,
            "emergency" => AlertSeverity.Emergency,
            _ => AlertSeverity.Low
        };
    }

    private static bool ShouldSendNotification(AlertSeverity severity)
    {
        // Send notifications for Medium and High severity alerts
        return severity is AlertSeverity.Medium or AlertSeverity.High or AlertSeverity.Critical or AlertSeverity.Emergency;
    }
}
