using MonitoringGrid.Worker.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;

namespace MonitoringGrid.Worker.Services;

/// <summary>
/// Background service responsible for monitoring system health
/// </summary>
public class HealthCheckWorker : BackgroundService
{
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly WorkerConfiguration _configuration;
    private readonly Meter _meter;
    private readonly Counter<int> _healthStatusGauge;

    public HealthCheckWorker(
        ILogger<HealthCheckWorker> logger,
        HealthCheckService healthCheckService,
        IOptions<WorkerConfiguration> configuration)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _configuration = configuration.Value;
        
        _meter = new Meter("MonitoringGrid.Worker.HealthCheck");
        _healthStatusGauge = _meter.CreateCounter<int>("health_status", "status", "Current health status (0=Unhealthy, 1=Degraded, 2=Healthy)");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health Check Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during health check cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_configuration.HealthChecks.IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Health Check Worker stopped");
    }

    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting health check cycle");

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.HealthChecks.TimeoutSeconds));

            var healthReport = await _healthCheckService.CheckHealthAsync(timeoutCts.Token);

            // Log overall health status
            _logger.LogInformation("Health check completed. Status: {Status}, Duration: {Duration}ms", 
                healthReport.Status, healthReport.TotalDuration.TotalMilliseconds);

            // Update metrics
            var statusValue = healthReport.Status switch
            {
                HealthStatus.Healthy => 2,
                HealthStatus.Degraded => 1,
                HealthStatus.Unhealthy => 0,
                _ => 0
            };
            _healthStatusGauge.Add(statusValue);

            // Log individual check results
            foreach (var entry in healthReport.Entries)
            {
                var logLevel = entry.Value.Status switch
                {
                    HealthStatus.Healthy => LogLevel.Debug,
                    HealthStatus.Degraded => LogLevel.Warning,
                    HealthStatus.Unhealthy => LogLevel.Error,
                    _ => LogLevel.Information
                };

                _logger.Log(logLevel, "Health check '{CheckName}': {Status} ({Duration}ms) - {Description}",
                    entry.Key, entry.Value.Status, entry.Value.Duration.TotalMilliseconds,
                    entry.Value.Description ?? "No description");

                if (entry.Value.Exception != null)
                {
                    _logger.LogError(entry.Value.Exception, "Health check '{CheckName}' failed with exception", entry.Key);
                }
            }

            // Alert on unhealthy status
            if (healthReport.Status == HealthStatus.Unhealthy)
            {
                _logger.LogCritical("System health is UNHEALTHY. Immediate attention required!");
            }
            else if (healthReport.Status == HealthStatus.Degraded)
            {
                _logger.LogWarning("System health is DEGRADED. Performance may be impacted.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Health check cancelled");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check timed out");
            _healthStatusGauge.Add(0); // Mark as unhealthy on timeout
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check execution");
            _healthStatusGauge.Add(0); // Mark as unhealthy on error
        }
    }

    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Health check for KPI execution functionality
/// </summary>
public class KpiExecutionHealthCheck : IHealthCheck
{
    private readonly ILogger<KpiExecutionHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public KpiExecutionHealthCheck(
        ILogger<KpiExecutionHealthCheck> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var kpiService = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Core.Interfaces.IKpiService>();

            // Check if we can retrieve KPIs
            var kpis = await kpiService.GetAllKpisAsync(cancellationToken);
            var activeKpis = kpis.Count(k => k.IsActive);
            var totalKpis = kpis.Count;

            // Check for recent executions
            var recentExecutions = kpis.Count(k => k.LastRun.HasValue && k.LastRun.Value > DateTime.UtcNow.AddHours(-1));

            var data = new Dictionary<string, object>
            {
                ["total_kpis"] = totalKpis,
                ["active_kpis"] = activeKpis,
                ["recent_executions"] = recentExecutions,
                ["last_check"] = DateTime.UtcNow
            };

            if (totalKpis == 0)
            {
                return HealthCheckResult.Degraded("No KPIs configured", data: data);
            }

            if (activeKpis == 0)
            {
                return HealthCheckResult.Degraded("No active KPIs found", data: data);
            }

            return HealthCheckResult.Healthy($"KPI execution system operational. {activeKpis}/{totalKpis} KPIs active, {recentExecutions} recent executions", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KPI execution health check failed");
            return HealthCheckResult.Unhealthy("KPI execution system unavailable", ex);
        }
    }
}

/// <summary>
/// Health check for alert processing functionality
/// </summary>
public class AlertProcessingHealthCheck : IHealthCheck
{
    private readonly ILogger<AlertProcessingHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AlertProcessingHealthCheck(
        ILogger<AlertProcessingHealthCheck> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var alertService = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Core.Interfaces.IAlertService>();

            // Check if we can retrieve alerts from database directly
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            var recentAlerts = await dbContext.AlertLogs
                .Where(a => a.TriggerTime >= cutoffTime)
                .ToListAsync(cancellationToken);

            var activeAlerts = recentAlerts.Count(a => !a.IsResolved);
            var totalRecentAlerts = recentAlerts.Count;

            var data = new Dictionary<string, object>
            {
                ["active_alerts"] = activeAlerts,
                ["total_recent_alerts"] = totalRecentAlerts,
                ["last_check"] = DateTime.UtcNow
            };

            if (activeAlerts > 100)
            {
                return HealthCheckResult.Degraded($"High number of active alerts: {activeAlerts}", data: data);
            }

            return HealthCheckResult.Healthy($"Alert processing system operational. {activeAlerts} active alerts, {totalRecentAlerts} recent alerts", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert processing health check failed");
            return HealthCheckResult.Unhealthy("Alert processing system unavailable", ex);
        }
    }
}
