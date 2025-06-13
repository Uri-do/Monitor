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
        _logger.LogInformation("Indicator Monitoring Worker started");

        // Initialize SignalR connection
        await InitializeSignalRConnectionAsync();

        // Start countdown timer for real-time updates
        StartCountdownTimer();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessIndicatorsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Indicator monitoring cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_configuration.IndicatorMonitoring.IntervalSeconds), stoppingToken);
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
        using var scope = _serviceProvider.CreateScope();
        var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
        var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

        _logger.LogDebug("Starting Indicator monitoring cycle");

        try
        {
            // Get Indicators that are due for execution
            var dueIndicators = await GetDueIndicatorsAsync(indicatorService, cancellationToken);
            
            if (!dueIndicators.Any())
            {
                _logger.LogDebug("No Indicators due for execution");
                return;
            }

            _logger.LogInformation("Found {Count} Indicators due for execution", dueIndicators.Count);

            // Process Indicators in parallel with configured concurrency limit
            var semaphore = new SemaphoreSlim(_configuration.IndicatorMonitoring.MaxParallelIndicators);
            var tasks = dueIndicators.Select(async indicator =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await ProcessSingleIndicatorAsync(indicatorExecutionService, indicator, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Completed Indicator monitoring cycle. Processed {Count} Indicators", dueIndicators.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Indicator processing cycle");
            throw;
        }
    }

    private async Task<List<Indicator>> GetDueIndicatorsAsync(IIndicatorService indicatorService, CancellationToken cancellationToken)
    {
        var dueIndicators = await indicatorService.GetDueIndicatorsAsync(cancellationToken);

        // Apply additional filtering based on configuration
        var filteredIndicators = dueIndicators.Where(indicator =>
        {
            // Only process active Indicators if configured
            if (_configuration.IndicatorMonitoring.ProcessOnlyActiveIndicators && !indicator.IsActive)
                return false;

            // Skip Indicators that are currently running if configured
            if (_configuration.IndicatorMonitoring.SkipRunningIndicators && indicator.IsCurrentlyRunning)
                return false;

            return true;
        }).ToList();

        return filteredIndicators;
    }

    private async Task ProcessSingleIndicatorAsync(IIndicatorExecutionService indicatorExecutionService, Indicator indicator, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing Indicator {IndicatorId}: {IndicatorName}", indicator.IndicatorID, indicator.IndicatorName);

            // Broadcast Indicator execution started
            await BroadcastIndicatorExecutionStartedAsync(indicator);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.IndicatorMonitoring.ExecutionTimeoutSeconds));

            var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                indicator.IndicatorID,
                "Scheduled",
                saveResults: true,
                timeoutCts.Token);

            if (result.WasSuccessful)
            {
                _logger.LogInformation("Successfully executed Indicator {IndicatorId}: {IndicatorName}. Value: {Value}",
                    indicator.IndicatorID, indicator.IndicatorName, result.CurrentValue);
                _indicatorsProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

                // Broadcast successful completion
                await BroadcastIndicatorExecutionCompletedAsync(indicator, true, result.CurrentValue, stopwatch.Elapsed);
            }
            else
            {
                _logger.LogWarning("Indicator execution failed for {IndicatorId}: {IndicatorName}. Error: {Error}",
                    indicator.IndicatorID, indicator.IndicatorName, result.ErrorMessage);
                _indicatorsFailedCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));

                // Broadcast failed completion
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
                IndicatorId = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Owner = indicator.OwnerContact?.Name ?? "Unknown",
                StartTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EstimatedDuration = (int?)null // Could be calculated based on historical data
            };

            await _hubConnection.SendAsync("IndicatorExecutionStarted", executionStarted);
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
                Success = success,
                Value = value,
                Duration = (int)duration.TotalSeconds,
                CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = errorMessage
            };

            await _hubConnection.SendAsync("IndicatorExecutionCompleted", executionCompleted);
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

                await _hubConnection.SendAsync("IndicatorCountdownUpdate", countdownUpdate);
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
