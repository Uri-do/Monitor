using EnterpriseApp.Worker.Jobs;
using EnterpriseApp.Core.Interfaces;

namespace EnterpriseApp.Worker.Services;

/// <summary>
/// Main background service that coordinates all worker operations
/// </summary>
public class WorkerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJobSchedulerService _jobScheduler;
    private readonly ILogger<WorkerBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<IDisposable> _disposables = new();

    /// <summary>
    /// Initializes a new instance of the WorkerBackgroundService
    /// </summary>
    public WorkerBackgroundService(
        IServiceProvider serviceProvider,
        IJobSchedulerService jobScheduler,
        ILogger<WorkerBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _jobScheduler = jobScheduler ?? throw new ArgumentNullException(nameof(jobScheduler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Background Service starting");

        try
        {
            // Initialize scheduled jobs
            await InitializeScheduledJobsAsync();

            // Start monitoring services
            await StartMonitoringServicesAsync(stoppingToken);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                
                // Perform periodic health checks
                await PerformHealthChecksAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker Background Service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker Background Service encountered an error");
            throw;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Initializes all scheduled jobs
    /// </summary>
    private async Task InitializeScheduledJobsAsync()
    {
        _logger.LogInformation("Initializing scheduled jobs");

        try
        {
            // Schedule cleanup job - runs daily at 2 AM
            await _jobScheduler.ScheduleRecurringJobAsync<CleanupInactiveDomainEntitiesJob>(
                "0 2 * * *", // Daily at 2 AM
                new Dictionary<string, object>
                {
                    ["InactiveDays"] = _configuration.GetValue<int>("Jobs:Cleanup:InactiveDays", 90),
                    ["BatchSize"] = _configuration.GetValue<int>("Jobs:Cleanup:BatchSize", 100),
                    ["DryRun"] = _configuration.GetValue<bool>("Jobs:Cleanup:DryRun", false)
                });

            // Schedule statistics generation job - runs every hour
            await _jobScheduler.ScheduleRecurringJobAsync<GenerateDomainEntityStatisticsJob>(
                "0 * * * *"); // Every hour at minute 0

            // Schedule any startup jobs
            var runStartupJobs = _configuration.GetValue<bool>("Jobs:RunStartupJobs", true);
            if (runStartupJobs)
            {
                await ScheduleStartupJobsAsync();
            }

            _logger.LogInformation("Scheduled jobs initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize scheduled jobs");
            throw;
        }
    }

    /// <summary>
    /// Schedules jobs that should run on startup
    /// </summary>
    private async Task ScheduleStartupJobsAsync()
    {
        _logger.LogInformation("Scheduling startup jobs");

        // Generate initial statistics
        await _jobScheduler.EnqueueJobAsync<GenerateDomainEntityStatisticsJob>();

        // Add any other startup jobs here
        _logger.LogInformation("Startup jobs scheduled");
    }

    /// <summary>
    /// Starts monitoring services
    /// </summary>
    private async Task StartMonitoringServicesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting monitoring services");

        // Start job queue monitor
        var jobQueueMonitor = new JobQueueMonitorService(_serviceProvider, _logger);
        _disposables.Add(jobQueueMonitor);
        _ = Task.Run(() => jobQueueMonitor.StartAsync(cancellationToken), cancellationToken);

        // Start performance monitor
        var performanceMonitor = new PerformanceMonitorService(_serviceProvider, _logger);
        _disposables.Add(performanceMonitor);
        _ = Task.Run(() => performanceMonitor.StartAsync(cancellationToken), cancellationToken);

        // Start health check monitor
        var healthCheckMonitor = new HealthCheckMonitorService(_serviceProvider, _logger);
        _disposables.Add(healthCheckMonitor);
        _ = Task.Run(() => healthCheckMonitor.StartAsync(cancellationToken), cancellationToken);

        await Task.CompletedTask;
        _logger.LogInformation("Monitoring services started");
    }

    /// <summary>
    /// Performs periodic health checks
    /// </summary>
    private async Task PerformHealthChecksAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            // Check database connectivity
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.TestConnectionAsync();

            // Check cache connectivity
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            await cacheService.PingAsync();

            // Log health status
            _logger.LogDebug("Health check completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed");
        }
    }

    /// <summary>
    /// Cleans up resources
    /// </summary>
    private async Task CleanupAsync()
    {
        _logger.LogInformation("Cleaning up Worker Background Service");

        // Dispose all monitoring services
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing service");
            }
        }

        _disposables.Clear();

        await Task.CompletedTask;
        _logger.LogInformation("Worker Background Service cleanup completed");
    }

    /// <summary>
    /// Disposes the service
    /// </summary>
    public override void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
        
        base.Dispose();
    }
}

