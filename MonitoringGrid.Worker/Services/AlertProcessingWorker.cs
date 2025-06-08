using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;

namespace MonitoringGrid.Worker.Services;

/// <summary>
/// Background service responsible for processing alerts, escalations, and auto-resolution
/// </summary>
public class AlertProcessingWorker : BackgroundService
{
    private readonly ILogger<AlertProcessingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerConfiguration _configuration;
    private readonly Meter _meter;
    private readonly Counter<int> _alertsProcessedCounter;
    private readonly Counter<int> _alertsEscalatedCounter;
    private readonly Counter<int> _alertsResolvedCounter;

    public AlertProcessingWorker(
        ILogger<AlertProcessingWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerConfiguration> configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration.Value;
        
        _meter = new Meter("MonitoringGrid.Worker.AlertProcessing");
        _alertsProcessedCounter = _meter.CreateCounter<int>("alerts_processed_total", "count", "Total number of alerts processed");
        _alertsEscalatedCounter = _meter.CreateCounter<int>("alerts_escalated_total", "count", "Total number of alerts escalated");
        _alertsResolvedCounter = _meter.CreateCounter<int>("alerts_resolved_total", "count", "Total number of alerts auto-resolved");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert Processing Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during alert processing cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_configuration.AlertProcessing.IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Alert Processing Worker stopped");
    }

    private async Task ProcessAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

        _logger.LogDebug("Starting alert processing cycle");

        try
        {
            // Get unresolved alerts
            var unresolvedAlerts = await GetUnresolvedAlertsAsync(alertService, cancellationToken);
            
            if (!unresolvedAlerts.Any())
            {
                _logger.LogDebug("No unresolved alerts to process");
                return;
            }

            _logger.LogInformation("Processing {Count} unresolved alerts", unresolvedAlerts.Count);

            // Process alerts in batches
            var batches = unresolvedAlerts
                .Select((alert, index) => new { alert, index })
                .GroupBy(x => x.index / _configuration.AlertProcessing.BatchSize)
                .Select(g => g.Select(x => x.alert).ToList());

            foreach (var batch in batches)
            {
                await ProcessAlertBatchAsync(alertService, batch, cancellationToken);
            }

            _logger.LogInformation("Completed alert processing cycle");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during alert processing cycle");
            throw;
        }
    }

    private async Task<List<AlertLog>> GetUnresolvedAlertsAsync(IAlertService alertService, CancellationToken cancellationToken)
    {
        // Get unresolved alerts from the database directly
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        return await context.AlertLogs
            .Where(a => !a.IsResolved && a.TriggerTime >= cutoffTime)
            .OrderBy(a => a.TriggerTime)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessAlertBatchAsync(IAlertService alertService, List<AlertLog> alerts, CancellationToken cancellationToken)
    {
        var tasks = alerts.Select(alert => ProcessSingleAlertAsync(alertService, alert, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleAlertAsync(IAlertService alertService, AlertLog alert, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing alert {AlertId} for KPI {KpiId}", alert.AlertId, alert.KpiId);

            var processed = false;

            // Check for auto-resolution based on age
            if (_configuration.AlertProcessing.EnableAutoResolution)
            {
                var autoResolutionThreshold = DateTime.UtcNow.AddMinutes(-_configuration.AlertProcessing.AutoResolutionTimeoutMinutes);
                if (alert.TriggerTime <= autoResolutionThreshold)
                {
                    await AutoResolveAlertAsync(alertService, alert, cancellationToken);
                    processed = true;
                }
            }

            // Log escalation need (without actual escalation since the entity doesn't support it)
            if (_configuration.AlertProcessing.EnableEscalation)
            {
                var escalationThreshold = DateTime.UtcNow.AddMinutes(-_configuration.AlertProcessing.EscalationTimeoutMinutes);
                if (alert.TriggerTime <= escalationThreshold)
                {
                    _logger.LogWarning("Alert {AlertId} for KPI {KpiId} requires escalation - unresolved for {Minutes} minutes",
                        alert.AlertId, alert.KpiId, _configuration.AlertProcessing.EscalationTimeoutMinutes);
                    _alertsEscalatedCounter.Add(1, new KeyValuePair<string, object?>("kpi_id", alert.KpiId.ToString()));
                    processed = true;
                }
            }

            if (processed)
            {
                _alertsProcessedCounter.Add(1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alert {AlertId}", alert.AlertId);
        }
    }



    private async Task AutoResolveAlertAsync(IAlertService alertService, AlertLog alert, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Auto-resolving alert {AlertId} for KPI {KpiId} - timeout reached after {Minutes} minutes",
                alert.AlertId, alert.KpiId, _configuration.AlertProcessing.AutoResolutionTimeoutMinutes);

            // Check if the underlying issue is still present
            var shouldResolve = await ShouldAutoResolveAlertAsync(alert, cancellationToken);

            if (shouldResolve)
            {
                // Use the existing ResolveAlertAsync method from IAlertService
                var resolved = await alertService.ResolveAlertAsync(alert.AlertId, "System", "Auto-resolved due to timeout", cancellationToken);

                if (resolved)
                {
                    _alertsResolvedCounter.Add(1, new KeyValuePair<string, object?>("kpi_id", alert.KpiId.ToString()));
                    _logger.LogInformation("Alert {AlertId} auto-resolved successfully", alert.AlertId);
                }
                else
                {
                    _logger.LogWarning("Failed to auto-resolve alert {AlertId}", alert.AlertId);
                }
            }
            else
            {
                _logger.LogWarning("Alert {AlertId} not auto-resolved - underlying issue still present", alert.AlertId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-resolve alert {AlertId}", alert.AlertId);
            throw;
        }
    }

    private async Task<bool> ShouldAutoResolveAlertAsync(AlertLog alert, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var kpiExecutionService = scope.ServiceProvider.GetRequiredService<IKpiExecutionService>();

            // Re-execute the KPI to check current status
            // Get the KPI first
            using var kpiScope = _serviceProvider.CreateScope();
            var kpiService = kpiScope.ServiceProvider.GetRequiredService<IKpiService>();
            var kpi = await kpiService.GetKpiByIdAsync(alert.KpiId, cancellationToken);

            if (kpi == null)
            {
                _logger.LogWarning("KPI {KpiId} not found for alert {AlertId}", alert.KpiId, alert.AlertId);
                return false;
            }

            var result = await kpiExecutionService.ExecuteKpiAsync(kpi, cancellationToken);
            
            if (!result.IsSuccessful)
            {
                _logger.LogWarning("Cannot determine auto-resolution for alert {AlertId} - KPI execution failed", alert.AlertId);
                return false; // Don't auto-resolve if we can't determine current state
            }

            // Simple logic: if the current value is within acceptable range, resolve the alert
            // In a real implementation, this would be more sophisticated based on alert type and thresholds
            var currentValue = result.CurrentValue;
            
            // For demonstration, assume alerts are triggered when values are outside normal range
            // This would be replaced with actual business logic
            var isWithinNormalRange = currentValue > 0 && currentValue < 1000; // Example thresholds
            
            return isWithinNormalRange;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auto-resolution condition for alert {AlertId}", alert.AlertId);
            return false; // Don't auto-resolve on error
        }
    }

    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}
