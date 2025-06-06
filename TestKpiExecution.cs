using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MonitoringGrid.Test;

/// <summary>
/// Test script to execute a KPI and show enhanced raw data capture
/// </summary>
public class TestKpiExecution
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== KPI Execution Test with Enhanced Raw Data Capture ===");
        Console.WriteLine();

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Setup services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Add monitoring configuration
        services.Configure<MonitoringConfiguration>(configuration.GetSection("Monitoring"));

        // Add Entity Framework
        services.AddDbContext<MonitoringContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("MonitoringGrid")));

        // Add KPI execution service
        services.AddScoped<IKpiExecutionService, KpiExecutionService>();

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
            var kpiService = scope.ServiceProvider.GetRequiredService<IKpiExecutionService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestKpiExecution>>();

            // Get a test KPI (Transaction Monitoring)
            var kpi = await context.KPIs
                .FirstOrDefaultAsync(k => k.SpName.Contains("stp_MonitorTransactions"));

            if (kpi == null)
            {
                Console.WriteLine("❌ No transaction monitoring KPI found!");
                return;
            }

            Console.WriteLine($"🎯 Executing KPI: {kpi.Indicator}");
            Console.WriteLine($"📋 Stored Procedure: {kpi.SpName}");
            Console.WriteLine($"⏱️ Frequency: {kpi.Frequency} minutes");
            Console.WriteLine();

            // Execute the KPI
            var result = await kpiService.ExecuteKpiAsync(kpi);

            Console.WriteLine("=== EXECUTION RESULTS ===");
            Console.WriteLine($"✅ Success: {result.IsSuccessful}");
            Console.WriteLine($"🔑 Key: {result.Key}");
            Console.WriteLine($"📊 Current Value: {result.CurrentValue:F2}");
            Console.WriteLine($"📈 Historical Value: {result.HistoricalValue:F2}");
            Console.WriteLine($"📉 Deviation: {result.DeviationPercent:F2}%");
            Console.WriteLine($"🚨 Should Alert: {result.ShouldAlert}");
            Console.WriteLine($"⏱️ Execution Time: {result.ExecutionTimeMs}ms");
            Console.WriteLine();

            if (!string.IsNullOrEmpty(result.ExecutionDetails))
            {
                Console.WriteLine("=== DETAILED EXECUTION INFORMATION ===");
                Console.WriteLine(result.ExecutionDetails);
                Console.WriteLine();
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine("=== ERROR INFORMATION ===");
                Console.WriteLine($"❌ Error: {result.ErrorMessage}");
                Console.WriteLine();
            }

            // Show metadata if available
            if (result.Metadata != null && result.Metadata.Count > 0)
            {
                Console.WriteLine("=== METADATA ===");
                foreach (var kvp in result.Metadata)
                {
                    Console.WriteLine($"🔍 {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine();
            }

            // Check if historical data was stored
            var latestHistoricalData = await context.HistoricalData
                .Where(h => h.KpiId == kpi.KpiId)
                .OrderByDescending(h => h.Timestamp)
                .FirstOrDefaultAsync();

            if (latestHistoricalData != null)
            {
                Console.WriteLine("=== STORED HISTORICAL DATA ===");
                Console.WriteLine($"📅 Timestamp: {latestHistoricalData.Timestamp}");
                Console.WriteLine($"👤 Executed By: {latestHistoricalData.ExecutedBy}");
                Console.WriteLine($"🔧 Execution Method: {latestHistoricalData.ExecutionMethod}");
                Console.WriteLine($"💾 Database: {latestHistoricalData.DatabaseName}");
                Console.WriteLine($"🖥️ Server: {latestHistoricalData.ServerName}");
                Console.WriteLine($"✅ Successful: {latestHistoricalData.IsSuccessful}");
                Console.WriteLine($"⏱️ Execution Time: {latestHistoricalData.ExecutionTimeMs}ms");
                
                if (!string.IsNullOrEmpty(latestHistoricalData.RawResponse))
                {
                    Console.WriteLine();
                    Console.WriteLine("=== RAW RESPONSE DATA ===");
                    Console.WriteLine(latestHistoricalData.RawResponse);
                }

                if (!string.IsNullOrEmpty(latestHistoricalData.ExecutionContext))
                {
                    Console.WriteLine();
                    Console.WriteLine("=== EXECUTION CONTEXT (JSON) ===");
                    Console.WriteLine(latestHistoricalData.ExecutionContext);
                }
            }

            Console.WriteLine();
            Console.WriteLine("✅ Test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
