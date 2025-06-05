using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid;

/// <summary>
/// Background service that continuously monitors KPIs and sends alerts
/// </summary>
public class MonitoringWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MonitoringConfiguration _config;
    private readonly ILogger<MonitoringWorker> _logger;
    private readonly SemaphoreSlim _semaphore;

    public MonitoringWorker(
        IServiceProvider serviceProvider,
        IOptions<MonitoringConfiguration> config,
        ILogger<MonitoringWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_config.MaxParallelExecutions, _config.MaxParallelExecutions);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring Worker started with {MaxParallel} max parallel executions", 
            _config.MaxParallelExecutions);

        // Initial delay to allow system to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessKpisAsync(stoppingToken);
                await UpdateSystemStatusAsync(stoppingToken);
                await CleanupOldDataAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Monitoring Worker is stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring worker main loop: {Message}", ex.Message);
            }

            // Wait for the configured interval before next execution
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.ServiceIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Monitoring Worker stopped");
    }

    /// <summary>
    /// Processes all due KPIs in parallel
    /// </summary>
    private async Task ProcessKpisAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

        // Get KPIs that are due for execution
        var dueKpis = await GetDueKpisAsync(context, cancellationToken);
        
        if (!dueKpis.Any())
        {
            _logger.LogDebug("No KPIs are due for execution");
            return;
        }

        _logger.LogInformation("Processing {KpiCount} due KPIs", dueKpis.Count);

        // Process KPIs in parallel with controlled concurrency
        var tasks = dueKpis.Select(kpi => ProcessSingleKpiAsync(kpi, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogDebug("Completed processing {KpiCount} KPIs", dueKpis.Count);
    }

    /// <summary>
    /// Gets KPIs that are due for execution based on frequency and last run time
    /// </summary>
    private async Task<List<KPI>> GetDueKpisAsync(MonitoringContext context, CancellationToken cancellationToken)
    {
        var currentTime = DateTime.UtcNow;
        
        return await context.KPIs
            .Where(k => k.IsActive && 
                       (k.LastRun == null || 
                        k.LastRun < currentTime.AddMinutes(-k.Frequency)))
            .OrderBy(k => k.LastRun ?? DateTime.MinValue) // Process oldest first
            .Take(_config.BatchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Processes a single KPI with proper error handling and concurrency control
    /// </summary>
    private async Task ProcessSingleKpiAsync(KPI kpi, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
            var kpiExecutionService = scope.ServiceProvider.GetRequiredService<IKpiExecutionService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

            _logger.LogDebug("Processing KPI: {Indicator}", kpi.Indicator);

            // Execute the KPI
            var executionResult = await kpiExecutionService.ExecuteKpiAsync(kpi, cancellationToken);

            // Update last run time regardless of execution result
            kpi.LastRun = DateTime.UtcNow;
            context.Entry(kpi).State = EntityState.Modified;

            if (!executionResult.IsSuccessful)
            {
                _logger.LogWarning("KPI {Indicator} execution failed: {Error}", kpi.Indicator, executionResult.ErrorMessage);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            // Check if alert should be triggered
            if (executionResult.ShouldAlert)
            {
                // Check cooldown period
                var inCooldown = await alertService.IsInCooldownAsync(kpi, cancellationToken);
                if (inCooldown)
                {
                    _logger.LogDebug("KPI {Indicator} alert skipped due to cooldown period", kpi.Indicator);
                }
                else
                {
                    // Send alerts
                    var alertResult = await alertService.SendAlertsAsync(kpi, executionResult, cancellationToken);
                    
                    // Log the alert
                    await alertService.LogAlertAsync(kpi, alertResult, executionResult, cancellationToken);
                    
                    if (alertResult.Success)
                    {
                        _logger.LogInformation("Alert sent successfully for KPI {Indicator}", kpi.Indicator);
                    }
                    else
                    {
                        _logger.LogWarning("Alert sending had errors for KPI {Indicator}: {Errors}", 
                            kpi.Indicator, string.Join(", ", alertResult.Errors));
                    }
                }
            }
            else
            {
                _logger.LogDebug("KPI {Indicator} within normal parameters - no alert needed", kpi.Indicator);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing KPI {Indicator}: {Message}", kpi.Indicator, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Updates system status for monitoring and health checks
    /// </summary>
    private async Task UpdateSystemStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

            var status = await context.SystemStatus
                .FirstOrDefaultAsync(s => s.ServiceName == "MonitoringWorker", cancellationToken);

            if (status == null)
            {
                status = new SystemStatus
                {
                    ServiceName = "MonitoringWorker",
                    Status = "Running",
                    LastHeartbeat = DateTime.UtcNow
                };
                context.SystemStatus.Add(status);
            }
            else
            {
                status.LastHeartbeat = DateTime.UtcNow;
                status.Status = "Running";
                status.ErrorMessage = null;
            }

            // Update statistics
            var processedToday = await context.KPIs
                .Where(k => k.LastRun != null && k.LastRun >= DateTime.UtcNow.Date)
                .CountAsync(cancellationToken);

            var alertsToday = await context.AlertLogs
                .Where(a => a.TriggerTime >= DateTime.UtcNow.Date)
                .CountAsync(cancellationToken);

            status.ProcessedKpis = processedToday;
            status.AlertsSent = alertsToday;

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system status: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Cleans up old data based on retention policies
    /// </summary>
    private async Task CleanupOldDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-_config.MaxAlertHistoryDays);

            // Clean up old alert logs
            var oldAlerts = await context.AlertLogs
                .Where(a => a.TriggerTime < cutoffDate && a.IsResolved)
                .Take(1000) // Process in batches
                .ToListAsync(cancellationToken);

            if (oldAlerts.Any())
            {
                context.AlertLogs.RemoveRange(oldAlerts);
                await context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {Count} old alert logs", oldAlerts.Count);
            }

            // Clean up old historical data (keep last 3 months)
            var historicalCutoff = DateTime.UtcNow.AddDays(-90);
            var oldHistorical = await context.HistoricalData
                .Where(h => h.Timestamp < historicalCutoff)
                .Take(1000) // Process in batches
                .ToListAsync(cancellationToken);

            if (oldHistorical.Any())
            {
                context.HistoricalData.RemoveRange(oldHistorical);
                await context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {Count} old historical data records", oldHistorical.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data cleanup: {Message}", ex.Message);
        }
    }

    public override void Dispose()
    {
        _semaphore?.Dispose();
        base.Dispose();
    }
}
