using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Jobs;
using Quartz;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service for managing KPI scheduling with Quartz.NET
/// </summary>
public class KpiSchedulingService : IKpiSchedulingService
{
    private readonly IScheduler _scheduler;
    private readonly MonitoringContext _context;
    private readonly ILogger<KpiSchedulingService> _logger;

    public KpiSchedulingService(
        IScheduler scheduler,
        MonitoringContext context,
        ILogger<KpiSchedulingService> logger)
    {
        _scheduler = scheduler;
        _context = context;
        _logger = logger;
    }

    public async Task ScheduleKpiAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scheduling KPI {Indicator} (ID: {KpiId})", kpi.Indicator, kpi.KpiId);

            // Parse schedule configuration
            var scheduleConfig = ParseScheduleConfiguration(kpi.ScheduleConfiguration);
            if (!scheduleConfig.IsEnabled)
            {
                _logger.LogInformation("KPI {KpiId} scheduling is disabled", kpi.KpiId);
                return;
            }

            // Create job
            var jobId = $"kpi-{kpi.KpiId}";
            var jobKey = new JobKey(jobId, "KPI_JOBS");
            var triggerKey = new TriggerKey($"trigger-{kpi.KpiId}", "KPI_TRIGGERS");

            var job = JobBuilder.Create<KpiExecutionJob>()
                .WithIdentity(jobKey)
                .WithDescription($"Scheduled execution for KPI: {kpi.Indicator}")
                .UsingJobData("KpiId", kpi.KpiId)
                .Build();

            // Create trigger based on schedule type
            var trigger = CreateTrigger(triggerKey, scheduleConfig, kpi);

            // Schedule the job
            await _scheduler.ScheduleJob(job, trigger, cancellationToken);

            // Save scheduled job information
            await SaveScheduledJobInfo(kpi, jobId, triggerKey.Name, scheduleConfig, cancellationToken);

