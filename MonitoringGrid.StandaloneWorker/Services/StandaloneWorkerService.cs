using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.StandaloneWorker.Models;
using System.Diagnostics;

namespace MonitoringGrid.StandaloneWorker.Services;

/// <summary>
/// Main standalone worker service that processes indicators
/// </summary>
public class StandaloneWorkerService : BackgroundService
{
    private readonly ILogger<StandaloneWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly StandaloneWorkerConfig _config;
    private readonly IWorkerStatusReporter _statusReporter;
    private readonly CancellationTokenSource _durationCts = new();

    public StandaloneWorkerService(
        ILogger<StandaloneWorkerService> logger,
        IServiceProvider serviceProvider,
        StandaloneWorkerConfig config,
        IWorkerStatusReporter statusReporter)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;
        _statusReporter = statusReporter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Standalone Worker {WorkerId} starting execution", _config.WorkerId);

        try
        {
            // Set up duration timeout if specified
            if (_config.DurationSeconds > 0)
            {
                _durationCts.CancelAfter(TimeSpan.FromSeconds(_config.DurationSeconds));
                _logger.LogInformation("Worker will run for {Duration} seconds", _config.DurationSeconds);
            }

            // Combine stopping token with duration token
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _durationCts.Token);
            var cancellationToken = combinedCts.Token;

            await _statusReporter.UpdateStateAsync(WorkerState.Running, "Worker started and ready to process indicators");

            // Verify database connection
            await VerifyDatabaseConnectionAsync();

            // Main processing loop
            var cycleCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                cycleCount++;
                
                try
                {
                    await _statusReporter.UpdateStateAsync(WorkerState.Processing, 
                        $"Starting processing cycle #{cycleCount}", 
                        $"Cycle #{cycleCount}");

                    _logger.LogInformation("=== Starting processing cycle #{CycleCount} ===", cycleCount);

                    await ProcessIndicatorsAsync(cancellationToken);

                    await _statusReporter.UpdateStateAsync(WorkerState.Idle, 
                        $"Completed cycle #{cycleCount}, waiting for next cycle", 
                        $"Waiting ({_config.ProcessingIntervalSeconds}s)");

                    _logger.LogInformation("=== Completed cycle #{CycleCount}, waiting {Interval}s ===", 
                        cycleCount, _config.ProcessingIntervalSeconds);

                    // Wait for next cycle
                    await Task.Delay(TimeSpan.FromSeconds(_config.ProcessingIntervalSeconds), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker execution cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in processing cycle #{CycleCount}", cycleCount);
                    await _statusReporter.UpdateStateAsync(WorkerState.Failed, 
                        $"Error in cycle #{cycleCount}: {ex.Message}");
                    
                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }

            // Check if we stopped due to duration timeout
            if (_durationCts.Token.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker {WorkerId} completed duration-limited execution", _config.WorkerId);
                await _statusReporter.UpdateStateAsync(WorkerState.Stopped, "Completed duration-limited execution");
            }
            else
            {
                _logger.LogInformation("Worker {WorkerId} stopped by cancellation", _config.WorkerId);
                await _statusReporter.UpdateStateAsync(WorkerState.Stopping, "Worker stopping");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in worker {WorkerId}", _config.WorkerId);
            await _statusReporter.UpdateStateAsync(WorkerState.Failed, $"Fatal error: {ex.Message}");
            throw;
        }
        finally
        {
            await _statusReporter.UpdateStateAsync(WorkerState.Stopped, "Worker execution completed");
            _logger.LogInformation("Worker {WorkerId} execution completed", _config.WorkerId);
        }
    }

