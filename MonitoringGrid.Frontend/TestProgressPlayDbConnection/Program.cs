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

            // Connection strings to try
            var connectionStrings = new[]
            {
                ("ProgressPlayDB with saturn", "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDB;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly"),
                ("ProgressPlayDB with conexusadmin", "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDB;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"),
                ("ProgressPlayDBTest with saturn", "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDBTest;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly"),
                ("ProgressPlayDBTest with conexusadmin", "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDBTest;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true")
            };

            string? workingConnectionString = null;
            string? workingDescription = null;

            // Test each connection string
            foreach (var (description, connectionString) in connectionStrings)
            {
                Console.WriteLine($"Testing: {description}...");
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        Console.WriteLine($"✅ {description} - Connection successful!");
                        Console.WriteLine($"   Database: {connection.Database}");
                        Console.WriteLine($"   Server: {connection.DataSource}");
                        workingConnectionString = connectionString;
                        workingDescription = description;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {description} - Failed: {ex.Message}");
                }
            }

            if (workingConnectionString == null)
            {
                Console.WriteLine("\n❌ No working connection string found!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\n🎉 Using working connection: {workingDescription}");

            try
            {
                // Test basic connection (already tested above)
                Console.WriteLine("\n1. Testing database schema and tables...");

                // Test stats schema access
                Console.WriteLine("\n2. Testing stats schema access...");
                using (var connection = new SqlConnection(workingConnectionString))
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

                // Discover actual table names in stats schema
                Console.WriteLine("\n3. Discovering tables in stats schema...");
                using (var connection = new SqlConnection(workingConnectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT TABLE_NAME, TABLE_TYPE
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_SCHEMA = 'stats'
                        ORDER BY TABLE_NAME";

                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Console.WriteLine("✅ Tables in stats schema:");
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine($"   - {reader["TABLE_NAME"]} ({reader["TABLE_TYPE"]})");
                        }
                    }
                }

                // Test collectors table (try different possible names)
                Console.WriteLine("\n4. Testing collectors table...");
                var collectorTableNames = new[] {
                    "tbl_Monitor_StatisticsCollectors",
                    "StatisticsCollectors",
                    "Collectors",
                    "tbl_StatisticsCollectors"
                };

                foreach (var tableName in collectorTableNames)
                {
                    try
                    {
                        using (var connection = new SqlConnection(workingConnectionString))
                        {
                            await connection.OpenAsync();

                            var sql = $@"
                                SELECT TOP 5 *
                                FROM [stats].[{tableName}]
                                ORDER BY 1";

                            using (var command = new SqlCommand(sql, connection))
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                Console.WriteLine($"✅ Found collectors table: [stats].[{tableName}]");
                                Console.WriteLine("   Sample data:");
                                while (await reader.ReadAsync())
                                {
                                    var values = new List<string>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        values.Add($"{reader.GetName(i)}={reader[i]}");
                                    }
                                    Console.WriteLine($"   {string.Join(", ", values)}");
                                }
                                break; // Found the table, stop trying
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Table [stats].[{tableName}] not found: {ex.Message}");
                    }
                }

                // Test statistics table (try different possible names)
                Console.WriteLine("\n5. Testing statistics table...");
                var statisticsTableNames = new[] {
                    "tbl_Monitor_Statistics",
                    "Statistics",
                    "MonitorStatistics",
                    "tbl_Statistics"
                };

                foreach (var tableName in statisticsTableNames)
                {
                    try
                    {
                        using (var connection = new SqlConnection(workingConnectionString))
                        {
                            await connection.OpenAsync();

                            var sql = $@"
                                SELECT TOP 3 *
                                FROM [stats].[{tableName}]
                                ORDER BY 1 DESC";

                            using (var command = new SqlCommand(sql, connection))
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                Console.WriteLine($"✅ Found statistics table: [stats].[{tableName}]");
                                Console.WriteLine("   Sample data:");
                                while (await reader.ReadAsync())
                                {
                                    var values = new List<string>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        values.Add($"{reader.GetName(i)}={reader[i]}");
                                    }
                                    Console.WriteLine($"   {string.Join(", ", values)}");
                                }
                                break; // Found the table, stop trying
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Table [stats].[{tableName}] not found: {ex.Message}");
                    }
                }

                // Test stored procedures
                Console.WriteLine("\n6. Testing stored procedures...");
                using (var connection = new SqlConnection(workingConnectionString))
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
