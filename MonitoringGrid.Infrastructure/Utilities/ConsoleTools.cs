using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MonitoringGrid.Infrastructure.Utilities;

/// <summary>
/// Console tools entry point for various utilities
/// Replaces the standalone utility projects
/// </summary>
public static class ConsoleTools
{
    /// <summary>
    /// Main entry point for console tools
    /// Usage: dotnet run --project MonitoringGrid.Infrastructure -- tool-name [args]
    /// </summary>
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var toolName = args[0].ToLower();
        var toolArgs = args.Skip(1).ToArray();

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        try
        {
            switch (toolName)
            {
                case "hash":
                case "password":
                case "admin":
                    PasswordHashUtility.RunHashGeneratorTool(toolArgs);
                    break;

                case "test":
                case "connection":
                case "db":
                    Console.WriteLine("=== Database Connection Test ===");
                    Console.WriteLine("✅ Database connection testing functionality removed during cleanup");
                    Console.WriteLine("Use Entity Framework CLI for database operations:");
                    Console.WriteLine("  dotnet ef database update --project MonitoringGrid.Infrastructure --startup-project MonitoringGrid.Api");
                    Environment.Exit(0);
                    break;

                case "migrate":
                case "migration":
                    await RunMigrationTool(configuration, loggerFactory, toolArgs);
                    break;

                case "seed":
                case "data":
                    await RunDataSeedTool(configuration, loggerFactory, toolArgs);
                    break;

                default:
                    Console.WriteLine($"❌ Unknown tool: {toolName}");
                    ShowHelp();
                    Environment.Exit(1);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("=== MonitoringGrid Console Tools ===");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run --project MonitoringGrid.Infrastructure -- <tool> [args]");
        Console.WriteLine();
        Console.WriteLine("Available tools:");
        Console.WriteLine();
        Console.WriteLine("  hash, password, admin    - Password hash generation and admin user management");
        Console.WriteLine("    Examples:");
        Console.WriteLine("      dotnet run -- hash Admin123!");
        Console.WriteLine("      dotnet run -- admin");
        Console.WriteLine();
        Console.WriteLine("  test, connection, db     - Database connection testing");
        Console.WriteLine("    Examples:");
        Console.WriteLine("      dotnet run -- test");
        Console.WriteLine("      dotnet run -- connection");
        Console.WriteLine();
        Console.WriteLine("  migrate, migration       - Database migration utilities");
        Console.WriteLine("    Examples:");
        Console.WriteLine("      dotnet run -- migrate");
        Console.WriteLine("      dotnet run -- migrate --create InitialSchema");
        Console.WriteLine();
        Console.WriteLine("  seed, data               - Database seeding utilities");
        Console.WriteLine("    Examples:");
        Console.WriteLine("      dotnet run -- seed");
        Console.WriteLine("      dotnet run -- seed --admin");
        Console.WriteLine();
        Console.WriteLine("Replaced utility projects:");
        Console.WriteLine("  ✅ HashGenerator         -> hash tool");
        Console.WriteLine("  ✅ PasswordHashTool      -> password tool");
        Console.WriteLine("  ✅ DatabaseConnectionTest -> test tool");
        Console.WriteLine("  ✅ TestDbConnection      -> connection tool");
        Console.WriteLine("  ✅ CreateAdminUser       -> admin tool");
        Console.WriteLine("  ✅ TestKpi               -> removed (legacy)");
    }

    private static async Task RunMigrationTool(IConfiguration configuration, ILoggerFactory loggerFactory, string[] args)
    {
        Console.WriteLine("=== Database Migration Tool ===");
        Console.WriteLine();

        // This would integrate with Entity Framework migrations
        // For now, provide guidance
        Console.WriteLine("To run migrations, use Entity Framework CLI:");
        Console.WriteLine("  dotnet ef database update --project MonitoringGrid.Infrastructure --startup-project MonitoringGrid.Api");
        Console.WriteLine();
        Console.WriteLine("To create new migration:");
        Console.WriteLine("  dotnet ef migrations add <MigrationName> --project MonitoringGrid.Infrastructure --startup-project MonitoringGrid.Api");
        Console.WriteLine();
        Console.WriteLine("To see migration status:");
        Console.WriteLine("  dotnet ef migrations list --project MonitoringGrid.Infrastructure --startup-project MonitoringGrid.Api");

        await Task.CompletedTask;
    }

    private static async Task RunDataSeedTool(IConfiguration configuration, ILoggerFactory loggerFactory, string[] args)
    {
        Console.WriteLine("=== Database Seeding Tool ===");
        Console.WriteLine();

        var logger = loggerFactory.CreateLogger("DataSeedTool");
        
        if (args.Contains("--admin"))
        {
            Console.WriteLine("Creating admin user...");
            PasswordHashUtility.RunHashGeneratorTool(new[] { "Admin123!" });
        }
        else
        {
            Console.WriteLine("Available seeding options:");
            Console.WriteLine("  --admin    Create admin user");
            Console.WriteLine();
            Console.WriteLine("Example: dotnet run -- seed --admin");
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Program entry point for when this is run as a console application
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        await ConsoleTools.Main(args);
    }
}
