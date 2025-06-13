# Worker Service Template

This document describes the Worker Service template that provides background job processing, scheduled tasks, and long-running operations.

## 🏗️ Architecture Overview

The Worker Service implements a robust background processing system with:

- **Job Scheduling**: Multiple scheduling providers (Internal, Hangfire, Quartz)
- **Background Processing**: Long-running tasks and periodic jobs
- **Monitoring & Health Checks**: Comprehensive monitoring and alerting
- **Notification System**: Email notifications for job status and alerts
- **File Storage**: Flexible file storage for exports and processing
- **Retry Logic**: Automatic retry with exponential backoff
- **Performance Monitoring**: Metrics collection and performance tracking

## 📁 Project Structure

```
EnterpriseApp.Worker/
├── Jobs/
│   ├── IJob.cs                          # Job interfaces and base classes
│   ├── BaseJob.cs                       # Base job implementation
│   └── DomainEntityJobs.cs              # Domain-specific jobs
├── Services/
│   ├── JobSchedulerService.cs           # Job scheduling service
│   ├── WorkerBackgroundService.cs       # Main background service
│   ├── NotificationService.cs           # Email notification service
│   └── FileStorageService.cs            # File storage service
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # Service registration
├── Configuration/
│   ├── appsettings.json                 # Base configuration
│   ├── appsettings.Development.json     # Development settings
│   └── appsettings.Production.json      # Production settings
└── Program.cs                           # Application entry point
```

## 🎯 Job System

### Job Interfaces

The job system provides multiple interfaces for different job types:

```csharp
// Basic job interface
public interface IJob
{
    string JobName { get; }
    string Description { get; }
    Task<Result> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken = default);
}

// Parameterized job interface
public interface IJob<in TParameters> : IJob
{
    Task<Result> ExecuteAsync(TParameters parameters, JobExecutionContext context, CancellationToken cancellationToken = default);
}

// Scheduled job interface
public interface IScheduledJob : IJob
{
    string CronExpression { get; }
    TimeZoneInfo TimeZone { get; }
    bool RunOnStartup { get; }
    int MaxRetryAttempts { get; }
    int RetryDelaySeconds { get; }
}

// Recurring job interface
public interface IRecurringJob : IJob
{
    TimeSpan Interval { get; }
    TimeSpan InitialDelay { get; }
    bool RunImmediately { get; }
}
```

### Base Job Classes

All jobs inherit from base classes that provide common functionality:

```csharp
public abstract class BaseJob : IJob
{
    protected readonly ILogger Logger;
    protected readonly IUnitOfWork UnitOfWork;

    public abstract string JobName { get; }
    public abstract string Description { get; }

    protected abstract Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken);

    // Lifecycle hooks
    protected virtual Task OnBeforeExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken);
    protected virtual Task OnAfterExecuteAsync(JobExecutionContext context, Result result, CancellationToken cancellationToken);
    protected virtual Task OnJobCancelledAsync(JobExecutionContext context, CancellationToken cancellationToken);
    protected virtual Task OnJobFailedAsync(JobExecutionContext context, Exception exception, CancellationToken cancellationToken);

    // Helper methods
    protected T GetParameter<T>(JobExecutionContext context, string key, T defaultValue = default!);
    protected void SetMetadata(JobExecutionContext context, string key, object value);
    protected void LogProgress(string message, params object[] args);
}
```

### Example Jobs

#### Cleanup Job

```csharp
public class CleanupInactiveDomainEntitiesJob : BaseScheduledJob
{
    public override string JobName => "CleanupInactiveDomainEntities";
    public override string Description => "Cleans up domain entities that have been inactive for a specified period";
    public override string CronExpression => "0 2 * * *"; // Daily at 2 AM

    protected override async Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var inactiveDays = GetParameter(context, "InactiveDays", 90);
        var batchSize = GetParameter(context, "BatchSize", 100);
        var dryRun = GetParameter(context, "DryRun", false);

        LogProgress("Starting cleanup of inactive domain entities (inactive for {InactiveDays} days)", inactiveDays);

        // Implementation details...
        
        return Result.Success();
    }
}
```

#### Statistics Generation Job

```csharp
public class GenerateDomainEntityStatisticsJob : BaseScheduledJob
{
    public override string JobName => "GenerateDomainEntityStatistics";
    public override string Description => "Generates and caches domain entity statistics";
    public override string CronExpression => "0 * * * *"; // Every hour

    protected override async Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        LogProgress("Starting domain entity statistics generation");

        var statistics = await _domainEntityService.GetStatisticsAsync(cancellationToken);
        await _cacheService.SetAsync("domainentity:statistics:latest", statistics, TimeSpan.FromHours(1), cancellationToken);

        LogProgress("Statistics generated successfully. Total: {Total}, Active: {Active}", 
            statistics.TotalCount, statistics.ActiveCount);

        return Result.Success();
    }
}
```

#### Data Export Job

