using Hangfire;
using Hangfire.SqlServer;
using Quartz;
using EnterpriseApp.Worker.Jobs;
using EnterpriseApp.Worker.Services;
<!--#if (enableMonitoring)-->
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
<!--#endif-->
<!--#if (enableRealtime)-->
using Microsoft.AspNetCore.SignalR.Client;
<!--#endif-->
using Polly;
using Polly.Extensions.Http;

namespace EnterpriseApp.Worker.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds worker services
    /// </summary>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register job scheduler
        services.AddSingleton<IJobSchedulerService, JobSchedulerService>();

        // Register job types
        services.AddTransient<CleanupInactiveDomainEntitiesJob>();
        services.AddTransient<GenerateDomainEntityStatisticsJob>();
        services.AddTransient<ProcessDataExportJob>();

        // Register worker configuration
        services.Configure<WorkerConfiguration>(configuration.GetSection("Worker"));

        return services;
    }

    /// <summary>
    /// Adds job scheduling services
    /// </summary>
    public static IServiceCollection AddJobScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        var schedulingProvider = configuration.GetValue<string>("Worker:SchedulingProvider", "Internal");

        switch (schedulingProvider.ToLower())
        {
            case "hangfire":
                services.AddHangfireScheduling(configuration);
                break;
            case "quartz":
                services.AddQuartzScheduling(configuration);
                break;
            default:
                // Use internal scheduler (already registered in AddWorkerServices)
                break;
        }

        return services;
    }

    /// <summary>
    /// Adds Hangfire scheduling
    /// </summary>
    private static IServiceCollection AddHangfireScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
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
            options.WorkerCount = configuration.GetValue<int>("Worker:Hangfire:WorkerCount", Environment.ProcessorCount);
            options.Queues = configuration.GetSection("Worker:Hangfire:Queues").Get<string[]>() ?? new[] { "default" };
        });

        return services;
    }

    /// <summary>
    /// Adds Quartz scheduling
    /// </summary>
    private static IServiceCollection AddQuartzScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = configuration.GetValue<int>("Worker:Quartz:MaxConcurrency", 10);
            });

            // Configure jobs
            ConfigureQuartzJobs(q, configuration);
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }

    /// <summary>
    /// Configures Quartz jobs
    /// </summary>
    private static void ConfigureQuartzJobs(IServiceCollectionQuartzConfigurator q, IConfiguration configuration)
    {
        // Cleanup job
        var cleanupJobKey = new JobKey("CleanupInactiveDomainEntities");
        q.AddJob<CleanupInactiveDomainEntitiesJob>(opts => opts.WithIdentity(cleanupJobKey));
        q.AddTrigger(opts => opts
            .ForJob(cleanupJobKey)
            .WithIdentity("CleanupInactiveDomainEntities-trigger")
            .WithCronSchedule("0 2 * * ?") // Daily at 2 AM
        );

        // Statistics job
        var statsJobKey = new JobKey("GenerateDomainEntityStatistics");
        q.AddJob<GenerateDomainEntityStatisticsJob>(opts => opts.WithIdentity(statsJobKey));
        q.AddTrigger(opts => opts
            .ForJob(statsJobKey)
            .WithIdentity("GenerateDomainEntityStatistics-trigger")
            .WithCronSchedule("0 0 * * * ?") // Every hour
        );
    }

    /// <summary>
    /// Adds background services
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<WorkerBackgroundService>();
        return services;
    }

