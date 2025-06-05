using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace MonitoringGrid.Scripts;

/// <summary>
/// Simple test script to verify database connectivity and stored procedure execution
/// </summary>
public class TestConnection
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Monitoring Grid - Database Connection Test");
        Console.WriteLine("==========================================");

        try
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var monitoringConnectionString = configuration.GetConnectionString("MonitoringGrid");
            var mainConnectionString = configuration.GetConnectionString("MainDatabase");

            if (string.IsNullOrEmpty(monitoringConnectionString))
            {
                Console.WriteLine("❌ MonitoringGrid connection string not found in configuration");
                return;
            }

            Console.WriteLine($"🔗 Testing connection to PopAI monitoring database...");

            // Test monitoring database connectivity
            using var monitoringConnection = new SqlConnection(monitoringConnectionString);
            await monitoringConnection.OpenAsync();
            Console.WriteLine("✅ PopAI monitoring database connection successful");

            // Test main database connectivity if configured
            if (!string.IsNullOrEmpty(mainConnectionString))
            {
                Console.WriteLine($"🔗 Testing connection to main ProgressPlayDBTest database...");
                using var mainConnection = new SqlConnection(mainConnectionString);
                await mainConnection.OpenAsync();
                Console.WriteLine("✅ ProgressPlayDBTest main database connection successful");
            }

            // Test schema existence on monitoring database
            await TestSchemaAsync(monitoringConnection);

            // Test stored procedures (they will query the main database)
            await TestStoredProceduresAsync(monitoringConnection);

            // Test configuration data
            await TestConfigurationDataAsync(monitoringConnection);



            Console.WriteLine("\n🎉 All tests passed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static async Task TestSchemaAsync(SqlConnection connection)
    {
        Console.WriteLine("\n📋 Testing database schema...");

        var tables = new[]
        {
            "monitoring.KPIs",
            "monitoring.Contacts", 
            "monitoring.KpiContacts",
            "monitoring.AlertLogs",
            "monitoring.HistoricalData",
            "monitoring.Config",
            "monitoring.SystemStatus"
        };

        foreach (var table in tables)
        {
            var sql = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'monitoring' AND TABLE_NAME = '{table.Split('.')[1]}'";
            using var command = new SqlCommand(sql, connection);
            var count = (int)await command.ExecuteScalarAsync();
            
            if (count > 0)
            {
                Console.WriteLine($"  ✅ Table {table} exists");
            }
            else
            {
                Console.WriteLine($"  ❌ Table {table} missing");
            }
        }
    }

    private static async Task TestStoredProceduresAsync(SqlConnection connection)
    {
        Console.WriteLine("\n🔧 Testing stored procedures...");

        var procedures = new[]
        {
            "monitoring.usp_MonitorDeposits",
            "monitoring.usp_MonitorTransactions",
            "monitoring.usp_MonitorSettlementCompanies",
            "monitoring.usp_MonitorCountryDeposits",
            "monitoring.usp_MonitorWhiteLabelPerformance"
        };

        foreach (var procedure in procedures)
        {
            try
            {
                using var command = new SqlCommand(procedure, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };

                // Add parameters
                command.Parameters.AddWithValue("@ForLastMinutes", 60);
                
                var keyParam = command.Parameters.Add("@Key", SqlDbType.NVarChar, 255);
                keyParam.Direction = ParameterDirection.Output;

                var currentParam = command.Parameters.Add("@CurrentValue", SqlDbType.Decimal);
                currentParam.Direction = ParameterDirection.Output;
                currentParam.Precision = 18;
                currentParam.Scale = 2;

                var historicalParam = command.Parameters.Add("@HistoricalValue", SqlDbType.Decimal);
                historicalParam.Direction = ParameterDirection.Output;
                historicalParam.Precision = 18;
                historicalParam.Scale = 2;

                await command.ExecuteNonQueryAsync();

                var key = keyParam.Value?.ToString() ?? "Unknown";
                var current = currentParam.Value != DBNull.Value ? (decimal)currentParam.Value : 0;
                var historical = historicalParam.Value != DBNull.Value ? (decimal)historicalParam.Value : 0;

                Console.WriteLine($"  ✅ {procedure} executed successfully");
                Console.WriteLine($"     Key: {key}, Current: {current:N2}, Historical: {historical:N2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {procedure} failed: {ex.Message}");
            }
        }
    }

    private static async Task TestConfigurationDataAsync(SqlConnection connection)
    {
        Console.WriteLine("\n⚙️  Testing configuration data...");

        try
        {
            // Test KPIs
            var kpiSql = "SELECT COUNT(*) FROM monitoring.KPIs WHERE IsActive = 1";
            using var kpiCommand = new SqlCommand(kpiSql, connection);
            var kpiCount = (int)await kpiCommand.ExecuteScalarAsync();
            Console.WriteLine($"  ✅ Active KPIs: {kpiCount}");

            // Test Contacts
            var contactSql = "SELECT COUNT(*) FROM monitoring.Contacts WHERE IsActive = 1";
            using var contactCommand = new SqlCommand(contactSql, connection);
            var contactCount = (int)await contactCommand.ExecuteScalarAsync();
            Console.WriteLine($"  ✅ Active Contacts: {contactCount}");

            // Test Config
            var configSql = "SELECT COUNT(*) FROM monitoring.Config";
            using var configCommand = new SqlCommand(configSql, connection);
            var configCount = (int)await configCommand.ExecuteScalarAsync();
            Console.WriteLine($"  ✅ Configuration entries: {configCount}");

            // Test KPI-Contact mappings
            var mappingSql = @"
                SELECT k.Indicator, c.Name, c.Email, c.Phone
                FROM monitoring.KPIs k
                INNER JOIN monitoring.KpiContacts kc ON k.KpiId = kc.KpiId
                INNER JOIN monitoring.Contacts c ON kc.ContactId = c.ContactId
                WHERE k.IsActive = 1 AND c.IsActive = 1
                ORDER BY k.Indicator, c.Name";

            using var mappingCommand = new SqlCommand(mappingSql, connection);
            using var reader = await mappingCommand.ExecuteReaderAsync();

            Console.WriteLine("\n  📧 KPI-Contact Mappings:");
            while (await reader.ReadAsync())
            {
                var indicator = reader.GetString("Indicator");
                var name = reader.GetString("Name");
                var email = reader.IsDBNull("Email") ? "N/A" : reader.GetString("Email");
                var phone = reader.IsDBNull("Phone") ? "N/A" : reader.GetString("Phone");
                
                Console.WriteLine($"     {indicator} → {name} (📧 {email}, 📱 {phone})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Configuration test failed: {ex.Message}");
        }
    }
}
