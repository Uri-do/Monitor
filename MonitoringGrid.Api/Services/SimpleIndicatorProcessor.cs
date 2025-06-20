using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using static MonitoringGrid.Core.Interfaces.IIndicatorExecutionService;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Simple indicator processor that runs directly in the API process
/// This bypasses the separate worker process and hosted service issues
/// </summary>
public class SimpleIndicatorProcessor : BackgroundService
{
    private readonly ILogger<SimpleIndicatorProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60); // Run every 60 seconds

    public SimpleIndicatorProcessor(
        ILogger<SimpleIndicatorProcessor> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 SimpleIndicatorProcessor started - Running every {Interval} seconds", _interval.TotalSeconds);

        // Wait a bit before starting to allow the API to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        var cycleCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            cycleCount++;
            try
            {
                _logger.LogInformation("🔄 Starting indicator processing cycle #{CycleCount} at {Time}", cycleCount, DateTime.Now);
                
                await ProcessIndicatorsAsync(stoppingToken);
                
                _logger.LogInformation("✅ Completed indicator processing cycle #{CycleCount}, waiting {Interval} seconds", cycleCount, _interval.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during indicator processing cycle #{CycleCount}", cycleCount);
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("🛑 SimpleIndicatorProcessor stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 SimpleIndicatorProcessor is stopping...");

        try
        {
            // Call base implementation to stop the background service
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("✅ SimpleIndicatorProcessor stopped gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during SimpleIndicatorProcessor shutdown");
            throw;
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("🧹 SimpleIndicatorProcessor disposing...");
        base.Dispose();
        _logger.LogInformation("✅ SimpleIndicatorProcessor disposed");
    }

    private async Task ProcessIndicatorsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📋 Creating service scope for indicator processing...");
            
            using var scope = _serviceProvider.CreateScope();
            var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            _logger.LogInformation("✅ Services resolved successfully");

            // Get due indicators
            _logger.LogInformation("🔍 Getting due indicators...");
            var priorityFilter = new PriorityFilterOptions();
            var dueIndicatorsResult = await indicatorService.GetDueIndicatorsAsync(priorityFilter, cancellationToken);
            var dueIndicators = dueIndicatorsResult.IsSuccess ? dueIndicatorsResult.Value : new List<Core.Entities.Indicator>();
            
            _logger.LogInformation("📊 Found {Count} indicators due for execution", dueIndicators.Count());

            if (!dueIndicators.Any())
            {
                _logger.LogInformation("ℹ️ No indicators are currently due for execution");
                return;
            }

            // Process each indicator
            var successCount = 0;
            var failureCount = 0;

            foreach (var indicator in dueIndicators)
            {
                try
                {
                    _logger.LogInformation("🚀 Executing indicator {IndicatorId}: {IndicatorName}",
                        indicator.IndicatorID, indicator.IndicatorName);

                    // Broadcast execution started
                    await BroadcastIndicatorExecutionStartedAsync(indicator);

                    var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                        indicator.IndicatorID,
                        "SimpleProcessor",
                        saveResults: true,
                        cancellationToken);

                    if (result.WasSuccessful)
                    {
                        successCount++;
                        _logger.LogInformation("✅ Indicator {IndicatorId} executed successfully in {Duration}ms. Current value: {Value}",
                            indicator.IndicatorID, result.ExecutionDuration.TotalMilliseconds, result.Value);

                        // Broadcast successful completion
                        await BroadcastIndicatorExecutionCompletedAsync(indicator, result, true);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning("❌ Indicator {IndicatorId} execution failed: {Error}",
                            indicator.IndicatorID, result.ErrorMessage);

                        // Broadcast failed completion
                        await BroadcastIndicatorExecutionCompletedAsync(indicator, result, false);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "💥 Exception executing indicator {IndicatorId}: {IndicatorName}",
                        indicator.IndicatorID, indicator.IndicatorName);

                    // Broadcast exception completion
                    await BroadcastIndicatorExecutionExceptionAsync(indicator, ex);
                }
            }

            _logger.LogInformation("📈 Processing summary: {SuccessCount} successful, {FailureCount} failed, {TotalCount} total", 
                successCount, failureCount, dueIndicators.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Critical error during indicator processing");
        }
    }

    private async Task BroadcastIndicatorExecutionStartedAsync(Core.Entities.Indicator indicator)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var realtimeService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();

            var dto = new IndicatorExecutionStartedDto
            {
                IndicatorID = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Owner = indicator.OwnerContact?.Name ?? "System",
                StartTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ExecutionContext = "SimpleProcessor"
            };

            await realtimeService.SendIndicatorExecutionStartedAsync(dto);
            _logger.LogDebug("Broadcasted Indicator execution started for {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast indicator execution started for {IndicatorId}", indicator.IndicatorID);
        }
    }

    private async Task BroadcastIndicatorExecutionCompletedAsync(Core.Entities.Indicator indicator, IndicatorExecutionResult result, bool success)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var realtimeService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();

            var dto = new IndicatorExecutionCompletedDto
            {
                IndicatorId = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Success = success,
                Value = result.Value,
                Duration = (int)result.ExecutionDuration.TotalMilliseconds,
                CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = result.ErrorMessage,
                ThresholdBreached = result.ThresholdBreached,
                ExecutionContext = "SimpleProcessor"
            };

            await realtimeService.SendIndicatorExecutionCompletedAsync(dto);
            _logger.LogDebug("Broadcasted Indicator execution completed for {IndicatorId}: {IndicatorName}, Success: {Success}",
                indicator.IndicatorID, indicator.IndicatorName, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast indicator execution completed for {IndicatorId}", indicator.IndicatorID);
        }
    }

    private async Task BroadcastIndicatorExecutionExceptionAsync(Core.Entities.Indicator indicator, Exception exception)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var realtimeService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();

            var dto = new IndicatorExecutionCompletedDto
            {
                IndicatorId = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Success = false,
                Value = null,
                Duration = 0,
                CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ErrorMessage = exception.Message,
                ThresholdBreached = false,
                ExecutionContext = "SimpleProcessor"
            };

            await realtimeService.SendIndicatorExecutionCompletedAsync(dto);
            _logger.LogDebug("Broadcasted Indicator execution exception for {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast indicator execution exception for {IndicatorId}", indicator.IndicatorID);
        }
    }
}