<!--#if (enableMonitoring)-->
    /// <summary>
    /// Adds worker health checks
    /// </summary>
    public static IServiceCollection AddWorkerHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Worker is running"))
            .AddDbContextCheck<EnterpriseApp.Infrastructure.Data.ApplicationDbContext>("database");

        // Add Redis health check if configured
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddHealthChecks().AddRedis(redisConnectionString, "redis");
        }

        // Add Hangfire health check if configured
        var schedulingProvider = configuration.GetValue<string>("Worker:SchedulingProvider", "Internal");
        if (schedulingProvider.Equals("hangfire", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHealthChecks().AddHangfire(options =>
            {
                options.MinimumAvailableServers = 1;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds worker metrics
    /// </summary>
    public static IServiceCollection AddWorkerMetrics(this IServiceCollection services)
    {
        // Register custom metrics
        services.AddSingleton<IWorkerMetrics, WorkerMetrics>();
        
        return services;
    }
<!--#endif-->

<!--#if (enableRealtime)-->
    /// <summary>
    /// Adds SignalR client for real-time communication
    /// </summary>
    public static IServiceCollection AddSignalRClient(this IServiceCollection services, IConfiguration configuration)
    {
        var hubUrl = configuration.GetValue<string>("SignalR:HubUrl");
        if (!string.IsNullOrEmpty(hubUrl))
        {
            services.AddSingleton<HubConnection>(provider =>
            {
                return new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build();
            });

            services.AddSingleton<ISignalRClientService, SignalRClientService>();
        }

        return services;
    }
<!--#endif-->

    /// <summary>
    /// Adds HTTP clients with retry policies
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Add retry policy
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                    logger?.LogWarning("HTTP retry attempt {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });

        // Add circuit breaker policy
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));

        // Combine policies
        var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

        // Register HTTP clients
        services.AddHttpClient("ApiClient", client =>
        {
            var baseUrl = configuration.GetValue<string>("ExternalServices:ApiBaseUrl");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddPolicyHandler(combinedPolicy);

        services.AddHttpClient("NotificationClient", client =>
        {
            var baseUrl = configuration.GetValue<string>("ExternalServices:NotificationBaseUrl");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
            client.Timeout = TimeSpan.FromMinutes(2);
        })
        .AddPolicyHandler(retryPolicy);

        return services;
    }

    /// <summary>
    /// Adds worker configuration validation
    /// </summary>
    public static IServiceCollection AddWorkerConfigurationValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WorkerConfiguration>()
            .Bind(configuration.GetSection("Worker"))
            .ValidateDataAnnotations()
            .Validate(config =>
            {
                // Custom validation logic
                if (config.MaxConcurrentJobs <= 0)
                    return false;
                
                if (config.JobTimeoutMinutes <= 0)
                    return false;

                return true;
            }, "Worker configuration is invalid");

        return services;
    }
}

/// <summary>
/// Worker configuration
/// </summary>
public class WorkerConfiguration
{
    /// <summary>
    /// Maximum number of concurrent jobs
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 5;

    /// <summary>
    /// Job timeout in minutes
    /// </summary>
    public int JobTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Scheduling provider (Internal, Hangfire, Quartz)
    /// </summary>
    public string SchedulingProvider { get; set; } = "Internal";

    /// <summary>
    /// Enable job persistence
    /// </summary>
    public bool EnableJobPersistence { get; set; } = true;

    /// <summary>
    /// Job history retention days
    /// </summary>
    public int JobHistoryRetentionDays { get; set; } = 30;

    /// <summary>
    /// Enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Health check interval in minutes
    /// </summary>
    public int HealthCheckIntervalMinutes { get; set; } = 5;
}

<!--#if (enableMonitoring)-->
/// <summary>
/// Interface for worker metrics
/// </summary>
public interface IWorkerMetrics
{
    /// <summary>
    /// Increments job execution counter
    /// </summary>
    void IncrementJobExecution(string jobName, bool success);

    /// <summary>
    /// Records job execution duration
    /// </summary>
    void RecordJobDuration(string jobName, TimeSpan duration);

    /// <summary>
    /// Records active job count
    /// </summary>
    void RecordActiveJobs(int count);
}

/// <summary>
/// Implementation of worker metrics
/// </summary>
public class WorkerMetrics : IWorkerMetrics
{
    private readonly Counter _jobExecutionCounter;
    private readonly Histogram _jobDurationHistogram;
    private readonly Gauge _activeJobsGauge;

    public WorkerMetrics()
    {
        _jobExecutionCounter = Metrics
            .CreateCounter("worker_job_executions_total", "Total number of job executions", new[] { "job_name", "status" });

        _jobDurationHistogram = Metrics
            .CreateHistogram("worker_job_duration_seconds", "Job execution duration in seconds", new[] { "job_name" });

        _activeJobsGauge = Metrics
            .CreateGauge("worker_active_jobs", "Number of currently active jobs");
    }

    public void IncrementJobExecution(string jobName, bool success)
    {
        _jobExecutionCounter.WithLabels(jobName, success ? "success" : "failure").Inc();
    }

    public void RecordJobDuration(string jobName, TimeSpan duration)
    {
        _jobDurationHistogram.WithLabels(jobName).Observe(duration.TotalSeconds);
    }

    public void RecordActiveJobs(int count)
    {
        _activeJobsGauge.Set(count);
    }
}
<!--#endif-->

<!--#if (enableRealtime)-->
/// <summary>
/// Interface for SignalR client service
/// </summary>
public interface ISignalRClientService
{
    /// <summary>
    /// Starts the SignalR connection
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the SignalR connection
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a job status update
    /// </summary>
    Task SendJobStatusUpdateAsync(string jobId, JobStatus status, string? message = null);

    /// <summary>
    /// Sends a worker health update
    /// </summary>
    Task SendWorkerHealthUpdateAsync(bool isHealthy, string? message = null);
}

/// <summary>
/// Implementation of SignalR client service
/// </summary>
public class SignalRClientService : ISignalRClientService
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRClientService> _logger;

    public SignalRClientService(HubConnection hubConnection, ILogger<SignalRClientService> logger)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR connection started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubConnection.StopAsync(cancellationToken);
            _logger.LogInformation("SignalR connection stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop SignalR connection");
        }
    }

    public async Task SendJobStatusUpdateAsync(string jobId, JobStatus status, string? message = null)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("JobStatusUpdate", jobId, status.ToString(), message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job status update for job {JobId}", jobId);
        }
    }

    public async Task SendWorkerHealthUpdateAsync(bool isHealthy, string? message = null)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("WorkerHealthUpdate", isHealthy, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send worker health update");
        }
    }
}
<!--#endif-->
