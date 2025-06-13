using System.Diagnostics;
using EnterpriseApp.Core.Common;
using EnterpriseApp.Core.Interfaces;

namespace EnterpriseApp.Worker.Jobs;

/// <summary>
/// Base class for all jobs
/// </summary>
public abstract class BaseJob : IJob
{
    /// <summary>
    /// Logger instance
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Unit of work for database operations
    /// </summary>
    protected readonly IUnitOfWork UnitOfWork;

    /// <summary>
    /// Initializes a new instance of the BaseJob class
    /// </summary>
    protected BaseJob(ILogger logger, IUnitOfWork unitOfWork)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Job name for identification
    /// </summary>
    public abstract string JobName { get; }

    /// <summary>
    /// Job description
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Executes the job
    /// </summary>
    public async Task<Result> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogInformation("Starting job execution: {JobName} (ExecutionId: {ExecutionId})", 
                JobName, context.ExecutionId);

            // Pre-execution hook
            await OnBeforeExecuteAsync(context, cancellationToken);

            // Execute the job
            var result = await ExecuteJobAsync(context, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                Logger.LogInformation("Job completed successfully: {JobName} in {Duration}ms (ExecutionId: {ExecutionId})", 
                    JobName, stopwatch.ElapsedMilliseconds, context.ExecutionId);

                // Post-execution hook
                await OnAfterExecuteAsync(context, result, cancellationToken);
            }
            else
            {
                Logger.LogError("Job failed: {JobName} - {Error} (ExecutionId: {ExecutionId})", 
                    JobName, result.Error.Message, context.ExecutionId);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            Logger.LogWarning("Job cancelled: {JobName} after {Duration}ms (ExecutionId: {ExecutionId})", 
                JobName, stopwatch.ElapsedMilliseconds, context.ExecutionId);

            await OnJobCancelledAsync(context, cancellationToken);
            return Result.Failure(Error.Failure("Job.Cancelled", "Job execution was cancelled"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Job failed with exception: {JobName} after {Duration}ms (ExecutionId: {ExecutionId})", 
                JobName, stopwatch.ElapsedMilliseconds, context.ExecutionId);

            await OnJobFailedAsync(context, ex, cancellationToken);
            return Result.Failure(Error.Failure("Job.Exception", ex.Message));
        }
    }

