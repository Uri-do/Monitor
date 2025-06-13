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
/// Health check for Indicator execution functionality
/// </summary>
public class IndicatorExecutionHealthCheck : IHealthCheck
{
    private readonly ILogger<IndicatorExecutionHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public IndicatorExecutionHealthCheck(
        ILogger<IndicatorExecutionHealthCheck> logger,
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
            var indicatorService = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Core.Interfaces.IIndicatorService>();

            // Check if we can retrieve Indicators
            var indicators = await indicatorService.GetAllIndicatorsAsync(cancellationToken);
            var activeIndicators = indicators.Count(i => i.IsActive);
            var totalIndicators = indicators.Count();

            // Check for recent executions
            var recentExecutions = indicators.Count(i => i.LastRun.HasValue && i.LastRun.Value > DateTime.UtcNow.AddHours(-1));

            var data = new Dictionary<string, object>
            {
                ["total_indicators"] = totalIndicators,
                ["active_indicators"] = activeIndicators,
                ["recent_executions"] = recentExecutions,
                ["last_check"] = DateTime.UtcNow
            };

            if (totalIndicators == 0)
            {
                return HealthCheckResult.Degraded("No Indicators configured", data: data);
            }

            if (activeIndicators == 0)
            {
                return HealthCheckResult.Degraded("No active Indicators found", data: data);
            }

            return HealthCheckResult.Healthy($"Indicator execution system operational. {activeIndicators}/{totalIndicators} Indicators active, {recentExecutions} recent executions", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indicator execution health check failed");
            return HealthCheckResult.Unhealthy("Indicator execution system unavailable", ex);
        }
    }
}