/// <summary>
/// Service for monitoring job queue health and performance
/// </summary>
public class JobQueueMonitorService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly Timer _monitorTimer;

    public JobQueueMonitorService(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Monitor every 5 minutes
        _monitorTimer = new Timer(MonitorJobQueue, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Queue Monitor started");
        
        // Initial monitoring
        await MonitorJobQueueAsync();
        
        // Keep running until cancelled
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        }
    }

    private async void MonitorJobQueue(object? state)
    {
        await MonitorJobQueueAsync();
    }

    private async Task MonitorJobQueueAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jobScheduler = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();
            
            var scheduledJobs = await jobScheduler.GetScheduledJobsAsync();
            var runningJobs = scheduledJobs.Count(j => j.Status == JobStatus.Running);
            var scheduledJobsCount = scheduledJobs.Count(j => j.Status == JobStatus.Scheduled);
            var failedJobs = scheduledJobs.Count(j => j.Status == JobStatus.Failed);

            _logger.LogDebug("Job Queue Status - Running: {Running}, Scheduled: {Scheduled}, Failed: {Failed}", 
                runningJobs, scheduledJobsCount, failedJobs);

            // Alert on too many failed jobs
            if (failedJobs > 10)
            {
                _logger.LogWarning("High number of failed jobs detected: {FailedCount}", failedJobs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring job queue");
        }
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

/// <summary>
/// Service for monitoring worker performance
/// </summary>
public class PerformanceMonitorService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly Timer _monitorTimer;

    public PerformanceMonitorService(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Monitor every 10 minutes
        _monitorTimer = new Timer(MonitorPerformance, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performance Monitor started");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
        }
    }

    private async void MonitorPerformance(object? state)
    {
        await MonitorPerformanceAsync();
    }

    private async Task MonitorPerformanceAsync()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
            var cpuTime = process.TotalProcessorTime;

            _logger.LogDebug("Performance Metrics - Memory: {Memory}MB, CPU Time: {CpuTime}", 
                memoryUsage, cpuTime);

            // Alert on high memory usage (> 1GB)
            if (memoryUsage > 1024)
            {
                _logger.LogWarning("High memory usage detected: {Memory}MB", memoryUsage);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring performance");
        }
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

/// <summary>
/// Service for monitoring health checks
/// </summary>
public class HealthCheckMonitorService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly Timer _monitorTimer;

    public HealthCheckMonitorService(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Monitor every 2 minutes
        _monitorTimer = new Timer(MonitorHealth, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health Check Monitor started");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
        }
    }

    private async void MonitorHealth(object? state)
    {
        await MonitorHealthAsync();
    }

    private async Task MonitorHealthAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            // Check critical services
            var healthChecks = new List<(string Name, Func<Task<bool>> Check)>
            {
                ("Database", async () => {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    await unitOfWork.TestConnectionAsync();
                    return true;
                }),
                ("Cache", async () => {
                    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                    await cacheService.PingAsync();
                    return true;
                })
            };

            var healthyServices = 0;
            var totalServices = healthChecks.Count;

            foreach (var (name, check) in healthChecks)
            {
                try
                {
                    await check();
                    healthyServices++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for {ServiceName}", name);
                }
            }

            _logger.LogDebug("Health Status - {Healthy}/{Total} services healthy", healthyServices, totalServices);

            if (healthyServices < totalServices)
            {
                _logger.LogWarning("Some services are unhealthy: {Healthy}/{Total}", healthyServices, totalServices);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring health");
        }
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}
