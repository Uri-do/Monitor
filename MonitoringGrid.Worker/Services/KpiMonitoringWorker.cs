using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MonitoringGrid.Worker.Services;

/// <summary>
/// Background service responsible for monitoring and executing KPIs
/// </summary>
public class KpiMonitoringWorker : BackgroundService
{
    private readonly ILogger<KpiMonitoringWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerConfiguration _configuration;
    private readonly Meter _meter;
    private readonly Counter<int> _kpisProcessedCounter;
    private readonly Counter<int> _kpisFailedCounter;
    private readonly Histogram<double> _kpiExecutionDuration;

    public KpiMonitoringWorker(
        ILogger<KpiMonitoringWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerConfiguration> configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration.Value;
        
        _meter = new Meter("MonitoringGrid.Worker.KpiMonitoring");
        _kpisProcessedCounter = _meter.CreateCounter<int>("kpis_processed_total", "count", "Total number of KPIs processed");
        _kpisFailedCounter = _meter.CreateCounter<int>("kpis_failed_total", "count", "Total number of KPIs that failed");
        _kpiExecutionDuration = _meter.CreateHistogram<double>("kpi_execution_duration_seconds", "seconds", "Duration of KPI execution");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KPI Monitoring Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessKpisAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during KPI monitoring cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_configuration.KpiMonitoring.IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("KPI Monitoring Worker stopped");
    }

    private async Task ProcessKpisAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var kpiService = scope.ServiceProvider.GetRequiredService<IKpiService>();
        var kpiExecutionService = scope.ServiceProvider.GetRequiredService<IKpiExecutionService>();

        _logger.LogDebug("Starting KPI monitoring cycle");

        try
        {
            // Get KPIs that are due for execution
            var dueKpis = await GetDueKpisAsync(kpiService, cancellationToken);
            
            if (!dueKpis.Any())
            {
                _logger.LogDebug("No KPIs due for execution");
                return;
            }

            _logger.LogInformation("Found {Count} KPIs due for execution", dueKpis.Count);

            // Process KPIs in parallel with configured concurrency limit
            var semaphore = new SemaphoreSlim(_configuration.KpiMonitoring.MaxParallelKpis);
            var tasks = dueKpis.Select(async kpi =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await ProcessSingleKpiAsync(kpiExecutionService, kpi, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Completed KPI monitoring cycle. Processed {Count} KPIs", dueKpis.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KPI processing cycle");
            throw;
        }
    }

    private async Task<List<KPI>> GetDueKpisAsync(IKpiService kpiService, CancellationToken cancellationToken)
    {
        var allKpis = await kpiService.GetAllKpisAsync(cancellationToken);

        var dueKpis = allKpis.Where(kpi =>
        {
            // Only process active KPIs if configured
            if (_configuration.KpiMonitoring.ProcessOnlyActiveKpis && !kpi.IsActive)
                return false;

            // Skip KPIs that are currently running if configured
            if (_configuration.KpiMonitoring.SkipRunningKpis && kpi.IsCurrentlyRunning)
                return false;

            // Check if KPI is due for execution using whole time scheduling
            return MonitoringGrid.Infrastructure.Utilities.WholeTimeScheduler
                .IsKpiDueForWholeTimeExecution(kpi.LastRun, kpi.Frequency);
        }).ToList();

        return dueKpis;
    }

    private async Task ProcessSingleKpiAsync(IKpiExecutionService kpiExecutionService, KPI kpi, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Executing KPI {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.KpiMonitoring.ExecutionTimeoutSeconds));

            var result = await kpiExecutionService.ExecuteKpiAsync(kpi, timeoutCts.Token);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Successfully executed KPI {KpiId}: {Indicator}. Value: {Value}",
                    kpi.KpiId, kpi.Indicator, result.CurrentValue);
                _kpisProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
            }
            else
            {
                _logger.LogWarning("KPI execution failed for {KpiId}: {Indicator}. Error: {Error}",
                    kpi.KpiId, kpi.Indicator, result.ErrorMessage);
                _kpisFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("KPI execution cancelled for {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("KPI execution timed out for {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
            _kpisFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "timeout"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
            _kpisFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
        }
        finally
        {
            stopwatch.Stop();
            _kpiExecutionDuration.Record(stopwatch.Elapsed.TotalSeconds, 
                new KeyValuePair<string, object?>("kpi_id", kpi.KpiId.ToString()));
        }
    }

    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}