```csharp
public class ProcessDataExportJob : BaseJob<DataExportParameters>
{
    public override string JobName => "ProcessDataExport";
    public override string Description => "Processes data export requests and sends results via email";

    protected override async Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = GetJobParameters(context);

        LogProgress("Starting data export for user {UserId}, format: {Format}", 
            parameters.UserId, parameters.Format);

        // Get data, generate export file, send email
        var entities = await GetFilteredEntitiesAsync(parameters, cancellationToken);
        var exportData = await GenerateExportDataAsync(entities, parameters.Format, cancellationToken);
        var fileName = await SaveExportFileAsync(exportData, parameters.Format, cancellationToken);
        await SendExportEmailAsync(parameters.EmailAddress, fileName, cancellationToken);

        return Result.Success();
    }
}
```

## 📅 Job Scheduling

### Internal Scheduler

The built-in scheduler provides basic scheduling capabilities:

```csharp
public interface IJobSchedulerService
{
    // Schedule one-time jobs
    Task<string> ScheduleJobAsync<TJob>(DateTime scheduledTime, Dictionary<string, object>? parameters = null);
    Task<string> ScheduleJobAsync<TJob, TParameters>(DateTime scheduledTime, TParameters parameters);

    // Schedule recurring jobs
    Task<string> ScheduleRecurringJobAsync<TJob>(string cronExpression, Dictionary<string, object>? parameters = null);

    // Enqueue immediate jobs
    Task<string> EnqueueJobAsync<TJob>(Dictionary<string, object>? parameters = null);
    Task<string> EnqueueJobAsync<TJob, TParameters>(TParameters parameters);

    // Job management
    Task<bool> CancelJobAsync(string jobId);
    Task<JobExecutionResult?> GetJobStatusAsync(string jobId);
    Task<IEnumerable<ScheduledJobInfo>> GetScheduledJobsAsync();
    Task<IEnumerable<JobExecutionResult>> GetJobHistoryAsync(string? jobName = null, int limit = 100);
}
```

### Hangfire Integration

For production scenarios, Hangfire provides persistent job storage:

```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount;
    options.Queues = new[] { "default", "critical", "background" };
});
```

### Quartz.NET Integration

For advanced scheduling scenarios:

```csharp
services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    
    // Configure jobs
    var jobKey = new JobKey("CleanupInactiveDomainEntities");
    q.AddJob<CleanupInactiveDomainEntitiesJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("CleanupInactiveDomainEntities-trigger")
        .WithCronSchedule("0 2 * * ?") // Daily at 2 AM
    );
});

services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

## 🔧 Background Services

### Main Worker Service

The main background service coordinates all worker operations:

```csharp
public class WorkerBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Background Service starting");

        // Initialize scheduled jobs
        await InitializeScheduledJobsAsync();

        // Start monitoring services
        await StartMonitoringServicesAsync(stoppingToken);

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            await PerformHealthChecksAsync();
        }
    }
}
```

### Monitoring Services

#### Job Queue Monitor

```csharp
public class JobQueueMonitorService : IDisposable
{
    private async Task MonitorJobQueueAsync()
    {
        var scheduledJobs = await jobScheduler.GetScheduledJobsAsync();
        var runningJobs = scheduledJobs.Count(j => j.Status == JobStatus.Running);
        var failedJobs = scheduledJobs.Count(j => j.Status == JobStatus.Failed);

        _logger.LogDebug("Job Queue Status - Running: {Running}, Failed: {Failed}", runningJobs, failedJobs);

        if (failedJobs > 10)
        {
            _logger.LogWarning("High number of failed jobs detected: {FailedCount}", failedJobs);
        }
    }
}
```

#### Performance Monitor

```csharp
public class PerformanceMonitorService : IDisposable
{
    private async Task MonitorPerformanceAsync()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB

        _logger.LogDebug("Performance Metrics - Memory: {Memory}MB", memoryUsage);

        if (memoryUsage > 1024)
        {
            _logger.LogWarning("High memory usage detected: {Memory}MB", memoryUsage);
        }
    }
}
```

## 📧 Notification System

### Email Notifications

```csharp
public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendJobCompletionNotificationAsync(string jobName, bool success, string? details = null, CancellationToken cancellationToken = default);
    Task SendSystemAlertAsync(string alertType, string message, string? details = null, CancellationToken cancellationToken = default);
    Task SendWorkerHealthNotificationAsync(bool isHealthy, string? message = null, CancellationToken cancellationToken = default);
}
```

### Notification Examples

```csharp
// Job completion notification
await _notificationService.SendJobCompletionNotificationAsync(
    "CleanupInactiveDomainEntities", 
    true, 
    "Processed 150 entities, deleted 25 inactive entities");

// System alert
await _notificationService.SendSystemAlertAsync(
    "High Memory Usage", 
    "Worker service memory usage exceeded threshold",
    $"Current usage: {memoryUsage}MB, Threshold: 1024MB");

