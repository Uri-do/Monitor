using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure;
using MonitoringGrid.StandaloneWorker.Services;
using MonitoringGrid.StandaloneWorker.Models;
using System.Text.Json;

namespace MonitoringGrid.StandaloneWorker;

/// <summary>
/// Standalone Worker Process for Integration Testing
/// This is a separate executable that can be spawned, monitored, and terminated by the API
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Create command line interface
        var rootCommand = new RootCommand("MonitoringGrid Standalone Worker Process");

        // Worker ID option
        var workerIdOption = new Option<string>(
            name: "--worker-id",
            description: "Unique identifier for this worker process")
        {
            IsRequired = true
        };

        // Test mode option
        var testModeOption = new Option<bool>(
            name: "--test-mode",
            description: "Run in test mode with enhanced logging and status reporting")
        {
            IsRequired = false
        };

        // Duration option (for test scenarios)
        var durationOption = new Option<int>(
            name: "--duration",
            description: "Duration to run in seconds (0 = run indefinitely)")
        {
            IsRequired = false
        };

        // Indicator IDs option
        var indicatorIdsOption = new Option<string>(
            name: "--indicator-ids",
            description: "Comma-separated list of indicator IDs to process (empty = all)")
        {
            IsRequired = false
        };

        // Status file option
        var statusFileOption = new Option<string>(
            name: "--status-file",
            description: "Path to status file for IPC communication")
        {
            IsRequired = false
        };

        // API URL option
        var apiUrlOption = new Option<string>(
            name: "--api-url",
            description: "Base URL of the API for SignalR communication")
        {
            IsRequired = false
        };

        rootCommand.AddOption(workerIdOption);
        rootCommand.AddOption(testModeOption);
        rootCommand.AddOption(durationOption);
        rootCommand.AddOption(indicatorIdsOption);
        rootCommand.AddOption(statusFileOption);
        rootCommand.AddOption(apiUrlOption);

        rootCommand.SetHandler(async (workerId, testMode, duration, indicatorIds, statusFile, apiUrl) =>
        {
            await RunWorkerAsync(workerId, testMode, duration, indicatorIds, statusFile, apiUrl);
        }, workerIdOption, testModeOption, durationOption, indicatorIdsOption, statusFileOption, apiUrlOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunWorkerAsync(string workerId, bool testMode, int duration, string? indicatorIds, string? statusFile, string? apiUrl)
    {
        Console.WriteLine($"🚀 Starting MonitoringGrid Standalone Worker");
        Console.WriteLine($"   Worker ID: {workerId}");
        Console.WriteLine($"   Test Mode: {testMode}");
        Console.WriteLine($"   Duration: {(duration > 0 ? $"{duration} seconds" : "Indefinite")}");
        Console.WriteLine($"   Indicator IDs: {indicatorIds ?? "All"}");
        Console.WriteLine($"   Status File: {statusFile ?? "None"}");
        Console.WriteLine($"   API URL: {apiUrl ?? "Default"}");
        Console.WriteLine();

        // Parse indicator IDs
        var indicatorIdList = new List<int>();
        if (!string.IsNullOrEmpty(indicatorIds))
        {
            foreach (var id in indicatorIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(id.Trim(), out var indicatorId))
                {
                    indicatorIdList.Add(indicatorId);
                }
            }
        }

        // Create worker configuration
        var config = new StandaloneWorkerConfig
        {
            WorkerId = workerId,
            TestMode = testMode,
            DurationSeconds = duration,
            IndicatorIds = indicatorIdList,
            StatusFilePath = statusFile,
            ApiBaseUrl = apiUrl ?? "http://localhost:57653"
        };

        // Build host
        var builder = Host.CreateApplicationBuilder();

        // Configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        if (testMode)
        {
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
        }

        // Database
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=Progr3ssP1@y!;TrustServerCertificate=true;";

        builder.Services.AddDbContext<MonitoringContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(120);
            }));

        // Add Infrastructure services
        builder.Services.AddInfrastructure(builder.Configuration);

        // Register worker configuration
        builder.Services.AddSingleton(config);

        // Register worker services
        builder.Services.AddHostedService<StandaloneWorkerService>();
        builder.Services.AddSingleton<IWorkerStatusReporter, WorkerStatusReporter>();

        var host = builder.Build();

        try
        {
            // Write initial status
            await WriteStatusAsync(config, new WorkerStatus
            {
                WorkerId = workerId,
                State = WorkerState.Starting,
                StartTime = DateTime.UtcNow,
                Message = "Worker process starting"
            });

            Console.WriteLine($"✅ Worker {workerId} configured and ready to start");
            
            // Run the worker
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error in worker {workerId}: {ex.Message}");
            
            // Write error status
            await WriteStatusAsync(config, new WorkerStatus
            {
                WorkerId = workerId,
                State = WorkerState.Failed,
                StartTime = DateTime.UtcNow,
                Message = $"Fatal error: {ex.Message}",
                ErrorDetails = ex.ToString()
            });
            
            throw;
        }
    }

    static async Task WriteStatusAsync(StandaloneWorkerConfig config, WorkerStatus status)
    {
        if (string.IsNullOrEmpty(config.StatusFilePath))
            return;

        try
        {
            var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(config.StatusFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to write status file: {ex.Message}");
        }
    }
}
