using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR.Client;

namespace MonitoringGrid.Worker.Services;

/// <summary>
/// Background service responsible for monitoring and executing Indicators
/// Replaces KpiMonitoringWorker with enhanced functionality for ProgressPlayDB integration
/// </summary>
public class IndicatorMonitoringWorker : BackgroundService
{
    private readonly ILogger<IndicatorMonitoringWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerConfiguration _configuration;
    private readonly Meter _meter;
    private readonly Counter<int> _indicatorsProcessedCounter;
    private readonly Counter<int> _indicatorsFailedCounter;
    private readonly Histogram<double> _indicatorExecutionDuration;
    private HubConnection? _hubConnection;
    private Timer? _countdownTimer;

    public IndicatorMonitoringWorker(
        ILogger<IndicatorMonitoringWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerConfiguration> configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration.Value;
        
        _meter = new Meter("MonitoringGrid.Worker.IndicatorMonitoring");
        _indicatorsProcessedCounter = _meter.CreateCounter<int>("indicators_processed_total", "count", "Total number of Indicators processed");
        _indicatorsFailedCounter = _meter.CreateCounter<int>("indicators_failed_total", "count", "Total number of Indicators that failed");
        _indicatorExecutionDuration = _meter.CreateHistogram<double>("indicator_execution_duration_seconds", "seconds", "Duration of Indicator execution");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🚀 IndicatorMonitoringWorker.ExecuteAsync() called - Starting up...");
            _logger.LogInformation("Indicator Monitoring Worker started - Beginning execution loop");
            _logger.LogInformation("Worker Configuration: IntervalSeconds={IntervalSeconds}, MaxParallelIndicators={MaxParallelIndicators}, ProcessOnlyActiveIndicators={ProcessOnlyActiveIndicators}",
                _configuration.IndicatorMonitoring.IntervalSeconds,
                _configuration.IndicatorMonitoring.MaxParallelIndicators,
                _configuration.IndicatorMonitoring.ProcessOnlyActiveIndicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during IndicatorMonitoringWorker initial setup");
            throw;
        }

        // Initialize SignalR connection (temporarily disabled for debugging)
        // await InitializeSignalRConnectionAsync();

        // Start countdown timer for real-time updates (temporarily disabled for debugging)
        // StartCountdownTimer();

        try
        {
            _logger.LogInformation("🔄 Starting main execution loop...");
            var cycleCount = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                cycleCount++;
                try
                {
                    _logger.LogInformation("=== Starting Indicator monitoring cycle #{CycleCount} at {Time} ===", cycleCount, DateTime.Now);
                    await ProcessIndicatorsAsync(stoppingToken);
                    _logger.LogInformation("=== Completed Indicator monitoring cycle #{CycleCount}, waiting {IntervalSeconds} seconds ===", cycleCount, _configuration.IndicatorMonitoring.IntervalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during Indicator monitoring cycle #{CycleCount}", cycleCount);
                }

                await Task.Delay(TimeSpan.FromSeconds(_configuration.IndicatorMonitoring.IntervalSeconds), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "❌ FATAL ERROR in IndicatorMonitoringWorker main loop");
            throw;
        }

        // Cleanup
        _countdownTimer?.Dispose();
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _logger.LogInformation("Indicator Monitoring Worker stopped");
    }

    private async Task ProcessIndicatorsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProcessIndicatorsAsync - Creating service scope");
        using var scope = _serviceProvider.CreateScope();
        var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
        var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

        _logger.LogInformation("ProcessIndicatorsAsync - Services resolved, starting indicator check");

        try
        {
            // Get Indicators that are due for execution
            _logger.LogInformation("ProcessIndicatorsAsync - Calling GetDueIndicatorsAsync");
            var dueIndicators = await GetDueIndicatorsAsync(indicatorService, cancellationToken);

            _logger.LogInformation("ProcessIndicatorsAsync - GetDueIndicatorsAsync returned {Count} indicators", dueIndicators?.Count ?? 0);

            if (dueIndicators == null || !dueIndicators.Any())
            {
                _logger.LogInformation("ProcessIndicatorsAsync - No Indicators due for execution");
                return;
            }

            _logger.LogInformation("ProcessIndicatorsAsync - Found {Count} Indicators due for execution: {IndicatorNames}",
                dueIndicators.Count,
                string.Join(", ", dueIndicators.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID})")));

            // Process Indicators in parallel with configured concurrency limit
            var semaphore = new SemaphoreSlim(_configuration.IndicatorMonitoring.MaxParallelIndicators);
            var tasks = dueIndicators.Select(async indicator =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Each parallel execution will create its own scope, so we don't pass the shared service
                    await ProcessSingleIndicatorAsync(null, indicator, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("ProcessIndicatorsAsync - Completed processing {Count} Indicators", dueIndicators.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessIndicatorsAsync - Error during Indicator processing cycle");
            throw;
        }
    }

    private async Task<List<Indicator>> GetDueIndicatorsAsync(IIndicatorService indicatorService, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetDueIndicatorsAsync - Calling indicatorService.GetDueIndicatorsAsync");
        var dueIndicators = await indicatorService.GetDueIndicatorsAsync(cancellationToken);

        _logger.LogInformation("GetDueIndicatorsAsync - Service returned {Count} due indicators", dueIndicators?.Count ?? 0);

        if (dueIndicators == null || !dueIndicators.Any())
        {
            _logger.LogInformation("GetDueIndicatorsAsync - No indicators returned from service");
            return new List<Indicator>();
        }

        _logger.LogInformation("GetDueIndicatorsAsync - Raw due indicators: {IndicatorDetails}",
            string.Join(", ", dueIndicators.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID}, Active: {i.IsActive}, Running: {i.IsCurrentlyRunning})")));

        // Apply additional filtering based on configuration
        var filteredIndicators = dueIndicators.Where(indicator =>
        {
            // Only process active Indicators if configured
            if (_configuration.IndicatorMonitoring.ProcessOnlyActiveIndicators && !indicator.IsActive)
            {
                _logger.LogInformation("GetDueIndicatorsAsync - Filtering out inactive indicator: {IndicatorName} (ID: {IndicatorID})", indicator.IndicatorName, indicator.IndicatorID);
                return false;
            }

            // Skip Indicators that are currently running if configured
            if (_configuration.IndicatorMonitoring.SkipRunningIndicators && indicator.IsCurrentlyRunning)
            {
                _logger.LogInformation("GetDueIndicatorsAsync - Filtering out currently running indicator: {IndicatorName} (ID: {IndicatorID})", indicator.IndicatorName, indicator.IndicatorID);
                return false;
            }

            return true;
        }).ToList();

        _logger.LogInformation("GetDueIndicatorsAsync - After filtering: {Count} indicators remain: {FilteredIndicatorDetails}",
            filteredIndicators.Count,
            string.Join(", ", filteredIndicators.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID})")));

        return filteredIndicators;
    }

    private async Task ProcessSingleIndicatorAsync(IIndicatorExecutionService? indicatorExecutionService, Indicator indicator, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting execution of Indicator {IndicatorId}: {IndicatorName}. " +
                "CollectorID: {CollectorId}, CollectorItemName: '{CollectorItemName}', LastMinutes: {LastMinutes}",
                indicator.IndicatorID, indicator.IndicatorName, indicator.CollectorID,
                indicator.CollectorItemName, indicator.LastMinutes);

            // Broadcast Indicator execution started (temporarily disabled for debugging)
            // await BroadcastIndicatorExecutionStartedAsync(indicator);

            // Create a new scope for this specific indicator execution to avoid DbContext concurrency issues
            using var scope = _serviceProvider.CreateScope();
            var scopedIndicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.IndicatorMonitoring.ExecutionTimeoutSeconds));

            var result = await scopedIndicatorExecutionService.ExecuteIndicatorAsync(
                indicator.IndicatorID,
                "Scheduled",
                saveResults: true,
                timeoutCts.Token);

            if (result.WasSuccessful)
            {
                _logger.LogInformation("Successfully executed Indicator {IndicatorId}: {IndicatorName}. Value: {Value}",
                    indicator.IndicatorID, indicator.IndicatorName, result.CurrentValue);
                _indicatorsProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

                // Broadcast successful completion (temporarily disabled for debugging)
                // await BroadcastIndicatorExecutionCompletedAsync(indicator, true, result.CurrentValue, stopwatch.Elapsed);
            }
            else
            {
                _logger.LogWarning("Indicator execution failed for {IndicatorId}: {IndicatorName}. " +
                    "Error: {Error}. Duration: {Duration}ms. Context: {Context}",
                    indicator.IndicatorID, indicator.IndicatorName, result.ErrorMessage,
                    result.ExecutionDuration.TotalMilliseconds, result.ExecutionContext);
                _indicatorsFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));

