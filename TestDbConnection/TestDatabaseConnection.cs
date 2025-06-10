using Microsoft.Data.SqlClient;
using System.Data;

namespace TestDbConnection;

class Program
{
    // Connection string for PopAI database (same as API uses)
    private static readonly string ConnectionString = 
        "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Database Connection to PopAI Database");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        try
        {
            await TestDatabaseConnection();
            await TestKpiTableAccess();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task TestDatabaseConnection()
    {
        Console.WriteLine("1. Testing database connection...");
        
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        Console.WriteLine($"✅ Connected to database: {connection.Database}");
        Console.WriteLine($"   Server: {connection.DataSource}");
        Console.WriteLine($"   State: {connection.State}");
        Console.WriteLine();
    }

    static async Task TestKpiTableAccess()
    {
        Console.WriteLine("2. Testing KPI table access...");
        
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Test if monitoring schema exists
        var schemaQuery = "SELECT COUNT(*) FROM sys.schemas WHERE name = 'monitoring'";
        using var schemaCmd = new SqlCommand(schemaQuery, connection);
        var schemaExists = (int)await schemaCmd.ExecuteScalarAsync() > 0;
        
        Console.WriteLine($"   Monitoring schema exists: {schemaExists}");

        if (schemaExists)
        {
            // Test if KPIs table exists
            var tableQuery = "SELECT COUNT(*) FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'monitoring' AND t.name = 'KPIs'";
            using var tableCmd = new SqlCommand(tableQuery, connection);
            var tableExists = (int)await tableCmd.ExecuteScalarAsync() > 0;
            
            Console.WriteLine($"   KPIs table exists: {tableExists}");

            if (tableExists)
            {
                // Count KPIs
                var countQuery = "SELECT COUNT(*) FROM monitoring.KPIs";
                using var countCmd = new SqlCommand(countQuery, connection);
                var kpiCount = (int)await countCmd.ExecuteScalarAsync();
                
                Console.WriteLine($"   Total KPIs in table: {kpiCount}");

                if (kpiCount > 0)
                {
                    // Get sample KPI data
                    var sampleQuery = @"
                        SELECT TOP 5 
                            KpiId, 
                            Indicator, 
                            Owner, 
                            Priority, 
                            Frequency, 
                            IsActive,
                            LastRun
                        FROM monitoring.KPIs 
                        ORDER BY KpiId";
                    
                    using var sampleCmd = new SqlCommand(sampleQuery, connection);
                    using var reader = await sampleCmd.ExecuteReaderAsync();
                    
                    Console.WriteLine("\n   Sample KPI Data:");
                    Console.WriteLine("   ================");
                    
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"   ID: {reader["KpiId"]}, " +
                                        $"Indicator: {reader["Indicator"]}, " +
                                        $"Owner: {reader["Owner"]}, " +
                                        $"Active: {reader["IsActive"]}, " +
                                        $"Frequency: {reader["Frequency"]} min");
                    }
                }
            }
        }
        
        Console.WriteLine();
    }
}
