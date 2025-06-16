using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Services;

using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Worker.Configuration;

Console.WriteLine("=== Testing Worker Logic Directly ===");

try
{
    // Configuration
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("MonitoringGrid.Worker/appsettings.json", optional: false)
        .AddJsonFile("MonitoringGrid.Worker/appsettings.Development.json", optional: true)
        .Build();

    // Services
    var services = new ServiceCollection();
    
    // Logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Debug);
    });

    // Database
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"Using connection: {connectionString?.Substring(0, 50)}...");
    
    services.AddDbContext<MonitoringContext>(options =>
        options.UseSqlServer(connectionString));

    // Add required services
    services.AddScoped<ICacheService, CacheService>();
    services.AddScoped<IIndicatorService, IndicatorService>();
    services.AddScoped<IIndicatorExecutionService, IndicatorExecutionService>();
    services.AddMemoryCache();

    // Worker configuration
    services.Configure<WorkerConfiguration>(configuration.GetSection("MonitoringGrid:Worker"));

    var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting worker logic test...");

    using var scope = serviceProvider.CreateScope();
    var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
    var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();
    var workerConfig = scope.ServiceProvider.GetRequiredService<IOptions<WorkerConfiguration>>();

    // Test database connection
    var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    var canConnect = await context.Database.CanConnectAsync();
    Console.WriteLine($"Database connection: {canConnect}");

    if (!canConnect)
    {
        Console.WriteLine("Cannot connect to database. Exiting.");
        return;
    }

    // Test the exact logic from IndicatorMonitoringWorker
    logger.LogInformation("=== Testing GetDueIndicatorsAsync ===");
    
    // Get all indicators first
    var allIndicators = await indicatorService.GetAllIndicatorsAsync();
    Console.WriteLine($"Total indicators: {allIndicators.Count}");

    // Get active indicators
    var activeIndicators = await indicatorService.GetActiveIndicatorsAsync();
    Console.WriteLine($"Active indicators: {activeIndicators.Count}");

    // Get due indicators (this is where the issue might be)
    var dueIndicators = await indicatorService.GetDueIndicatorsAsync();
    Console.WriteLine($"Due indicators: {dueIndicators.Count}");

    if (dueIndicators.Any())
    {
        Console.WriteLine("Due indicators found:");
        foreach (var indicator in dueIndicators)
        {
            Console.WriteLine($"- {indicator.IndicatorName} (ID: {indicator.IndicatorID})");
        }
    }
    else
    {
        Console.WriteLine("No due indicators found - this is the issue!");
        
        // Debug why no indicators are due
        Console.WriteLine("\nDebugging indicator due logic...");
        foreach (var indicator in activeIndicators)
        {
            Console.WriteLine($"\nIndicator: {indicator.IndicatorName} (ID: {indicator.IndicatorID})");
            Console.WriteLine($"  IsActive: {indicator.IsActive}");
            Console.WriteLine($"  LastRun: {indicator.LastRun}");
            Console.WriteLine($"  SchedulerID: {indicator.SchedulerID}");
            Console.WriteLine($"  Scheduler loaded: {indicator.Scheduler != null}");
            
            if (indicator.Scheduler != null)
            {
                Console.WriteLine($"  Scheduler.IsEnabled: {indicator.Scheduler.IsEnabled}");
                Console.WriteLine($"  Scheduler.ScheduleType: {indicator.Scheduler.ScheduleType}");
                Console.WriteLine($"  Scheduler.IntervalMinutes: {indicator.Scheduler.IntervalMinutes}");
                
                var isCurrentlyActive = indicator.Scheduler.IsCurrentlyActive();
                Console.WriteLine($"  Scheduler.IsCurrentlyActive(): {isCurrentlyActive}");
                
                if (indicator.LastRun.HasValue)
                {
                    var nextExecution = indicator.Scheduler.GetNextExecutionTime(indicator.LastRun);
                    Console.WriteLine($"  NextExecutionTime: {nextExecution}");
                    Console.WriteLine($"  Current time: {DateTime.UtcNow}");
                    Console.WriteLine($"  Should be due: {nextExecution.HasValue && DateTime.UtcNow >= nextExecution.Value}");
                }
                else
                {
                    Console.WriteLine($"  Never run before - should be due immediately");
                }
            }
            else
            {
                Console.WriteLine($"  No scheduler - cannot be due");
            }
            
            var isDue = indicator.IsDue();
            Console.WriteLine($"  IsDue() result: {isDue}");
        }
    }

    Console.WriteLine("\n=== Test Complete ===");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("Press any key to exit.");
Console.ReadKey();
