using MonitoringGrid.Worker;
using MonitoringGrid.Worker.Services;
using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Infrastructure;
using Microsoft.Extensions.Http;
using MonitoringGrid.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using FluentValidation;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MonitoringContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120);
    }));

// Configuration sections
builder.Services.Configure<MonitoringGrid.Core.Models.EmailConfiguration>(
    builder.Configuration.GetSection("Email"));
builder.Services.Configure<MonitoringGrid.Core.Models.MonitoringConfiguration>(
    builder.Configuration.GetSection("Monitoring"));

// Worker Configuration - bind from MonitoringGrid section and Worker section
builder.Services.Configure<WorkerConfiguration>(config =>
{
    // Bind Worker section for worker-specific settings
    builder.Configuration.GetSection("Worker").Bind(config);

    // Override ApiBaseUrl from MonitoringGrid section
    var apiBaseUrl = builder.Configuration.GetValue<string>("MonitoringGrid:ApiBaseUrl");
    if (!string.IsNullOrEmpty(apiBaseUrl))
    {
        config.ApiBaseUrl = apiBaseUrl;
    }
});

// Add Infrastructure services (includes all core services, cache, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add HttpClient
builder.Services.AddHttpClient();

// Worker Services
Console.WriteLine("🔧 Registering hosted services...");
builder.Services.AddHostedService<IndicatorMonitoringWorker>();
Console.WriteLine("✅ IndicatorMonitoringWorker registered");
builder.Services.AddHostedService<ScheduledTaskWorker>();
Console.WriteLine("✅ ScheduledTaskWorker registered");
builder.Services.AddHostedService<HealthCheckWorker>();
Console.WriteLine("✅ HealthCheckWorker registered");

// Quartz Scheduling
builder.Services.AddQuartz(q =>
{
    // Configure Quartz to use dependency injection
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MonitoringContext>("database");

// Resilience Policies
builder.Services.AddResiliencePipeline("default", builder =>
{
    builder.AddRetry(new Polly.Retry.RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        Delay = TimeSpan.FromSeconds(2),
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential
    });
    builder.AddTimeout(TimeSpan.FromMinutes(5));
});

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddRuntimeInstrumentation()
               .AddMeter("MonitoringGrid.Worker");
    });

// Windows Service Support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MonitoringGrid Worker Service";
});

var host = builder.Build();

// Get logger for startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MonitoringGrid Worker starting up...");

try
{
    // Ensure database is created
    logger.LogInformation("Checking database connection...");
    using (var scope = host.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database connection verified successfully");

        // Test query to verify schema access
        var indicatorCount = await context.Indicators.CountAsync();
        logger.LogInformation("Found {IndicatorCount} indicators in database", indicatorCount);
    }

    logger.LogInformation("Starting MonitoringGrid Worker host...");
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Fatal error during worker startup");
    throw;
}
