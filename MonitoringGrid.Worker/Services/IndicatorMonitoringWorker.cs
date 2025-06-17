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
    private Timer? _statusTimer;
    private DateTime _startTime;
    private int _totalIndicatorsProcessed;
    private int _successfulExecutions;
    private int _failedExecutions;
    private DateTime? _lastActivityTime;
    private string? _currentActivity;

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

        // Initialize tracking fields
        _startTime = DateTime.UtcNow;
        _totalIndicatorsProcessed = 0;
        _successfulExecutions = 0;
        _failedExecutions = 0;
        _currentActivity = "Initializing";
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

        // Initialize SignalR connection for real-time updates
        await InitializeSignalRConnectionAsync();

        // Start countdown timer for real-time updates
        StartCountdownTimer();

        // Start worker status broadcasting timer
        StartWorkerStatusTimer();

        try
        {
            _logger.LogInformation("🔄 Starting main execution loop...");
            _currentActivity = "Running";
            var cycleCount = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                cycleCount++;
                try
                {
                    _currentActivity = $"Processing cycle #{cycleCount}";
                    _lastActivityTime = DateTime.UtcNow;
                    _logger.LogInformation("=== Starting Indicator monitoring cycle #{CycleCount} at {Time} ===", cycleCount, DateTime.Now);
                    await ProcessIndicatorsAsync(stoppingToken);
                    _logger.LogInformation("=== Completed Indicator monitoring cycle #{CycleCount}, waiting {IntervalSeconds} seconds ===", cycleCount, _configuration.IndicatorMonitoring.IntervalSeconds);
                    _currentActivity = $"Waiting (next cycle #{cycleCount + 1} in {_configuration.IndicatorMonitoring.IntervalSeconds}s)";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during Indicator monitoring cycle #{CycleCount}", cycleCount);
                    _currentActivity = $"Error in cycle #{cycleCount}";
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
        _currentActivity = "Stopping";
        _countdownTimer?.Dispose();
        _statusTimer?.Dispose();
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

            // Update current activity
            _currentActivity = $"Executing {indicator.IndicatorName}";

            // Broadcast Indicator execution started
            await BroadcastIndicatorExecutionStartedAsync(indicator);

            // Create a new scope for this specific indicator execution to avoid DbContext concurrency issues
            using var scope = _serviceProvider.CreateScope();
            var scopedIndicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.IndicatorMonitoring.ExecutionTimeoutSeconds));

            // Start progress tracking
            var progressTask = TrackExecutionProgressAsync(indicator, stopwatch, timeoutCts.Token);

            var result = await scopedIndicatorExecutionService.ExecuteIndicatorAsync(
                indicator.IndicatorID,
                "Scheduled",
                saveResults: true,
                timeoutCts.Token);

            // Stop progress tracking
            timeoutCts.Cancel();
            try { await progressTask; } catch (OperationCanceledException) { /* Expected */ }

            if (result.WasSuccessful)
            {
                _logger.LogInformation("Successfully executed Indicator {IndicatorId}: {IndicatorName}. Value: {Value}",
                    indicator.IndicatorID, indicator.IndicatorName, result.CurrentValue);
                _indicatorsProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

                // Update tracking
                Interlocked.Increment(ref _totalIndicatorsProcessed);
                Interlocked.Increment(ref _successfulExecutions);
                _lastActivityTime = DateTime.UtcNow;

                // Broadcast successful completion
                await BroadcastIndicatorExecutionCompletedAsync(indicator, true, result.CurrentValue, stopwatch.Elapsed);
            }
            else
            {
                _logger.LogWarning("Indicator execution failed for {IndicatorId}: {IndicatorName}. " +
                    "Error: {Error}. Duration: {Duration}ms. Context: {Context}",
                    indicator.IndicatorID, indicator.IndicatorName, result.ErrorMessage,
                    result.ExecutionDuration.TotalMilliseconds, result.ExecutionContext);
                _indicatorsFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));

                // Update tracking
                Interlocked.Increment(ref _totalIndicatorsProcessed);
                Interlocked.Increment(ref _failedExecutions);
                _lastActivityTime = DateTime.UtcNow;

                // Broadcast failed completion with detailed error
                await BroadcastIndicatorExecutionCompletedAsync(indicator, false, null, stopwatch.Elapsed, result.ErrorMessage);
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
                IndicatorID = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Owner = indicator.OwnerContact?.Name ?? "Unknown",
                StartTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EstimatedDuration = (int?)null, // Could be calculated based on historical data
                ExecutionContext = "Scheduled"
            };

            // Send to the hub's method that will broadcast to all clients
            await _hubConnection.InvokeAsync("SendIndicatorExecutionStartedAsync", executionStarted);
            _logger.LogDebug("Broadcasted Indicator execution started for {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error broadcasting Indicator execution started for {IndicatorId}", indicator.IndicatorID);
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
                IndicatorId = indicator.IndicatorID, // Note: DTO uses IndicatorId (lowercase 'd')
                IndicatorName = indicator.IndicatorName,
                Success = success,
                Value = value,
                Duration = (int)duration.TotalSeconds, // Duration in seconds as expected by DTO
                CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = errorMessage,
                ThresholdBreached = false, // Will be determined by the execution service
                ExecutionContext = "Scheduled"
            };

            // Send to the hub's method that will broadcast to all clients
            await _hubConnection.InvokeAsync("SendIndicatorExecutionCompletedAsync", executionCompleted);
            _logger.LogDebug("Broadcasted Indicator execution completed for {IndicatorId}: {IndicatorName}, Success: {Success}",
                indicator.IndicatorID, indicator.IndicatorName, success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error broadcasting Indicator execution completed for {IndicatorId}", indicator.IndicatorID);
        }
    }

    private async Task TrackExecutionProgressAsync(Indicator indicator, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Starting progress tracking for Indicator {IndicatorId}: {IndicatorName}",
            indicator.IndicatorID, indicator.IndicatorName);

        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("⚠️ SignalR connection not available for progress tracking. State: {State}",
                _hubConnection?.State.ToString() ?? "null");
            return;
        }

        try
        {
            var progressSteps = new[]
            {
                new { Step = "Initializing", Progress = 10, DelayMs = 500 },
                new { Step = "Connecting to Database", Progress = 25, DelayMs = 800 },
                new { Step = "Executing Query", Progress = 50, DelayMs = 2000 },
                new { Step = "Processing Results", Progress = 80, DelayMs = 600 },
                new { Step = "Finalizing", Progress = 95, DelayMs = 300 }
            };

            foreach (var step in progressSteps)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(step.DelayMs, cancellationToken);

                var progressUpdate = new
                {
                    IndicatorId = indicator.IndicatorID,
                    IndicatorName = indicator.IndicatorName,
                    Progress = step.Progress,
                    CurrentStep = step.Step,
                    StartTime = DateTime.UtcNow.Subtract(stopwatch.Elapsed),
                    ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                    EstimatedRemainingSeconds = step.Progress < 95 ? (int)((stopwatch.Elapsed.TotalSeconds / step.Progress) * (100 - step.Progress)) : 0
                };

                await _hubConnection.InvokeAsync("SendIndicatorExecutionProgressAsync", progressUpdate);
                _logger.LogInformation("📊 Sent progress update for Indicator {IndicatorId}: {Progress}% - {Step}",
                    indicator.IndicatorID, step.Progress, step.Step);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when execution completes
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error tracking execution progress for Indicator {IndicatorId}", indicator.IndicatorID);
        }
    }

    private async Task InitializeSignalRConnectionAsync()
    {
        try
        {
            // Get API base URL from configuration or use default
            var apiBaseUrl = _configuration.ApiBaseUrl ?? "https://localhost:57652";
            var hubUrl = $"{apiBaseUrl}/monitoring-hub";

            _logger.LogInformation("Initializing SignalR connection to {HubUrl}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Add connection event handlers
            _hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning("SignalR connection closed. Error: {Error}", error?.Message ?? "None");
                await Task.CompletedTask; // Satisfy async requirement
            };

            _hubConnection.Reconnecting += (error) =>
            {
                _logger.LogInformation("SignalR connection reconnecting. Error: {Error}", error?.Message ?? "None");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation("SignalR connection reconnected. Connection ID: {ConnectionId}", connectionId ?? "Unknown");
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established successfully. Connection ID: {ConnectionId}, State: {State}",
                _hubConnection.ConnectionId, _hubConnection.State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish SignalR connection to {ApiBaseUrl}. Real-time updates will be disabled.",
                _configuration.ApiBaseUrl ?? "https://localhost:57652");
        }
    }

    private void StartCountdownTimer()
    {
        _countdownTimer = new Timer(async _ => await SendCountdownUpdateAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void StartWorkerStatusTimer()
    {
        // Send worker status updates every 5 seconds
        _statusTimer = new Timer(async _ => await SendWorkerStatusUpdateAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
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

            _logger.LogDebug("Countdown: Found {Count} total indicators", allIndicators.Count);

            var activeIndicators = allIndicators
                .Where(indicator => indicator.IsActive && !indicator.IsCurrentlyRunning)
                .ToList();

            _logger.LogDebug("Countdown: Found {Count} active, non-running indicators", activeIndicators.Count);

            var indicatorsWithSchedule = activeIndicators
                .Select(indicator => new
                {
                    Indicator = indicator,
                    NextDue = indicator.GetNextRunTime(),
                    IsDue = indicator.IsDue()
                })
                .ToList();

            foreach (var item in indicatorsWithSchedule)
            {
                _logger.LogDebug("Countdown: Indicator {Id} ({Name}) - NextDue: {NextDue}, IsDue: {IsDue}",
                    item.Indicator.IndicatorID, item.Indicator.IndicatorName, item.NextDue, item.IsDue);
            }

            var nextIndicator = indicatorsWithSchedule
                .Where(x => x.NextDue.HasValue && !x.IsDue)
                .OrderBy(x => x.NextDue)
                .FirstOrDefault();

            if (nextIndicator != null)
            {
                var secondsUntilDue = Math.Max(0, (int)(nextIndicator.NextDue!.Value - now).TotalSeconds);

                var countdownUpdate = new
                {
                    NextIndicatorID = nextIndicator.Indicator.IndicatorID,
                    IndicatorName = nextIndicator.Indicator.IndicatorName,
                    Owner = nextIndicator.Indicator.OwnerContact?.Name ?? "Unknown",
                    SecondsUntilDue = secondsUntilDue,
                    ScheduledTime = nextIndicator.NextDue.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                await _hubConnection.InvokeAsync("SendIndicatorCountdownUpdateAsync", countdownUpdate);
                _logger.LogDebug("Sent countdown update for Indicator {IndicatorId}: {SecondsUntilDue} seconds",
                    nextIndicator.Indicator.IndicatorID, secondsUntilDue);
            }
            else
            {
                _logger.LogDebug("Countdown: No indicators found with future next run time");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error sending countdown update");
        }
    }

    private async Task SendWorkerStatusUpdateAsync()
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var now = DateTime.UtcNow;
            var uptime = now - _startTime;

            var workerStatus = new
            {
                IsRunning = true,
                Mode = "Manual",
                ProcessId = Environment.ProcessId,
                StartTime = _startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
                UptimeSeconds = (int)uptime.TotalSeconds,
                LastHeartbeat = now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CurrentActivity = _currentActivity ?? "Running",
                LastActivityTime = _lastActivityTime?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                TotalIndicatorsProcessed = _totalIndicatorsProcessed,
                SuccessfulExecutions = _successfulExecutions,
                FailedExecutions = _failedExecutions,
                SuccessRate = _totalIndicatorsProcessed > 0 ? (double)_successfulExecutions / _totalIndicatorsProcessed * 100 : 0,
                Services = new[]
                {
                    new
                    {
                        Name = "IndicatorMonitoringWorker",
                        Status = "Running",
                        LastActivity = _lastActivityTime?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        CurrentActivity = _currentActivity ?? "Running",
                        ProcessedCount = _totalIndicatorsProcessed,
                        SuccessCount = _successfulExecutions,
                        FailureCount = _failedExecutions,
                        Description = "Monitors and executes scheduled indicators"
                    }
                }
            };

            await _hubConnection.InvokeAsync("SendWorkerStatusUpdateAsync", workerStatus);
            _logger.LogTrace("Sent worker status update - Uptime: {Uptime}, Processed: {Processed}, Success Rate: {SuccessRate:F1}%",
                uptime.ToString(@"dd\.hh\:mm\:ss"), _totalIndicatorsProcessed, workerStatus.SuccessRate);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error sending worker status update");
        }
    }

    public override void Dispose()
    {
        _countdownTimer?.Dispose();
        _statusTimer?.Dispose();
        _hubConnection?.DisposeAsync();
        _meter?.Dispose();
        base.Dispose();
    }
}
