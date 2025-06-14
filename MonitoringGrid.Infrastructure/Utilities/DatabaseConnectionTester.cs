using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Utilities;

/// <summary>
/// Utility class for testing database connections
/// Consolidates functionality from DatabaseConnectionTest and TestDbConnection projects
/// </summary>
public class DatabaseConnectionTester
{
    private readonly ILogger<DatabaseConnectionTester> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseConnectionTester(ILogger<DatabaseConnectionTester> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Tests all database connections used by the MonitoringGrid system
    /// </summary>
    public async Task<bool> TestAllConnectionsAsync()
    {
        Console.WriteLine("=== MonitoringGrid Database Connection Test ===");
        Console.WriteLine();

        bool allTestsPassed = true;

        try
        {
            // Test PopAI database (main monitoring database)
            allTestsPassed &= await TestPopAIDatabaseAsync();
            Console.WriteLine();

            // Test ProgressPlayDB database (monitored database)
            allTestsPassed &= await TestProgressPlayDBAsync();
            Console.WriteLine();

            // Test Entity Framework context
            allTestsPassed &= await TestEntityFrameworkContextAsync();
            Console.WriteLine();

            Console.WriteLine($"=== Overall Result: {(allTestsPassed ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED")} ===");
            return allTestsPassed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection testing failed");
            Console.WriteLine($"❌ Critical Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tests connection to PopAI database (main monitoring database)
    /// </summary>
    public async Task<bool> TestPopAIDatabaseAsync()
    {
        Console.WriteLine("📊 Testing PopAI Database Connection...");

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ??
                                 "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"✅ PopAI Database connection successful");
            Console.WriteLine($"   Server: {connection.DataSource}");
            Console.WriteLine($"   Database: {connection.Database}");
            Console.WriteLine($"   State: {connection.State}");

            // Test key tables
            await TestPopAITablesAsync(connection);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PopAI database connection failed");
            Console.WriteLine($"❌ PopAI Database connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tests connection to ProgressPlayDB database (monitored database)
    /// </summary>
    public async Task<bool> TestProgressPlayDBAsync()
    {
        Console.WriteLine("📊 Testing ProgressPlayDB Connection...");

        try
        {
            var connectionString = _configuration.GetConnectionString("SourceDatabase") ??
                                 _configuration.GetConnectionString("ProgressPlayDB") ??
                                 "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDBTest;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"✅ SourceDatabase (ProgressPlayDBTest) connection successful");
            Console.WriteLine($"   Server: {connection.DataSource}");
            Console.WriteLine($"   Database: {connection.Database}");
            Console.WriteLine($"   State: {connection.State}");

            // Test collectors table
            await TestProgressPlayDBTablesAsync(connection);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SourceDatabase connection failed");
            Console.WriteLine($"❌ SourceDatabase connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tests Entity Framework context
    /// </summary>
    public async Task<bool> TestEntityFrameworkContextAsync()
    {
        Console.WriteLine("📊 Testing Entity Framework Context...");

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var options = new DbContextOptionsBuilder<MonitoringContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var context = new MonitoringContext(options);
            
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("❌ Entity Framework context cannot connect");
                return false;
            }

            Console.WriteLine("✅ Entity Framework context connection successful");

            // Test basic queries
            var indicatorCount = await context.Indicators.CountAsync();
            var contactCount = await context.Contacts.CountAsync();
            var userCount = await context.Users.CountAsync();

            Console.WriteLine($"   Indicators: {indicatorCount}");
            Console.WriteLine($"   Contacts: {contactCount}");
            Console.WriteLine($"   Users: {userCount}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity Framework context test failed");
            Console.WriteLine($"❌ Entity Framework context test failed: {ex.Message}");
            return false;
        }
    }

    private async Task TestPopAITablesAsync(SqlConnection connection)
    {
        try
        {
            Console.WriteLine("  🔍 Checking PopAI tables...");

            var tables = new[]
            {
                ("monitoring.Indicators", "SELECT COUNT(*) FROM monitoring.Indicators"),
                ("monitoring.Contacts", "SELECT COUNT(*) FROM monitoring.Contacts"),
                ("auth.Users", "SELECT COUNT(*) FROM auth.Users"),
                ("auth.Roles", "SELECT COUNT(*) FROM auth.Roles")
            };

            foreach (var (tableName, query) in tables)
            {
                try
                {
                    using var command = new SqlCommand(query, connection);
                    var count = await command.ExecuteScalarAsync();
                    Console.WriteLine($"     ✅ {tableName}: {count} records");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"     ❌ {tableName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error checking PopAI tables: {ex.Message}");
        }
    }

    private async Task TestProgressPlayDBTablesAsync(SqlConnection connection)
    {
        try
        {
            Console.WriteLine("  🔍 Checking ProgressPlayDBTest tables...");

            const string collectorsQuery = @"
                SELECT COUNT(*) FROM [stats].[tbl_Monitor_StatisticsCollectors]
                WHERE IsActive = 1";

            using var command = new SqlCommand(collectorsQuery, connection);
            var count = await command.ExecuteScalarAsync();
            Console.WriteLine($"     ✅ Active Collectors: {count}");

            // Test sample collector data
            const string sampleQuery = @"
                SELECT TOP 3 CollectorID, CollectorCode, CollectorDesc
                FROM [stats].[tbl_Monitor_StatisticsCollectors]
                WHERE IsActive = 1
                ORDER BY CollectorCode";

            using var sampleCommand = new SqlCommand(sampleQuery, connection);
            using var reader = await sampleCommand.ExecuteReaderAsync();

            Console.WriteLine("     Sample collectors:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"       - {reader["CollectorCode"]}: {reader["CollectorDesc"]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error checking ProgressPlayDBTest tables: {ex.Message}");
        }
    }

    /// <summary>
    /// Console application entry point for database connection testing
    /// Replaces the standalone DatabaseConnectionTest and TestDbConnection projects
    /// </summary>
    public static async Task RunConnectionTestTool(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DatabaseConnectionTester>();

        var tester = new DatabaseConnectionTester(logger, configuration);
        var success = await tester.TestAllConnectionsAsync();

        Environment.Exit(success ? 0 : 1);
    }
}