    /// <summary>
    /// Executes the actual job logic
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job execution result</returns>
    protected abstract Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Called before job execution
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual Task OnBeforeExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after successful job execution
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="result">Job execution result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual Task OnAfterExecuteAsync(JobExecutionContext context, Result result, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when job is cancelled
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual Task OnJobCancelledAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when job fails with exception
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="exception">Exception that caused the failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual Task OnJobFailedAsync(JobExecutionContext context, Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a parameter value from the job context
    /// </summary>
    /// <typeparam name="T">Parameter type</typeparam>
    /// <param name="context">Job execution context</param>
    /// <param name="key">Parameter key</param>
    /// <param name="defaultValue">Default value if parameter not found</param>
    /// <returns>Parameter value</returns>
    protected T GetParameter<T>(JobExecutionContext context, string key, T defaultValue = default!)
    {
        if (context.Parameters.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to convert parameter {Key} to type {Type}, using default value", key, typeof(T).Name);
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Sets a metadata value in the job context
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    protected void SetMetadata(JobExecutionContext context, string key, object value)
    {
        context.Metadata[key] = value;
    }

    /// <summary>
    /// Gets a metadata value from the job context
    /// </summary>
    /// <typeparam name="T">Metadata type</typeparam>
    /// <param name="context">Job execution context</param>
    /// <param name="key">Metadata key</param>
    /// <param name="defaultValue">Default value if metadata not found</param>
    /// <returns>Metadata value</returns>
    protected T GetMetadata<T>(JobExecutionContext context, string key, T defaultValue = default!)
    {
        if (context.Metadata.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to convert metadata {Key} to type {Type}, using default value", key, typeof(T).Name);
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Checks if the job should continue execution
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if job should continue</returns>
    protected bool ShouldContinue(CancellationToken cancellationToken)
    {
        return !cancellationToken.IsCancellationRequested;
    }

    /// <summary>
    /// Logs progress information
    /// </summary>
    /// <param name="message">Progress message</param>
    /// <param name="args">Message arguments</param>
    protected void LogProgress(string message, params object[] args)
    {
        Logger.LogInformation($"[{JobName}] {message}", args);
    }

    /// <summary>
    /// Logs warning information
    /// </summary>
    /// <param name="message">Warning message</param>
    /// <param name="args">Message arguments</param>
    protected void LogWarning(string message, params object[] args)
    {
        Logger.LogWarning($"[{JobName}] {message}", args);
    }

    /// <summary>
    /// Logs error information
    /// </summary>
    /// <param name="exception">Exception</param>
    /// <param name="message">Error message</param>
    /// <param name="args">Message arguments</param>
    protected void LogError(Exception exception, string message, params object[] args)
    {
        Logger.LogError(exception, $"[{JobName}] {message}", args);
    }
}

/// <summary>
/// Base class for parameterized jobs
/// </summary>
/// <typeparam name="TParameters">Type of job parameters</typeparam>
public abstract class BaseJob<TParameters> : BaseJob, IJob<TParameters>
{
    /// <summary>
    /// Initializes a new instance of the BaseJob class
    /// </summary>
    protected BaseJob(ILogger logger, IUnitOfWork unitOfWork) : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Executes the job with parameters
    /// </summary>
    public async Task<Result> ExecuteAsync(TParameters parameters, JobExecutionContext context, CancellationToken cancellationToken = default)
    {
        // Store parameters in context
        context.Parameters["JobParameters"] = parameters!;

        return await ExecuteAsync(context, cancellationToken);
    }

    /// <summary>
    /// Gets the job parameters from the context
    /// </summary>
    /// <param name="context">Job execution context</param>
    /// <returns>Job parameters</returns>
    protected TParameters GetJobParameters(JobExecutionContext context)
    {
        if (context.Parameters.TryGetValue("JobParameters", out var parameters) && parameters is TParameters typedParameters)
        {
            return typedParameters;
        }

        throw new InvalidOperationException("Job parameters not found or invalid type");
    }
}

/// <summary>
/// Base class for scheduled jobs
/// </summary>
public abstract class BaseScheduledJob : BaseJob, IScheduledJob
{
    /// <summary>
    /// Initializes a new instance of the BaseScheduledJob class
    /// </summary>
    protected BaseScheduledJob(ILogger logger, IUnitOfWork unitOfWork) : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Cron expression for scheduling
    /// </summary>
    public abstract string CronExpression { get; }

    /// <summary>
    /// Time zone for scheduling
    /// </summary>
    public virtual TimeZoneInfo TimeZone => TimeZoneInfo.Utc;

    /// <summary>
    /// Indicates if the job should run on startup
    /// </summary>
    public virtual bool RunOnStartup => false;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public virtual int MaxRetryAttempts => 3;

    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    public virtual int RetryDelaySeconds => 60;
}

/// <summary>
/// Base class for recurring jobs
/// </summary>
public abstract class BaseRecurringJob : BaseJob, IRecurringJob
{
    /// <summary>
    /// Initializes a new instance of the BaseRecurringJob class
    /// </summary>
    protected BaseRecurringJob(ILogger logger, IUnitOfWork unitOfWork) : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Interval between executions
    /// </summary>
    public abstract TimeSpan Interval { get; }

    /// <summary>
    /// Delay before first execution
    /// </summary>
    public virtual TimeSpan InitialDelay => TimeSpan.Zero;

    /// <summary>
    /// Indicates if the job should run immediately on startup
    /// </summary>
    public virtual bool RunImmediately => false;
}
