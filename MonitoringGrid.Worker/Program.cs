using MonitoringGrid.Worker;
using MonitoringGrid.Worker.Services;
using MonitoringGrid.Worker.Configuration;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Services;
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
builder.Logging.AddEventLog();

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

// Core Services
builder.Services.AddScoped<IIndicatorService, MonitoringGrid.Infrastructure.Services.IndicatorService>();
builder.Services.AddScoped<IIndicatorExecutionService, MonitoringGrid.Infrastructure.Services.IndicatorExecutionService>();
builder.Services.AddScoped<IEmailService, MonitoringGrid.Infrastructure.Services.EmailService>();
builder.Services.AddScoped<ISmsService, MonitoringGrid.Infrastructure.Services.SmsService>();
builder.Services.AddScoped<IRepository<MonitoringGrid.Core.Entities.Indicator>, MonitoringGrid.Infrastructure.Repositories.Repository<MonitoringGrid.Core.Entities.Indicator>>();

// Worker Services
builder.Services.AddHostedService<IndicatorMonitoringWorker>();
builder.Services.AddHostedService<ScheduledTaskWorker>();
builder.Services.AddHostedService<HealthCheckWorker>();

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

// Ensure database is created
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    await context.Database.EnsureCreatedAsync();
}

await host.RunAsync();
