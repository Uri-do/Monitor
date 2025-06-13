using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Worker.Jobs;

/// <summary>
/// Interface for background jobs
/// </summary>
public interface IJob
{
    /// <summary>
    /// Job name for identification
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Job description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the job
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job execution result</returns>
    Task<Result> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for parameterized jobs
/// </summary>
/// <typeparam name="TParameters">Type of job parameters</typeparam>
public interface IJob<in TParameters> : IJob
{
    /// <summary>
    /// Executes the job with parameters
    /// </summary>
    /// <param name="parameters">Job parameters</param>
    /// <param name="context">Job execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job execution result</returns>
    Task<Result> ExecuteAsync(TParameters parameters, JobExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for scheduled jobs
/// </summary>
public interface IScheduledJob : IJob
{
    /// <summary>
    /// Cron expression for scheduling
    /// </summary>
    string CronExpression { get; }

    /// <summary>
    /// Time zone for scheduling
    /// </summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Indicates if the job should run on startup
    /// </summary>
    bool RunOnStartup { get; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    int MaxRetryAttempts { get; }

    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    int RetryDelaySeconds { get; }
}

/// <summary>
/// Interface for recurring jobs
/// </summary>
public interface IRecurringJob : IJob
{
    /// <summary>
    /// Interval between executions
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Delay before first execution
    /// </summary>
    TimeSpan InitialDelay { get; }

    /// <summary>
    /// Indicates if the job should run immediately on startup
    /// </summary>
    bool RunImmediately { get; }
}

/// <summary>
/// Job execution context
/// </summary>
public class JobExecutionContext
{
    /// <summary>
    /// Unique job execution ID
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Job name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled execution time
    /// </summary>
    public DateTime ScheduledTime { get; set; }

    /// <summary>
    /// Actual execution start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Job parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Execution metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Retry attempt number (0 for first attempt)
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Previous execution results (for retry scenarios)
    /// </summary>
    public List<JobExecutionResult> PreviousResults { get; set; } = new();

    /// <summary>
    /// Cancellation token for the job execution
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Logger for the job execution
    /// </summary>
    public ILogger Logger { get; set; } = null!;
}

/// <summary>
/// Job execution result
/// </summary>
public class JobExecutionResult
{
    /// <summary>
    /// Execution ID
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Job name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Indicates if the job executed successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception details if execution failed
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Job output or result data
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Execution metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Retry attempt number
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Next scheduled execution time (for recurring jobs)
    /// </summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static JobExecutionResult Success(string executionId, string jobName, DateTime startTime, string? output = null)
    {
        return new JobExecutionResult
        {
            ExecutionId = executionId,
            JobName = jobName,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            IsSuccess = true,
            Output = output
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static JobExecutionResult Failure(string executionId, string jobName, DateTime startTime, string errorMessage, Exception? exception = null)
    {
        return new JobExecutionResult
        {
            ExecutionId = executionId,
            JobName = jobName,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExceptionDetails = exception?.ToString()
        };
    }
}

/// <summary>
/// Job status enumeration
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is scheduled but not yet running
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Job is retrying after failure
    /// </summary>
    Retrying = 5,

    /// <summary>
    /// Job is paused
    /// </summary>
    Paused = 6,

    /// <summary>
    /// Job is disabled
    /// </summary>
    Disabled = 7
}

/// <summary>
/// Job priority enumeration
/// </summary>
public enum JobPriority
{
    /// <summary>
    /// Low priority
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority
    /// </summary>
    Critical = 3
}

/// <summary>
/// Job configuration
/// </summary>
public class JobConfiguration
{
    /// <summary>
    /// Job name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Job description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Job type
    /// </summary>
    public Type JobType { get; set; } = null!;

    /// <summary>
    /// Cron expression for scheduled jobs
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Interval for recurring jobs
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Job priority
    /// </summary>
    public JobPriority Priority { get; set; } = JobPriority.Normal;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Timeout in minutes
    /// </summary>
    public int TimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Indicates if the job is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Job parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Job metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