            _logger.LogInformation("Successfully scheduled KPI {Indicator} (ID: {KpiId})", kpi.Indicator, kpi.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule KPI {KpiId}: {Message}", kpi.KpiId, ex.Message);
            throw;
        }
    }

    public async Task UpdateKpiScheduleAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating schedule for KPI {Indicator} (ID: {KpiId})", kpi.Indicator, kpi.KpiId);

            // First unschedule the existing job
            await UnscheduleKpiAsync(kpi.KpiId, cancellationToken);

            // Then schedule with new configuration
            await ScheduleKpiAsync(kpi, cancellationToken);

            _logger.LogInformation("Successfully updated schedule for KPI {Indicator} (ID: {KpiId})", kpi.Indicator, kpi.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update schedule for KPI {KpiId}: {Message}", kpi.KpiId, ex.Message);
            throw;
        }
    }

    public async Task UnscheduleKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unscheduling KPI {KpiId}", kpiId);

            var jobKey = new JobKey($"kpi-{kpiId}", "KPI_JOBS");
            var deleted = await _scheduler.DeleteJob(jobKey, cancellationToken);

            if (deleted)
            {
                // Remove from database
                var scheduledJob = await _context.ScheduledJobs
                    .FirstOrDefaultAsync(sj => sj.KpiId == kpiId, cancellationToken);
                
                if (scheduledJob != null)
                {
                    _context.ScheduledJobs.Remove(scheduledJob);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Successfully unscheduled KPI {KpiId}", kpiId);
            }
            else
            {
                _logger.LogWarning("No scheduled job found for KPI {KpiId}", kpiId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unschedule KPI {KpiId}: {Message}", kpiId, ex.Message);
            throw;
        }
    }

    public async Task<List<ScheduledKpiInfo>> GetScheduledKpisAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduledKpis = new List<ScheduledKpiInfo>();

            var jobs = await _context.ScheduledJobs
                .Include(sj => sj.KPI)
                .Where(sj => sj.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var job in jobs)
            {
                var jobKey = new JobKey(job.JobId, job.JobGroup);
                var triggerKey = new TriggerKey(job.TriggerName, job.TriggerGroup);

                var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken);
                var trigger = await _scheduler.GetTrigger(triggerKey, cancellationToken);

                if (jobDetail != null && trigger != null)
                {
                    var triggerState = await _scheduler.GetTriggerState(triggerKey, cancellationToken);

                    scheduledKpis.Add(new ScheduledKpiInfo
                    {
                        KpiId = job.KpiId,
                        Indicator = job.KPI.Indicator,
                        JobId = job.JobId,
                        JobName = job.JobName,
                        TriggerName = job.TriggerName,
                        NextFireTime = trigger.GetNextFireTimeUtc()?.DateTime,
                        PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.DateTime,
                        ScheduleType = GetScheduleType(job),
                        ScheduleDescription = GetScheduleDescription(job),
                        IsActive = job.IsActive,
                        IsPaused = triggerState == TriggerState.Paused
                    });
                }
            }

            return scheduledKpis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled KPIs: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<DateTime?> GetNextExecutionTimeAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            var triggerKey = new TriggerKey($"trigger-{kpiId}", "KPI_TRIGGERS");
            var trigger = await _scheduler.GetTrigger(triggerKey, cancellationToken);
            return trigger?.GetNextFireTimeUtc()?.DateTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get next execution time for KPI {KpiId}: {Message}", kpiId, ex.Message);
            return null;
        }
    }

    public async Task PauseKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            var triggerKey = new TriggerKey($"trigger-{kpiId}", "KPI_TRIGGERS");
            await _scheduler.PauseTrigger(triggerKey, cancellationToken);
            _logger.LogInformation("Paused KPI {KpiId}", kpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause KPI {KpiId}: {Message}", kpiId, ex.Message);
            throw;
        }
    }

    public async Task ResumeKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            var triggerKey = new TriggerKey($"trigger-{kpiId}", "KPI_TRIGGERS");
            await _scheduler.ResumeTrigger(triggerKey, cancellationToken);
            _logger.LogInformation("Resumed KPI {KpiId}", kpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume KPI {KpiId}: {Message}", kpiId, ex.Message);
            throw;
        }
    }

    public async Task TriggerKpiExecutionAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobKey = new JobKey($"kpi-{kpiId}", "KPI_JOBS");
            await _scheduler.TriggerJob(jobKey, cancellationToken);
            _logger.LogInformation("Triggered immediate execution of KPI {KpiId}", kpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger KPI {KpiId}: {Message}", kpiId, ex.Message);
            throw;
        }
    }

    public Task<ScheduleValidationResult> ValidateScheduleConfigurationAsync(string scheduleConfiguration, CancellationToken cancellationToken = default)
    {
        var result = new ScheduleValidationResult();

        try
        {
            var config = ParseScheduleConfiguration(scheduleConfiguration);

            if (!config.IsEnabled)
            {
                result.IsValid = true;
                result.ScheduleDescription = "Scheduling disabled";
                return Task.FromResult(result);
            }

            // Validate based on schedule type
            switch (config.ScheduleType?.ToLower())
            {
                case "interval":
                    ValidateIntervalSchedule(config, result);
                    break;
                case "cron":
                    ValidateCronSchedule(config, result);
                    break;
                case "onetime":
                    ValidateOneTimeSchedule(config, result);
                    break;
                default:
                    result.Errors.Add("Invalid schedule type");
                    break;
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse schedule configuration: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    // Private helper methods
    private ScheduleConfig ParseScheduleConfiguration(string? scheduleConfiguration)
    {
        if (string.IsNullOrEmpty(scheduleConfiguration))
        {
            return new ScheduleConfig { IsEnabled = false };
        }

        try
        {
            return JsonSerializer.Deserialize<ScheduleConfig>(scheduleConfiguration) ?? new ScheduleConfig { IsEnabled = false };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse schedule configuration: {Configuration}", scheduleConfiguration);
            return new ScheduleConfig { IsEnabled = false };
        }
    }

    private ITrigger CreateTrigger(TriggerKey triggerKey, ScheduleConfig config, KPI kpi)
    {
        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithDescription($"Trigger for KPI: {kpi.Indicator}");

        // Set start time if specified
        if (config.StartDate.HasValue)
        {
            triggerBuilder.StartAt(config.StartDate.Value);
        }
        else
        {
            triggerBuilder.StartNow();
        }

        // Set end time if specified
        if (config.EndDate.HasValue)
        {
            triggerBuilder.EndAt(config.EndDate.Value);
        }

        // Configure schedule based on type
        switch (config.ScheduleType?.ToLower())
        {
            case "interval":
                if (config.IntervalMinutes.HasValue && config.IntervalMinutes > 0)
                {
                    triggerBuilder.WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(config.IntervalMinutes.Value)
                        .RepeatForever());
                }
                else
                {
                    throw new ArgumentException("Invalid interval configuration");
                }
                break;

            case "cron":
                if (!string.IsNullOrEmpty(config.CronExpression))
                {
                    triggerBuilder.WithCronSchedule(config.CronExpression);
                }
                else
                {
                    throw new ArgumentException("Cron expression is required for cron schedule");
                }
                break;

            case "onetime":
                if (config.StartDate.HasValue)
                {
                    // For one-time execution, don't repeat
                    triggerBuilder.WithSimpleSchedule(x => x.WithRepeatCount(0));
                }
                else
                {
                    throw new ArgumentException("Start date is required for one-time schedule");
                }
                break;

            default:
                throw new ArgumentException($"Unsupported schedule type: {config.ScheduleType}");
        }

        return triggerBuilder.Build();
    }

    private async Task SaveScheduledJobInfo(KPI kpi, string jobId, string triggerName, ScheduleConfig config, CancellationToken cancellationToken)
    {
        var scheduledJob = new ScheduledJob
        {
            JobId = jobId,
            KpiId = kpi.KpiId,
            JobName = $"KPI-{kpi.Indicator}",
            JobGroup = "KPI_JOBS",
            TriggerName = triggerName,
            TriggerGroup = "KPI_TRIGGERS",
            CronExpression = config.CronExpression,
            IntervalMinutes = config.IntervalMinutes,
            StartTime = config.StartDate,
            EndTime = config.EndDate,
            IsActive = true
        };

        _context.ScheduledJobs.Add(scheduledJob);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GetScheduleType(ScheduledJob job)
    {
        if (!string.IsNullOrEmpty(job.CronExpression))
            return "Cron";
        if (job.IntervalMinutes.HasValue)
            return "Interval";
        return "Unknown";
    }

    private string GetScheduleDescription(ScheduledJob job)
    {
        if (!string.IsNullOrEmpty(job.CronExpression))
            return $"Cron: {job.CronExpression}";
        if (job.IntervalMinutes.HasValue)
            return $"Every {job.IntervalMinutes} minutes";
        return "Unknown schedule";
    }

    private void ValidateIntervalSchedule(ScheduleConfig config, ScheduleValidationResult result)
    {
        if (!config.IntervalMinutes.HasValue || config.IntervalMinutes <= 0)
        {
            result.Errors.Add("Interval must be greater than 0 minutes");
        }
        else if (config.IntervalMinutes > 10080) // 1 week
        {
            result.Warnings.Add("Interval is longer than 1 week");
        }

        if (config.IntervalMinutes.HasValue)
        {
            result.ScheduleDescription = $"Every {config.IntervalMinutes} minutes";
            result.NextExecutionTime = DateTime.UtcNow.AddMinutes(config.IntervalMinutes.Value);
        }
    }

    private void ValidateCronSchedule(ScheduleConfig config, ScheduleValidationResult result)
    {
        if (string.IsNullOrEmpty(config.CronExpression))
        {
            result.Errors.Add("Cron expression is required");
            return;
        }

        try
        {
            var cronExpression = new CronExpression(config.CronExpression);
            result.ScheduleDescription = $"Cron: {config.CronExpression}";
            result.NextExecutionTime = cronExpression.GetNextValidTimeAfter(DateTime.UtcNow)?.DateTime;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Invalid cron expression: {ex.Message}");
        }
    }

    private void ValidateOneTimeSchedule(ScheduleConfig config, ScheduleValidationResult result)
    {
        if (!config.StartDate.HasValue)
        {
            result.Errors.Add("Start date is required for one-time schedule");
        }
        else if (config.StartDate <= DateTime.UtcNow)
        {
            result.Errors.Add("Start date must be in the future");
        }
        else
        {
            result.ScheduleDescription = $"Once at {config.StartDate:yyyy-MM-dd HH:mm:ss} UTC";
            result.NextExecutionTime = config.StartDate;
        }
    }
}

/// <summary>
/// Internal class for parsing schedule configuration JSON
/// </summary>
internal class ScheduleConfig
{
    public string? ScheduleType { get; set; }
    public string? CronExpression { get; set; }
    public int? IntervalMinutes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Timezone { get; set; }
    public bool IsEnabled { get; set; }
}
