using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR.Client;

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
    private HubConnection? _hubConnection;
    private Timer? _countdownTimer;

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

        // Initialize SignalR connection
        await InitializeSignalRConnectionAsync();

        // Start countdown timer for real-time updates
        StartCountdownTimer();

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

        // Cleanup
        _countdownTimer?.Dispose();
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
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
            return MonitoringGrid.Core.Utilities.WholeTimeScheduler
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

            // Broadcast KPI execution started
            await BroadcastKpiExecutionStartedAsync(kpi);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.KpiMonitoring.ExecutionTimeoutSeconds));

            var result = await kpiExecutionService.ExecuteKpiAsync(kpi, timeoutCts.Token);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Successfully executed KPI {KpiId}: {Indicator}. Value: {Value}",
                    kpi.KpiId, kpi.Indicator, result.CurrentValue);
                _kpisProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

                // Broadcast successful completion
                await BroadcastKpiExecutionCompletedAsync(kpi, true, result.CurrentValue, stopwatch.Elapsed);
            }
            else
            {
                _logger.LogWarning("KPI execution failed for {KpiId}: {Indicator}. Error: {Error}",
                    kpi.KpiId, kpi.Indicator, result.ErrorMessage);
                _kpisFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));

                // Broadcast failed completion
                await BroadcastKpiExecutionCompletedAsync(kpi, false, null, stopwatch.Elapsed, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("KPI execution cancelled for {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
            await BroadcastKpiExecutionCompletedAsync(kpi, false, null, stopwatch.Elapsed, "Execution cancelled");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("KPI execution timed out for {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
            _kpisFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "timeout"));
            await BroadcastKpiExecutionCompletedAsync(kpi, false, null, stopwatch.Elapsed, "Execution timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI {KpiId}: {Indicator}", kpi.KpiId, kpi.Indicator);
            _kpisFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
            await BroadcastKpiExecutionCompletedAsync(kpi, false, null, stopwatch.Elapsed, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            _kpiExecutionDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("kpi_id", kpi.KpiId.ToString()));
        }
    }

    private async Task BroadcastKpiExecutionStartedAsync(KPI kpi)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var executionStarted = new
            {
                KpiId = kpi.KpiId,
                Indicator = kpi.Indicator,
                Owner = kpi.Owner,
                StartTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EstimatedDuration = (int?)null // Could be calculated based on historical data
            };

            await _hubConnection.SendAsync("KpiExecutionStarted", executionStarted);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error broadcasting KPI execution started for {KpiId}", kpi.KpiId);
        }
    }

    private async Task BroadcastKpiExecutionCompletedAsync(KPI kpi, bool success, decimal? value, TimeSpan duration, string? errorMessage = null)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var executionCompleted = new
            {
                KpiId = kpi.KpiId,
                Indicator = kpi.Indicator,
                Success = success,
                Value = value,
                Duration = (int)duration.TotalSeconds,
                CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = errorMessage
            };

            await _hubConnection.SendAsync("KpiExecutionCompleted", executionCompleted);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error broadcasting KPI execution completed for {KpiId}", kpi.KpiId);
        }
    }

    private async Task InitializeSignalRConnectionAsync()
    {
        try
        {
            // Get API base URL from configuration or use default
            var apiBaseUrl = _configuration.ApiBaseUrl ?? "https://localhost:7001";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{apiBaseUrl}/monitoring-hub")
                .WithAutomaticReconnect()
                .Build();

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established for real-time updates");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to establish SignalR connection. Real-time updates will be disabled.");
        }
    }

    private void StartCountdownTimer()
    {
        _countdownTimer = new Timer(async _ => await SendCountdownUpdateAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private async Task SendCountdownUpdateAsync()
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var kpiService = scope.ServiceProvider.GetRequiredService<IKpiService>();

            var now = DateTime.UtcNow;
            var allKpis = await kpiService.GetAllKpisAsync(CancellationToken.None);

            var nextKpi = allKpis
                .Where(kpi => kpi.IsActive && !kpi.IsCurrentlyRunning)
                .Select(kpi => new
                {
                    Kpi = kpi,
                    NextDue = kpi.LastRun.HasValue ?
                        MonitoringGrid.Core.Utilities.WholeTimeScheduler.GetNextWholeTimeExecution(kpi.Frequency, kpi.LastRun.Value) :
                        MonitoringGrid.Core.Utilities.WholeTimeScheduler.GetNextWholeTimeExecution(kpi.Frequency, now),
                    IsDue = !kpi.LastRun.HasValue ||
                        MonitoringGrid.Core.Utilities.WholeTimeScheduler.IsKpiDueForWholeTimeExecution(kpi.LastRun, kpi.Frequency, now)
                })
                .Where(x => !x.IsDue)
                .OrderBy(x => x.NextDue)
                .FirstOrDefault();

            if (nextKpi != null)
            {
                var secondsUntilDue = Math.Max(0, (int)(nextKpi.NextDue - now).TotalSeconds);

                var countdownUpdate = new
                {
                    NextKpiId = nextKpi.Kpi.KpiId,
                    Indicator = nextKpi.Kpi.Indicator,
                    Owner = nextKpi.Kpi.Owner,
                    SecondsUntilDue = secondsUntilDue,
                    ScheduledTime = nextKpi.NextDue.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                await _hubConnection.SendAsync("CountdownUpdate", countdownUpdate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error sending countdown update");
        }
    }

    public override void Dispose()
    {
        _countdownTimer?.Dispose();
        _hubConnection?.DisposeAsync();
        _meter?.Dispose();
        base.Dispose();
    }
}
