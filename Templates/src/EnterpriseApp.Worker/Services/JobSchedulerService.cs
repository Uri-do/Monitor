using System.Collections.Concurrent;
using NCrontab;
using EnterpriseApp.Worker.Jobs;
using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Worker.Services;

/// <summary>
/// Service for scheduling and managing background jobs
/// </summary>
public interface IJobSchedulerService
{
    /// <summary>
    /// Schedules a job to run at a specific time
    /// </summary>
    Task<string> ScheduleJobAsync<TJob>(DateTime scheduledTime, Dictionary<string, object>? parameters = null) where TJob : class, IJob;

    /// <summary>
    /// Schedules a job with parameters to run at a specific time
    /// </summary>
    Task<string> ScheduleJobAsync<TJob, TParameters>(DateTime scheduledTime, TParameters parameters) where TJob : class, IJob<TParameters>;

    /// <summary>
    /// Schedules a recurring job with a cron expression
    /// </summary>
    Task<string> ScheduleRecurringJobAsync<TJob>(string cronExpression, Dictionary<string, object>? parameters = null) where TJob : class, IJob;

    /// <summary>
    /// Enqueues a job to run immediately
    /// </summary>
    Task<string> EnqueueJobAsync<TJob>(Dictionary<string, object>? parameters = null) where TJob : class, IJob;

    /// <summary>
    /// Enqueues a job with parameters to run immediately
    /// </summary>
    Task<string> EnqueueJobAsync<TJob, TParameters>(TParameters parameters) where TJob : class, IJob<TParameters>;

    /// <summary>
    /// Cancels a scheduled job
    /// </summary>
    Task<bool> CancelJobAsync(string jobId);

    /// <summary>
    /// Gets the status of a job
    /// </summary>
    Task<JobExecutionResult?> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Gets all scheduled jobs
    /// </summary>
    Task<IEnumerable<ScheduledJobInfo>> GetScheduledJobsAsync();

    /// <summary>
    /// Gets job execution history
    /// </summary>
    Task<IEnumerable<JobExecutionResult>> GetJobHistoryAsync(string? jobName = null, int limit = 100);
}

