using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Data;

/// <summary>
/// Console application to create and apply the Indicator migration
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Creating Indicator Migration...");

        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Check if database exists and is accessible
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("❌ Cannot connect to database. Please check connection string.");
                return;
            }

            Console.WriteLine("✅ Database connection successful");

            // Apply pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"📦 Applying {pendingMigrations.Count()} pending migrations...");
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Migrations applied successfully");
            }
            else
            {
                Console.WriteLine("✅ Database is up to date");
            }

            // Verify new tables exist
            var sql = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = 'monitoring' 
                AND TABLE_NAME IN ('Indicators', 'IndicatorContacts')";

            var tableNames = new List<string>();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            
            await context.Database.OpenConnectionAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }

            if (tableNames.Contains("Indicators") && tableNames.Contains("IndicatorContacts"))
            {
                Console.WriteLine("✅ Indicator tables created successfully");
            }
            else
            {
                Console.WriteLine("⚠️ Some Indicator tables may not have been created");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create/apply Indicator migration");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add Entity Framework
                var connectionString = context.Configuration.GetConnectionString("MonitoringGrid");
                services.AddDbContext<MonitoringContext>(options =>
                    options.UseSqlServer(connectionString));
            });
}