// Health notification
await _notificationService.SendWorkerHealthNotificationAsync(
    false, 
    "Database connection failed during health check");
```

## 💾 File Storage

### File Storage Interface

```csharp
public interface IFileStorageService
{
    Task<string> SaveFileAsync(string fileName, byte[] content, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<FileMetadata?> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListFilesAsync(string? directoryPath = null, CancellationToken cancellationToken = default);
    Task<string?> GetDownloadUrlAsync(string filePath, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<int> CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}
```

### Local File Storage

```csharp
public class LocalFileStorageService : IFileStorageService
{
    public async Task<string> SaveFileAsync(string fileName, byte[] content, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        // Create subdirectory based on date
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var directoryPath = Path.Combine(_basePath, dateFolder);
        Directory.CreateDirectory(directoryPath);

        // Generate unique file name if file already exists
        var filePath = Path.Combine(directoryPath, fileName);
        // ... handle duplicates

        // Save file and metadata
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);
        
        return Path.GetRelativePath(_basePath, filePath);
    }
}
```

## 📊 Monitoring & Health Checks

### Health Checks

```csharp
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Worker is running"))
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddRedis(redisConnectionString, "redis")
    .AddHangfire(options => options.MinimumAvailableServers = 1);
```

### Metrics Collection

```csharp
public interface IWorkerMetrics
{
    void IncrementJobExecution(string jobName, bool success);
    void RecordJobDuration(string jobName, TimeSpan duration);
    void RecordActiveJobs(int count);
}

public class WorkerMetrics : IWorkerMetrics
{
    private readonly Counter _jobExecutionCounter;
    private readonly Histogram _jobDurationHistogram;
    private readonly Gauge _activeJobsGauge;

    public void IncrementJobExecution(string jobName, bool success)
    {
        _jobExecutionCounter.WithLabels(jobName, success ? "success" : "failure").Inc();
    }
}
```

## ⚙️ Configuration

### Worker Configuration

```json
{
  "Worker": {
    "MaxConcurrentJobs": 5,
    "JobTimeoutMinutes": 30,
    "SchedulingProvider": "Internal",
    "EnableJobPersistence": true,
    "JobHistoryRetentionDays": 30,
    "EnablePerformanceMonitoring": true,
    "HealthCheckIntervalMinutes": 5
  },
  "Jobs": {
    "RunStartupJobs": true,
    "Cleanup": {
      "InactiveDays": 90,
      "BatchSize": 100,
      "DryRun": false
    },
    "Statistics": {
      "CacheExpirationHours": 2
    }
  }
}
```

### Environment-Specific Settings

#### Development
- Reduced concurrency and timeouts
- Dry run mode for destructive operations
- Detailed logging enabled
- Local file storage

#### Production
- Higher concurrency and longer timeouts
- Hangfire for job persistence
- Cloud storage integration
- Comprehensive monitoring

## 🚀 Deployment

### Windows Service

```csharp
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "EnterpriseApp Worker";
    });
}
```

### Linux Systemd

```csharp
else if (OperatingSystem.IsLinux())
{
    builder.Services.AddSystemd();
}
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EnterpriseApp.Worker/EnterpriseApp.Worker.csproj", "EnterpriseApp.Worker/"]
RUN dotnet restore "EnterpriseApp.Worker/EnterpriseApp.Worker.csproj"
COPY . .
WORKDIR "/src/EnterpriseApp.Worker"
RUN dotnet build "EnterpriseApp.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EnterpriseApp.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseApp.Worker.dll"]
```

## 🎯 Key Features

### Job Management
- **Multiple Scheduling Providers**: Internal, Hangfire, Quartz.NET
- **Flexible Job Types**: One-time, recurring, scheduled, parameterized
- **Retry Logic**: Automatic retry with exponential backoff
- **Job Persistence**: Optional job state persistence
- **Job History**: Complete execution history tracking

### Monitoring & Observability
- **Health Checks**: Database, cache, external services
- **Performance Monitoring**: Memory, CPU, job queue metrics
- **Structured Logging**: Comprehensive logging with Serilog
- **Metrics Collection**: Prometheus metrics integration
- **Real-time Notifications**: Email alerts for failures and health issues

### Reliability & Resilience
- **Graceful Shutdown**: Clean job completion on shutdown
- **Circuit Breaker**: HTTP client resilience patterns
- **Timeout Handling**: Configurable job timeouts
- **Error Handling**: Comprehensive error handling and recovery
- **Resource Management**: Automatic cleanup and resource disposal

### Production Features
- **Service Integration**: Windows Service and Linux Systemd support
- **Configuration Management**: Environment-specific configurations
- **Security**: Encryption, secure communication, audit logging
- **Scalability**: Horizontal scaling support with load balancing
- **Maintenance**: Automated cleanup, archiving, and maintenance tasks

The Worker Service template provides a complete, production-ready background processing solution that can handle enterprise workloads with reliability, monitoring, and maintainability! 🎯
