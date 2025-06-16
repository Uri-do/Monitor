using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;

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
            var dueIndicators = await indicatorService.GetDueIndicatorsAsync(cancellationToken);
            
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

                    var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                        indicator.IndicatorID,
                        "SimpleProcessor",
                        saveResults: true,
                        cancellationToken);

                    if (result.WasSuccessful)
                    {
                        successCount++;
                        _logger.LogInformation("✅ Indicator {IndicatorId} executed successfully in {Duration}ms. Current value: {Value}", 
                            indicator.IndicatorID, result.ExecutionDuration.TotalMilliseconds, result.CurrentValue);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning("❌ Indicator {IndicatorId} execution failed: {Error}", 
                            indicator.IndicatorID, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "💥 Exception executing indicator {IndicatorId}: {IndicatorName}", 
                        indicator.IndicatorID, indicator.IndicatorName);
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
}