/// <summary>
/// Implementation of job scheduler service
/// </summary>
public class JobSchedulerService : IJobSchedulerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerService> _logger;
    private readonly ConcurrentDictionary<string, ScheduledJobInfo> _scheduledJobs = new();
    private readonly ConcurrentDictionary<string, JobExecutionResult> _jobResults = new();
    private readonly Timer _schedulerTimer;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the JobSchedulerService
    /// </summary>
    public JobSchedulerService(IServiceProvider serviceProvider, ILogger<JobSchedulerService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Start the scheduler timer (check every minute)
        _schedulerTimer = new Timer(ProcessScheduledJobs, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Schedules a job to run at a specific time
    /// </summary>
    public Task<string> ScheduleJobAsync<TJob>(DateTime scheduledTime, Dictionary<string, object>? parameters = null) where TJob : class, IJob
    {
        var jobId = Guid.NewGuid().ToString();
        var jobInfo = new ScheduledJobInfo
        {
            JobId = jobId,
            JobType = typeof(TJob),
            JobName = typeof(TJob).Name,
            ScheduledTime = scheduledTime,
            Parameters = parameters ?? new Dictionary<string, object>(),
            Status = JobStatus.Scheduled,
            CreatedTime = DateTime.UtcNow
        };

        _scheduledJobs.TryAdd(jobId, jobInfo);
        
        _logger.LogInformation("Job scheduled: {JobName} (ID: {JobId}) at {ScheduledTime}", 
            jobInfo.JobName, jobId, scheduledTime);

        return Task.FromResult(jobId);
    }

    /// <summary>
    /// Schedules a job with parameters to run at a specific time
    /// </summary>
    public Task<string> ScheduleJobAsync<TJob, TParameters>(DateTime scheduledTime, TParameters parameters) where TJob : class, IJob<TParameters>
    {
        var jobParameters = new Dictionary<string, object> { ["JobParameters"] = parameters! };
        return ScheduleJobAsync<TJob>(scheduledTime, jobParameters);
    }

    /// <summary>
    /// Schedules a recurring job with a cron expression
    /// </summary>
    public Task<string> ScheduleRecurringJobAsync<TJob>(string cronExpression, Dictionary<string, object>? parameters = null) where TJob : class, IJob
    {
        var jobId = Guid.NewGuid().ToString();
        var schedule = CrontabSchedule.Parse(cronExpression);
        var nextRun = schedule.GetNextOccurrence(DateTime.UtcNow);

        var jobInfo = new ScheduledJobInfo
        {
            JobId = jobId,
            JobType = typeof(TJob),
            JobName = typeof(TJob).Name,
            ScheduledTime = nextRun,
            Parameters = parameters ?? new Dictionary<string, object>(),
            Status = JobStatus.Scheduled,
            CreatedTime = DateTime.UtcNow,
            CronExpression = cronExpression,
            IsRecurring = true
        };

        _scheduledJobs.TryAdd(jobId, jobInfo);
        
        _logger.LogInformation("Recurring job scheduled: {JobName} (ID: {JobId}) with cron: {CronExpression}, next run: {NextRun}", 
            jobInfo.JobName, jobId, cronExpression, nextRun);

        return Task.FromResult(jobId);
    }

    /// <summary>
    /// Enqueues a job to run immediately
    /// </summary>
    public Task<string> EnqueueJobAsync<TJob>(Dictionary<string, object>? parameters = null) where TJob : class, IJob
    {
        return ScheduleJobAsync<TJob>(DateTime.UtcNow, parameters);
    }

    /// <summary>
    /// Enqueues a job with parameters to run immediately
    /// </summary>
    public Task<string> EnqueueJobAsync<TJob, TParameters>(TParameters parameters) where TJob : class, IJob<TParameters>
    {
        return ScheduleJobAsync<TJob, TParameters>(DateTime.UtcNow, parameters);
    }

    /// <summary>
    /// Cancels a scheduled job
    /// </summary>
    public Task<bool> CancelJobAsync(string jobId)
    {
        if (_scheduledJobs.TryGetValue(jobId, out var jobInfo))
        {
            if (jobInfo.Status == JobStatus.Scheduled)
            {
                jobInfo.Status = JobStatus.Cancelled;
                _logger.LogInformation("Job cancelled: {JobName} (ID: {JobId})", jobInfo.JobName, jobId);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Gets the status of a job
    /// </summary>
    public Task<JobExecutionResult?> GetJobStatusAsync(string jobId)
    {
        _jobResults.TryGetValue(jobId, out var result);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets all scheduled jobs
    /// </summary>
    public Task<IEnumerable<ScheduledJobInfo>> GetScheduledJobsAsync()
    {
        return Task.FromResult(_scheduledJobs.Values.AsEnumerable());
    }

    /// <summary>
    /// Gets job execution history
    /// </summary>
    public Task<IEnumerable<JobExecutionResult>> GetJobHistoryAsync(string? jobName = null, int limit = 100)
    {
        var results = _jobResults.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(jobName))
        {
            results = results.Where(r => r.JobName.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        }

        results = results.OrderByDescending(r => r.StartTime).Take(limit);

        return Task.FromResult(results);
    }

    /// <summary>
    /// Processes scheduled jobs
    /// </summary>
    private async void ProcessScheduledJobs(object? state)
    {
        var now = DateTime.UtcNow;
        var jobsToRun = new List<ScheduledJobInfo>();

        lock (_lockObject)
        {
            foreach (var kvp in _scheduledJobs)
            {
                var jobInfo = kvp.Value;
                if (jobInfo.Status == JobStatus.Scheduled && jobInfo.ScheduledTime <= now)
                {
                    jobInfo.Status = JobStatus.Running;
                    jobsToRun.Add(jobInfo);
                }
            }
        }

        foreach (var jobInfo in jobsToRun)
        {
            _ = Task.Run(async () => await ExecuteJobAsync(jobInfo));
        }
    }

    /// <summary>
    /// Executes a scheduled job
    /// </summary>
    private async Task ExecuteJobAsync(ScheduledJobInfo jobInfo)
    {
        var startTime = DateTime.UtcNow;
        var context = new JobExecutionContext
        {
            ExecutionId = Guid.NewGuid().ToString(),
            JobName = jobInfo.JobName,
            ScheduledTime = jobInfo.ScheduledTime,
            StartTime = startTime,
            Parameters = jobInfo.Parameters,
            ServiceProvider = _serviceProvider,
            Logger = _logger
        };

        try
        {
            _logger.LogInformation("Executing job: {JobName} (ID: {JobId}, ExecutionId: {ExecutionId})", 
                jobInfo.JobName, jobInfo.JobId, context.ExecutionId);

            // Create job instance
            var job = (IJob)_serviceProvider.GetRequiredService(jobInfo.JobType);

            // Execute the job
            var result = await job.ExecuteAsync(context, CancellationToken.None);

            var executionResult = result.IsSuccess
                ? JobExecutionResult.Success(context.ExecutionId, jobInfo.JobName, startTime)
                : JobExecutionResult.Failure(context.ExecutionId, jobInfo.JobName, startTime, result.Error.Message);

            executionResult.Metadata = context.Metadata;

            // Store the result
            _jobResults.TryAdd(context.ExecutionId, executionResult);

            // Update job status
            if (jobInfo.IsRecurring && !string.IsNullOrEmpty(jobInfo.CronExpression))
            {
                // Schedule next occurrence
                var schedule = CrontabSchedule.Parse(jobInfo.CronExpression);
                jobInfo.ScheduledTime = schedule.GetNextOccurrence(DateTime.UtcNow);
                jobInfo.Status = JobStatus.Scheduled;
                jobInfo.LastExecutionTime = startTime;
                executionResult.NextExecution = jobInfo.ScheduledTime;

                _logger.LogInformation("Recurring job {JobName} completed, next run: {NextRun}", 
                    jobInfo.JobName, jobInfo.ScheduledTime);
            }
            else
            {
                // Remove one-time job
                jobInfo.Status = result.IsSuccess ? JobStatus.Completed : JobStatus.Failed;
                jobInfo.LastExecutionTime = startTime;
            }

            _logger.LogInformation("Job execution completed: {JobName} (ExecutionId: {ExecutionId}) - Success: {Success}", 
                jobInfo.JobName, context.ExecutionId, result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job execution failed: {JobName} (ID: {JobId})", jobInfo.JobName, jobInfo.JobId);

            var executionResult = JobExecutionResult.Failure(context.ExecutionId, jobInfo.JobName, startTime, ex.Message, ex);
            _jobResults.TryAdd(context.ExecutionId, executionResult);

            jobInfo.Status = JobStatus.Failed;
            jobInfo.LastExecutionTime = startTime;
        }
    }

    /// <summary>
    /// Disposes the scheduler
    /// </summary>
    public void Dispose()
    {
        _schedulerTimer?.Dispose();
    }
}

/// <summary>
/// Information about a scheduled job
/// </summary>
public class ScheduledJobInfo
{
    /// <summary>
    /// Job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job type
    /// </summary>
    public Type JobType { get; set; } = null!;

    /// <summary>
    /// Job name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled execution time
    /// </summary>
    public DateTime ScheduledTime { get; set; }

    /// <summary>
    /// Job parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Job status
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// When the job was created/scheduled
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// Last execution time
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Cron expression for recurring jobs
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Indicates if this is a recurring job
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Job priority
    /// </summary>
    public JobPriority Priority { get; set; } = JobPriority.Normal;
}
