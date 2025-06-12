using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Console application to encrypt and store the ProgressPlayDB connection string
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Encrypting ProgressPlayDB Connection String...");

        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // The connection string with the real password
            var connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDB;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly";
            
            // Store the encrypted connection string
            var success = await configService.SetEncryptedConfigValueAsync(
                "ProgressPlayDbConnectionString",
                connectionString,
                "Encrypted connection string for ProgressPlayDB database access"
            );

            if (success)
            {
                Console.WriteLine("✅ Successfully encrypted and stored ProgressPlayDB connection string");
                
                // Verify we can retrieve and decrypt it
                var retrievedValue = await configService.GetDecryptedConfigValueAsync("ProgressPlayDbConnectionString");
                if (retrievedValue == connectionString)
                {
                    Console.WriteLine("✅ Verification successful - connection string can be decrypted correctly");
                }
                else
                {
                    Console.WriteLine("❌ Verification failed - decrypted value doesn't match original");
                }
            }
            else
            {
                Console.WriteLine("❌ Failed to store encrypted connection string");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to encrypt and store connection string");
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

                // Add services
                services.AddScoped<IConfigurationService, ConfigurationService>();
                services.AddScoped<ISecurityService, SecurityService>();

                // Add configuration sections
                services.Configure<MonitoringGrid.Core.Security.SecurityConfiguration>(
                    context.Configuration.GetSection("Security"));
            });
}
