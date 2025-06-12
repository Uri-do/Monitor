using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Data;
using Microsoft.Data.SqlClient;

/// <summary>
/// Test database connections and verify tables exist
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Testing Database Connections and Tables...\n");

        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Test PopAI database connection
            Console.WriteLine("📊 Testing PopAI Database Connection...");
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                Console.WriteLine("✅ PopAI Database connection successful");
                
                // Check existing tables
                await CheckPopAITablesAsync(context);
            }
            else
            {
                Console.WriteLine("❌ Cannot connect to PopAI database");
                return;
            }

            Console.WriteLine();

            // Test ProgressPlayDB connection directly
            Console.WriteLine("📊 Testing ProgressPlayDB Connection...");
            await TestProgressPlayDBDirectAsync();

            Console.WriteLine();

            // Test data retrieval
            await TestDataRetrievalAsync(context);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection test failed");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task CheckPopAITablesAsync(MonitoringContext context)
    {
        try
        {
            Console.WriteLine("  🔍 Checking PopAI tables...");

            // Check existing KPI tables
            var kpiCount = await context.KPIs.CountAsync();
            Console.WriteLine($"  📋 KPIs table: {kpiCount} records");

            var contactCount = await context.Contacts.CountAsync();
            Console.WriteLine($"  👥 Contacts table: {contactCount} records");

            var configCount = await context.Config.CountAsync();
            Console.WriteLine($"  ⚙️ Config table: {configCount} records");

            // Check if Indicator tables exist
            try
            {
                var indicatorCount = await context.Indicators.CountAsync();
                Console.WriteLine($"  🎯 Indicators table: {indicatorCount} records");
            }
            catch (Exception)
            {
                Console.WriteLine("  ⚠️ Indicators table does not exist yet");
            }

            try
            {
                var indicatorContactCount = await context.IndicatorContacts.CountAsync();
                Console.WriteLine($"  🔗 IndicatorContacts table: {indicatorContactCount} records");
            }
            catch (Exception)
            {
                Console.WriteLine("  ⚠️ IndicatorContacts table does not exist yet");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error checking PopAI tables: {ex.Message}");
        }
    }

    static async Task TestProgressPlayDBDirectAsync()
    {
        try
        {
            Console.WriteLine("  🔍 Testing ProgressPlayDB connection directly...");

            var connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDB;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly";

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("✅ ProgressPlayDB connection successful");

            // Test collectors query
            const string collectorsQuery = @"
                SELECT TOP 5 CollectorID, CollectorCode, CollectorDesc, IsActive
                FROM [stats].[tbl_Monitor_StatisticsCollectors]
                WHERE IsActive = 1
                ORDER BY CollectorCode";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(collectorsQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            var collectorCount = 0;
            while (await reader.ReadAsync())
            {
                collectorCount++;
                if (collectorCount == 1)
                {
                    Console.WriteLine($"  📋 Sample collector: {reader["CollectorCode"]} - {reader["CollectorDesc"]}");
                }
            }
            Console.WriteLine($"  📊 Found {collectorCount} active collectors");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error testing ProgressPlayDB: {ex.Message}");
        }
    }

    static async Task TestDataRetrievalAsync(MonitoringContext context)
    {
        try
        {
            Console.WriteLine("🧪 Testing Data Retrieval...");

            // Test getting a sample contact for indicator creation
            var contacts = await context.Contacts.Take(1).ToListAsync();
            if (contacts.Any())
            {
                Console.WriteLine($"✅ Sample contact available: {contacts.First().Name}");
            }
            else
            {
                Console.WriteLine("⚠️ No contacts found - will need to create one for indicators");
            }

            // Test getting existing KPIs
            var kpis = await context.KPIs.Take(3).ToListAsync();
            Console.WriteLine($"📊 Found {kpis.Count} existing KPIs");

            // Test if Indicator tables exist
            try
            {
                var indicatorCount = await context.Indicators.CountAsync();
                Console.WriteLine($"🎯 Indicators table exists with {indicatorCount} records");
            }
            catch (Exception)
            {
                Console.WriteLine("⚠️ Indicators table does not exist yet - migration needed");
            }

            Console.WriteLine("✅ All data retrieval tests completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Data retrieval test failed: {ex.Message}");
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add Entity Framework
                var connectionString = context.Configuration.GetConnectionString("MonitoringGrid");
                services.AddDbContext<MonitoringContext>(options =>
                    options.UseSqlServer(connectionString));

                // Add minimal services for testing
                // services.AddScoped<IProgressPlayDbService, ProgressPlayDbService>();
                // services.AddScoped<IConfigurationService, ConfigurationService>();
                // services.AddScoped<ISecurityService, SecurityService>();

                // Add configuration sections
                // services.Configure<MonitoringGrid.Core.Security.SecurityConfiguration>(
                //     context.Configuration.GetSection("Security"));
            });
}