                // Broadcast failed completion with detailed error (temporarily disabled for debugging)
                // await BroadcastIndicatorExecutionCompletedAsync(indicator, false, null, stopwatch.Elapsed, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Indicator execution cancelled for {IndicatorId}: {IndicatorName}", indicator.IndicatorID, indicator.IndicatorName);
            await BroadcastIndicatorExecutionCompletedAsync(indicator, false, null, stopwatch.Elapsed, "Execution cancelled");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Indicator execution timed out for {IndicatorId}: {IndicatorName}", indicator.IndicatorID, indicator.IndicatorName);
            _indicatorsFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "timeout"));
            await BroadcastIndicatorExecutionCompletedAsync(indicator, false, null, stopwatch.Elapsed, "Execution timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Indicator {IndicatorId}: {IndicatorName}", indicator.IndicatorID, indicator.IndicatorName);
            _indicatorsFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
            await BroadcastIndicatorExecutionCompletedAsync(indicator, false, null, stopwatch.Elapsed, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            _indicatorExecutionDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("indicator_id", indicator.IndicatorID.ToString()));
        }
    }

    private async Task BroadcastIndicatorExecutionStartedAsync(Indicator indicator)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var executionStarted = new
            {
                IndicatorId = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Owner = indicator.OwnerContact?.Name ?? "Unknown",
                StartTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EstimatedDuration = (int?)null, // Could be calculated based on historical data
                CollectorID = indicator.CollectorID,
                CollectorItemName = indicator.CollectorItemName,
                LastMinutes = indicator.LastMinutes,
                ExecutionContext = "Scheduled"
            };

            // Send to the hub's method that will broadcast to all clients
            await _hubConnection.InvokeAsync("SendIndicatorExecutionStartedAsync", executionStarted);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error broadcasting Indicator execution started for {IndicatorId}", indicator.IndicatorID);
        }
    }

    private async Task BroadcastIndicatorExecutionCompletedAsync(Indicator indicator, bool success, decimal? value, TimeSpan duration, string? errorMessage = null)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var executionCompleted = new
            {
                IndicatorID = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Owner = indicator.OwnerContact?.Name ?? "Unknown",
                Success = success,
                Value = value,
                Duration = (int)duration.TotalMilliseconds, // Use milliseconds for better precision
                CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = errorMessage,
                CollectorID = indicator.CollectorID,
                CollectorItemName = indicator.CollectorItemName,
                LastMinutes = indicator.LastMinutes,
                ExecutionContext = "Scheduled",
                AlertsGenerated = 0 // TODO: Get actual alert count from execution result
            };

            // Send to the hub's method that will broadcast to all clients
            await _hubConnection.InvokeAsync("SendIndicatorExecutionCompletedAsync", executionCompleted);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error broadcasting Indicator execution completed for {IndicatorId}", indicator.IndicatorID);
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
            var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();

            var now = DateTime.UtcNow;
            var allIndicators = await indicatorService.GetAllIndicatorsAsync(CancellationToken.None);

            var nextIndicator = allIndicators
                .Where(indicator => indicator.IsActive && !indicator.IsCurrentlyRunning)
                .Select(indicator => new
                {
                    Indicator = indicator,
                    NextDue = indicator.GetNextRunTime() ?? now.AddMinutes(1),
                    IsDue = indicator.IsDue()
                })
                .Where(x => !x.IsDue)
                .OrderBy(x => x.NextDue)
                .FirstOrDefault();

            if (nextIndicator != null)
            {
                var secondsUntilDue = Math.Max(0, (int)(nextIndicator.NextDue - now).TotalSeconds);

                var countdownUpdate = new
                {
                    NextIndicatorID = nextIndicator.Indicator.IndicatorID,
                    IndicatorName = nextIndicator.Indicator.IndicatorName,
                    Owner = nextIndicator.Indicator.OwnerContact?.Name ?? "Unknown",
                    SecondsUntilDue = secondsUntilDue,
                    ScheduledTime = nextIndicator.NextDue.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                await _hubConnection.InvokeAsync("SendIndicatorCountdownUpdateAsync", countdownUpdate);
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
