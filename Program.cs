using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MonitoringGrid;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Infrastructure.Services;
using Serilog;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

// Add configuration sections
builder.Services.Configure<MonitoringConfiguration>(
    builder.Configuration.GetSection("Monitoring"));
builder.Services.Configure<EmailConfiguration>(
    builder.Configuration.GetSection("Email"));

// Validate configuration
var monitoringConfig = builder.Configuration.GetSection(MonitoringConfig.SectionName).Get<MonitoringConfig>();
var emailConfig = builder.Configuration.GetSection(EmailConfig.SectionName).Get<EmailConfig>();

if (monitoringConfig != null && !monitoringConfig.IsValid(out var monitoringErrors))
{
    Log.Fatal("Invalid monitoring configuration: {Errors}", string.Join(", ", monitoringErrors));
    return 1;
}

if (emailConfig != null && !emailConfig.IsValid(out var emailErrors))
{
    Log.Fatal("Invalid email configuration: {Errors}", string.Join(", ", emailErrors));
    return 1;
}

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("MonitoringGrid");
if (string.IsNullOrEmpty(connectionString))
{
    Log.Fatal("MonitoringGrid connection string is required");
    return 1;
}

builder.Services.AddDbContext<MonitoringContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(monitoringConfig?.DatabaseTimeoutSeconds ?? 30);
    });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add domain services
builder.Services.AddScoped<KpiDomainService>();

// Add repository services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Add domain services
builder.Services.AddScoped<KpiDomainService>();

// Add repository services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Add application services
builder.Services.AddScoped<IKpiExecutionService, KpiExecutionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Add the main worker service
builder.Services.AddHostedService<MonitoringWorker>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MonitoringContext>("database")
    .AddCheck<MonitoringHealthCheck>("monitoring-service");

// Configure Windows Service support
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "MonitoringGrid";
    });
}

// Build the host
var host = builder.Build();

// Ensure database is created and up to date
try
{
    using var scope = host.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    
    Log.Information("Checking database connection...");
    await context.Database.CanConnectAsync();
    Log.Information("Database connection successful");
    
    // Optionally create database if it doesn't exist (for development)
    if (builder.Environment.IsDevelopment())
    {
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database schema verified");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to connect to database: {Message}", ex.Message);
    return 1;
}

// Log startup information
Log.Information("Monitoring Grid Service starting...");
Log.Information("Configuration: MaxParallel={MaxParallel}, Interval={Interval}s, SMS={SmsEnabled}, Email={EmailEnabled}",
    monitoringConfig?.MaxParallelExecutions,
    monitoringConfig?.ServiceIntervalSeconds,
    monitoringConfig?.EnableSms,
    monitoringConfig?.EnableEmail);

try
{
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly: {Message}", ex.Message);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Health check for the monitoring service
/// </summary>
public class MonitoringHealthCheck : IHealthCheck
{
    private readonly MonitoringContext _context;
    private readonly ILogger<MonitoringHealthCheck> _logger;

    public MonitoringHealthCheck(MonitoringContext context, ILogger<MonitoringHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync(cancellationToken);

            // Check if service is running (last heartbeat within 5 minutes)
            var status = await _context.SystemStatus
                .FirstOrDefaultAsync(s => s.ServiceName == "MonitoringWorker", cancellationToken);

            if (status == null)
            {
                return HealthCheckResult.Degraded("Service status not found");
            }

            var timeSinceHeartbeat = DateTime.UtcNow - status.LastHeartbeat;
            if (timeSinceHeartbeat > TimeSpan.FromMinutes(5))
            {
                return HealthCheckResult.Unhealthy($"Service heartbeat is {timeSinceHeartbeat.TotalMinutes:F1} minutes old");
            }

            if (status.Status != "Running")
            {
                return HealthCheckResult.Unhealthy($"Service status: {status.Status}. Error: {status.ErrorMessage}");
            }

            // Check for recent activity
            var activeKpis = await _context.KPIs.CountAsync(k => k.IsActive, cancellationToken);
            var recentAlerts = await _context.AlertLogs
                .CountAsync(a => a.TriggerTime > DateTime.UtcNow.AddHours(-24), cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["LastHeartbeat"] = status.LastHeartbeat,
                ["ActiveKpis"] = activeKpis,
                ["ProcessedKpisToday"] = status.ProcessedKpis,
                ["AlertsSentToday"] = status.AlertsSent,
                ["RecentAlerts24h"] = recentAlerts
            };

            return HealthCheckResult.Healthy("Monitoring service is running normally", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}");
        }
    }
}
