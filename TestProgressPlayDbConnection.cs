using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace TestProgressPlayDbConnection
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Testing ProgressPlayDB Connection...");
            Console.WriteLine("=====================================");

            // Connection string from appsettings.json
            var connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDB;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly";

            try
            {
                // Test basic connection
                Console.WriteLine("1. Testing basic connection...");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("✅ Connection successful!");
                    Console.WriteLine($"   Database: {connection.Database}");
                    Console.WriteLine($"   Server: {connection.DataSource}");
                }

                // Test stats schema access
                Console.WriteLine("\n2. Testing stats schema access...");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT COUNT(*) as TableCount
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = 'stats'";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        var tableCount = await command.ExecuteScalarAsync();
                        Console.WriteLine($"✅ Found {tableCount} tables in 'stats' schema");
                    }
                }

                // Test collectors table
                Console.WriteLine("\n3. Testing tbl_Monitor_StatisticsCollectors table...");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP 5 CollectorID, CollectorCode, CollectorDesc, IsActive
                        FROM [stats].[tbl_Monitor_StatisticsCollectors]
                        ORDER BY CollectorCode";
                    
                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Console.WriteLine("✅ Sample collectors:");
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine($"   ID: {reader["CollectorID"]}, Code: {reader["CollectorCode"]}, Active: {reader["IsActive"]}");
                        }
                    }
                }

                // Test statistics table
                Console.WriteLine("\n4. Testing tbl_Monitor_Statistics table...");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP 5 CollectorID, ItemName, Total, Marked, Day, Hour
                        FROM [stats].[tbl_Monitor_Statistics]
                        ORDER BY Day DESC, Hour DESC";
                    
                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Console.WriteLine("✅ Sample statistics:");
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine($"   Collector: {reader["CollectorID"]}, Item: {reader["ItemName"]}, Total: {reader["Total"]}, Day: {reader["Day"]}");
                        }
                    }
                }

                // Test stored procedures
                Console.WriteLine("\n5. Testing stored procedures...");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT name 
                        FROM sys.procedures 
                        WHERE schema_id = SCHEMA_ID('stats')
                        AND name LIKE '%Monitor%'
                        ORDER BY name";
                    
                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Console.WriteLine("✅ Available monitoring stored procedures:");
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine($"   - [stats].[{reader["name"]}]");
                        }
                    }
                }

                Console.WriteLine("\n🎉 All tests passed! ProgressPlayDB connection is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
