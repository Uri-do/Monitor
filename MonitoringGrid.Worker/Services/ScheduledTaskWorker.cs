using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Infrastructure.Data;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace MonitoringGrid.Worker.Services;

/// <summary>
/// Background service responsible for scheduled maintenance tasks
/// </summary>
public class ScheduledTaskWorker : BackgroundService
{
    private readonly ILogger<ScheduledTaskWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerConfiguration _configuration;
    private readonly ISchedulerFactory _schedulerFactory;

    public ScheduledTaskWorker(
        ILogger<ScheduledTaskWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerConfiguration> configuration,
        ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration.Value;
        _schedulerFactory = schedulerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.ScheduledTasks.Enabled)
        {
            _logger.LogInformation("Scheduled tasks are disabled");
            return;
        }

        _logger.LogInformation("Scheduled Task Worker started");

        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
            await scheduler.Start(stoppingToken);

            // Schedule cleanup job
            await ScheduleCleanupJob(scheduler, stoppingToken);

            // Schedule maintenance job
            await ScheduleMaintenanceJob(scheduler, stoppingToken);

            _logger.LogInformation("All scheduled jobs have been configured");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled Task Worker stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Scheduled Task Worker");
            throw;
        }
    }

    private async Task ScheduleCleanupJob(IScheduler scheduler, CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<CleanupJob>()
            .WithIdentity("cleanup-job", "maintenance")
            .WithDescription("Cleanup old historical data and logs")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("cleanup-trigger", "maintenance")
            .WithCronSchedule(_configuration.ScheduledTasks.CleanupCronExpression)
            .WithDescription("Trigger for cleanup job")
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
        
        _logger.LogInformation("Scheduled cleanup job with cron expression: {CronExpression}", 
            _configuration.ScheduledTasks.CleanupCronExpression);
    }

    private async Task ScheduleMaintenanceJob(IScheduler scheduler, CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<MaintenanceJob>()
            .WithIdentity("maintenance-job", "maintenance")
            .WithDescription("Perform database maintenance tasks")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("maintenance-trigger", "maintenance")
            .WithCronSchedule(_configuration.ScheduledTasks.MaintenanceCronExpression)
            .WithDescription("Trigger for maintenance job")
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
        
        _logger.LogInformation("Scheduled maintenance job with cron expression: {CronExpression}", 
            _configuration.ScheduledTasks.MaintenanceCronExpression);
    }
}

/// <summary>
/// Quartz job for cleanup tasks
/// </summary>
[DisallowConcurrentExecution]
public class CleanupJob : IJob
{
    private readonly ILogger<CleanupJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerConfiguration _configuration;

    public CleanupJob(
        ILogger<CleanupJob> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerConfiguration> configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting cleanup job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-_configuration.ScheduledTasks.HistoricalDataRetentionDays);
            var logCutoffDate = DateTime.UtcNow.AddDays(-_configuration.ScheduledTasks.LogRetentionDays);

            // TODO: Clean up old indicator execution history when new table is implemented
            // For now, skip historical data cleanup since HistoricalData table is obsolete
            _logger.LogInformation("Historical data cleanup skipped - table is obsolete");

            // Clean up old alert logs
            var alertLogsDeleted = await dbContext.AlertLogs
                .Where(a => a.TriggerTime < logCutoffDate)
                .ExecuteDeleteAsync();

            _logger.LogInformation("Deleted {Count} old alert log records", alertLogsDeleted);

            _logger.LogInformation("Cleanup job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup job execution");
            throw;
        }
    }
}

/// <summary>
/// Quartz job for maintenance tasks
/// </summary>
[DisallowConcurrentExecution]
public class MaintenanceJob : IJob
{
    private readonly ILogger<MaintenanceJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MaintenanceJob(
        ILogger<MaintenanceJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting maintenance job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

            // Update database statistics
            await dbContext.Database.ExecuteSqlRawAsync("UPDATE STATISTICS");
            _logger.LogInformation("Updated database statistics");

            // Rebuild fragmented indexes (simplified approach)
            await dbContext.Database.ExecuteSqlRawAsync(@"
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql = @sql + 'ALTER INDEX ALL ON ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ' REORGANIZE;' + CHAR(13)
                FROM sys.tables WHERE is_ms_shipped = 0;
                EXEC sp_executesql @sql;
            ");
            _logger.LogInformation("Reorganized database indexes");

            // Check database integrity
            var integrityResults = await dbContext.Database.ExecuteSqlRawAsync("DBCC CHECKDB WITH NO_INFOMSGS");
            _logger.LogInformation("Database integrity check completed");

            _logger.LogInformation("Maintenance job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during maintenance job execution");
            throw;
        }
    }
}
