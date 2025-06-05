using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid;

/// <summary>
/// Console application to seed the database with initial data
/// </summary>
public class SeedDatabase
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<SeedDatabase>>();
        
        try
        {
            logger.LogInformation("Starting database seeding...");
            
            var context = services.GetRequiredService<MonitoringContext>();
            await SeedData.SeedAsync(context);
            
            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add Entity Framework
                services.AddDbContext<MonitoringContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("MonitoringGrid")));
            });
}
