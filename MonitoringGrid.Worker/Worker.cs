using MonitoringGrid.Worker.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace MonitoringGrid.Worker;

/// <summary>
/// Main coordinator worker service that manages the overall health and coordination of all worker services
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly WorkerConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly Meter _meter;
    private readonly Counter<int> _heartbeatCounter;

    public Worker(
        ILogger<Worker> logger,
        IOptions<WorkerConfiguration> configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration.Value;
        _serviceProvider = serviceProvider;

        _meter = new Meter("MonitoringGrid.Worker.Coordinator");
        _heartbeatCounter = _meter.CreateCounter<int>("coordinator_heartbeat_total", "count", "Total coordinator heartbeats");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonitoringGrid Worker Service started");
        _logger.LogInformation("Configuration: Indicator Monitoring Interval: {IndicatorInterval}s, Health Check Interval: {HealthInterval}s",
            _configuration.IndicatorMonitoring?.IntervalSeconds ?? 60, _configuration.HealthChecks.IntervalSeconds);

        try
        {
            // Perform startup checks
            await PerformStartupChecksAsync(stoppingToken);

            // Main coordination loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CoordinateServicesAsync(stoppingToken);
                    _heartbeatCounter.Add(1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in coordinator service cycle");
                }

                // Coordinator runs every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MonitoringGrid Worker Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in MonitoringGrid Worker Service");
            throw;
        }
        finally
        {
            _logger.LogInformation("MonitoringGrid Worker Service stopped");
        }
    }

    private async Task PerformStartupChecksAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing startup checks...");

        try
        {
            // Check database connectivity
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database");
            }

            _logger.LogInformation("Database connectivity check passed");

            // Check if required services are registered
            var requiredServices = new[]
            {
                typeof(MonitoringGrid.Core.Interfaces.IIndicatorService),
                typeof(MonitoringGrid.Core.Interfaces.IIndicatorExecutionService)
            };

            foreach (var serviceType in requiredServices)
            {
                var service = scope.ServiceProvider.GetService(serviceType);
                if (service == null)
                {
                    throw new InvalidOperationException($"Required service {serviceType.Name} is not registered");
                }
            }

            _logger.LogInformation("Service registration check passed");
            _logger.LogInformation("All startup checks completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Startup checks failed");
            throw;
        }
    }

    private async Task CoordinateServicesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Coordinator heartbeat - monitoring worker services");

        try
        {
            // Check service health and log status
            using var scope = _serviceProvider.CreateScope();

            // Log system metrics
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64 / 1024 / 1024; // MB
            var cpuTime = process.TotalProcessorTime.TotalSeconds;

            _logger.LogInformation("System Status - Memory: {MemoryMB}MB, CPU Time: {CpuTime}s, Threads: {ThreadCount}",
                workingSet, cpuTime, process.Threads.Count);

            // Log configuration status
            _logger.LogDebug("Active Configuration - Indicator Monitoring: {IndicatorEnabled}, Scheduled Tasks: {ScheduledEnabled}, Health Checks: {HealthEnabled}, Alert Processing: {AlertEnabled}",
                true, _configuration.ScheduledTasks.Enabled, true, true);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service coordination");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Gracefully shutting down MonitoringGrid Worker Service...");

        try
        {
            // Perform graceful shutdown
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("MonitoringGrid Worker Service shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during worker service shutdown");
            throw;
        }
    }

    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}