    private async Task VerifyDatabaseConnectionAsync()
    {
        _logger.LogInformation("Verifying database connection...");
        
        using var scope = _serviceProvider.CreateScope();
        var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();

        try
        {
            // Test database connection by getting indicator count
            var result = await indicatorService.GetAllIndicatorsAsync(null);
            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Database connection verified successfully");
                await _statusReporter.UpdateStateAsync(WorkerState.Running, "Database connection verified");
            }
            else
            {
                throw new InvalidOperationException($"Failed to verify database connection: {result.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database connection verification failed");
            await _statusReporter.UpdateStateAsync(WorkerState.Failed, $"Database connection failed: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessIndicatorsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
        var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

        try
        {
            // Get indicators to process
            var indicators = await GetIndicatorsToProcessAsync(indicatorService, cancellationToken);
            
            if (!indicators.Any())
            {
                _logger.LogInformation("No indicators to process in this cycle");
                return;
            }

            _logger.LogInformation("Processing {Count} indicators: {IndicatorNames}", 
                indicators.Count, 
                string.Join(", ", indicators.Select(i => i.IndicatorName)));

            // Process indicators with concurrency limit
            var semaphore = new SemaphoreSlim(_config.MaxParallelIndicators);
            var tasks = indicators.Select(async indicator =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await ProcessSingleIndicatorAsync(indicator, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Completed processing {Count} indicators", indicators.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during indicator processing");
            throw;
        }
    }

    private async Task<List<MonitoringGrid.Core.Entities.Indicator>> GetIndicatorsToProcessAsync(
        IIndicatorService indicatorService, 
        CancellationToken cancellationToken)
    {
        // If specific indicator IDs are configured, get those
        if (_config.IndicatorIds.Any())
        {
            var indicators = new List<MonitoringGrid.Core.Entities.Indicator>();
            foreach (var id in _config.IndicatorIds)
            {
                var result = await indicatorService.GetIndicatorByIdAsync(id, cancellationToken);
                if (result.IsSuccess && result.Value != null)
                {
                    indicators.Add(result.Value);
                }
                else
                {
                    _logger.LogWarning("Could not find indicator with ID {IndicatorId}", id);
                }
            }
            return indicators;
        }

        // Otherwise get due indicators
        var dueResult = await indicatorService.GetDueIndicatorsAsync(null, cancellationToken);
        return dueResult.IsSuccess ? dueResult.Value : new List<MonitoringGrid.Core.Entities.Indicator>();
    }

    private async Task ProcessSingleIndicatorAsync(MonitoringGrid.Core.Entities.Indicator indicator, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("🔄 Processing indicator {IndicatorId}: {IndicatorName}", 
                indicator.IndicatorID, indicator.IndicatorName);

            await _statusReporter.ReportIndicatorProgressAsync((int)indicator.IndicatorID, indicator.IndicatorName, 0, "Starting");

            // Create execution scope
            using var scope = _serviceProvider.CreateScope();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            // Set up timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.ExecutionTimeoutSeconds));

            // Simulate progress updates
            var progressTask = SimulateProgressAsync(indicator, timeoutCts.Token);

            // Execute indicator
            var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                indicator.IndicatorID,
                $"StandaloneWorker-{_config.WorkerId}",
                saveResults: true,
                timeoutCts.Token);

            // Stop progress simulation
            timeoutCts.Cancel();
            try { await progressTask; } catch (OperationCanceledException) { /* Expected */ }

            if (result.WasSuccessful)
            {
                _logger.LogInformation("✅ Successfully executed indicator {IndicatorId}: {IndicatorName} = {Value}", 
                    indicator.IndicatorID, indicator.IndicatorName, result.Value);
                
                await _statusReporter.ReportIndicatorCompletionAsync((int)indicator.IndicatorID, indicator.IndicatorName, true);
            }
            else
            {
                _logger.LogWarning("❌ Failed to execute indicator {IndicatorId}: {IndicatorName} - {Error}", 
                    indicator.IndicatorID, indicator.IndicatorName, result.ErrorMessage);
                
                await _statusReporter.ReportIndicatorCompletionAsync((int)indicator.IndicatorID, indicator.IndicatorName, false, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Indicator execution cancelled: {IndicatorName}", indicator.IndicatorName);
            await _statusReporter.ReportIndicatorCompletionAsync((int)indicator.IndicatorID, indicator.IndicatorName, false, "Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indicator {IndicatorId}: {IndicatorName}", 
                indicator.IndicatorID, indicator.IndicatorName);
            
            await _statusReporter.ReportIndicatorCompletionAsync((int)indicator.IndicatorID, indicator.IndicatorName, false, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogDebug("Indicator {IndicatorId} processing completed in {Duration}ms", 
                indicator.IndicatorID, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task SimulateProgressAsync(MonitoringGrid.Core.Entities.Indicator indicator, CancellationToken cancellationToken)
    {
        var steps = new[]
        {
            new { Progress = 10, Step = "Initializing" },
            new { Progress = 30, Step = "Connecting to database" },
            new { Progress = 60, Step = "Executing query" },
            new { Progress = 85, Step = "Processing results" },
            new { Progress = 95, Step = "Finalizing" }
        };

        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            await _statusReporter.ReportIndicatorProgressAsync((int)indicator.IndicatorID, indicator.IndicatorName, step.Progress, step.Step);
            await Task.Delay(500, cancellationToken);
        }
    }

    public override void Dispose()
    {
        _durationCts?.Dispose();
        base.Dispose();
    }
}
