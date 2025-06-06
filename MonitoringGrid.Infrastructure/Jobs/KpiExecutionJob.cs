using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;
using Quartz;

namespace MonitoringGrid.Infrastructure.Jobs;

/// <summary>
/// Quartz.NET job for executing KPIs on schedule
/// </summary>
[DisallowConcurrentExecution]
public class KpiExecutionJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KpiExecutionJob> _logger;

    public KpiExecutionJob(IServiceProvider serviceProvider, ILogger<KpiExecutionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var kpiId = context.JobDetail.JobDataMap.GetInt("KpiId");
        var jobId = context.JobDetail.Key.Name;

        _logger.LogInformation("Starting scheduled execution of KPI {KpiId} (Job: {JobId})", kpiId, jobId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
            var kpiExecutionService = scope.ServiceProvider.GetRequiredService<IKpiExecutionService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

            // Get the KPI
            var kpi = await dbContext.KPIs.FindAsync(kpiId);
            if (kpi == null)
            {
                _logger.LogWarning("KPI {KpiId} not found for scheduled execution", kpiId);
                return;
            }

            if (!kpi.IsActive)
            {
                _logger.LogInformation("KPI {KpiId} is inactive, skipping execution", kpiId);
                return;
            }

            _logger.LogDebug("Executing KPI {Indicator} (ID: {KpiId})", kpi.Indicator, kpiId);

            // Execute the KPI
            var executionResult = await kpiExecutionService.ExecuteKpiAsync(kpi);

            // Update last run time
            kpi.LastRun = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            // Update scheduled job information
            var nextFireTime = context.Trigger.GetNextFireTimeUtc();
            await UpdateScheduledJobInfo(dbContext, jobId, nextFireTime, context.FireTimeUtc);

            // Handle alerts if needed
            if (executionResult.ShouldAlert)
            {
                _logger.LogInformation("KPI {Indicator} triggered alert condition", kpi.Indicator);
                await alertService.SendAlertsAsync(kpi, executionResult);
            }

            _logger.LogInformation("Successfully completed scheduled execution of KPI {Indicator} (ID: {KpiId})", 
                kpi.Indicator, kpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled KPI {KpiId}: {Message}", kpiId, ex.Message);
            
            // Update job with error information
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
            var nextFireTime = context.Trigger.GetNextFireTimeUtc();
            await UpdateScheduledJobInfo(dbContext, jobId, nextFireTime, context.FireTimeUtc, ex.Message);
            
            throw; // Re-throw to let Quartz handle retry logic
        }
    }

    private async Task UpdateScheduledJobInfo(MonitoringContext dbContext, string jobId,
        DateTimeOffset? nextFireTime, DateTimeOffset fireTime, string? errorMessage = null)
    {
        try
        {
            var scheduledJob = await dbContext.ScheduledJobs.FindAsync(jobId);
            if (scheduledJob != null)
            {
                scheduledJob.NextFireTime = nextFireTime?.DateTime;
                scheduledJob.PreviousFireTime = fireTime.DateTime;
                scheduledJob.ModifiedDate = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("Job {JobId} encountered error: {Error}", jobId, errorMessage);
                }
                
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update scheduled job info for {JobId}", jobId);
        }
    }
}

/// <summary>
/// Job listener to handle job execution events
/// </summary>
public class KpiJobListener : IJobListener
{
    private readonly ILogger<KpiJobListener> _logger;

    public KpiJobListener(ILogger<KpiJobListener> logger)
    {
        _logger = logger;
    }

    public string Name => "KpiJobListener";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var kpiId = context.JobDetail.JobDataMap.GetInt("KpiId");
        _logger.LogDebug("KPI job {JobKey} for KPI {KpiId} is about to execute", context.JobDetail.Key, kpiId);
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var kpiId = context.JobDetail.JobDataMap.GetInt("KpiId");
        _logger.LogWarning("KPI job {JobKey} for KPI {KpiId} execution was vetoed", context.JobDetail.Key, kpiId);
        return Task.CompletedTask;
    }

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        var kpiId = context.JobDetail.JobDataMap.GetInt("KpiId");
        var duration = context.JobRunTime;

        if (jobException != null)
        {
            _logger.LogError(jobException, "KPI job {JobKey} for KPI {KpiId} failed after {Duration}ms", 
                context.JobDetail.Key, kpiId, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug("KPI job {JobKey} for KPI {KpiId} completed successfully in {Duration}ms", 
                context.JobDetail.Key, kpiId, duration.TotalMilliseconds);
        }

        return Task.CompletedTask;
    }
}
