using Microsoft.Data.SqlClient;
using System.Data;

namespace TestKpi;

class Program
{
    // Connection string for ProgressPlayDBTest database
    private static readonly string ConnectionString = 
        "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDBTest;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing KPI Execution: [stats].[stp_MonitorTransactions]");
        Console.WriteLine("Using 1000 minutes for testing with ProgressPlayDBTest database");
        Console.WriteLine();

        try
        {
            await ExecuteKpiStoredProcedure();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task ExecuteKpiStoredProcedure()
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        Console.WriteLine("Connected to ProgressPlayDBTest database successfully.");

        using var command = new SqlCommand("[stats].[stp_MonitorTransactions]", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60 // 60 seconds timeout
        };

        // Add input parameter - using 1000 minutes as requested
        command.Parameters.AddWithValue("@ForLastMinutes", 1000);

        // Add output parameters based on the standard KPI procedure format
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

        Console.WriteLine("Executing stored procedure [stats].[stp_MonitorTransactions] with @ForLastMinutes = 1000...");
        
        var startTime = DateTime.Now;
        await command.ExecuteNonQueryAsync();
        var endTime = DateTime.Now;

        // Get output values
        var key = keyParam.Value?.ToString() ?? "NULL";
        var currentValue = currentParam.Value == DBNull.Value ? (decimal?)null : (decimal)currentParam.Value;
        var historicalValue = historicalParam.Value == DBNull.Value ? (decimal?)null : (decimal)historicalParam.Value;

        // Calculate deviation if both values are available
        decimal? deviation = null;
        if (currentValue.HasValue && historicalValue.HasValue && historicalValue.Value != 0)
        {
            deviation = Math.Abs((currentValue.Value - historicalValue.Value) / historicalValue.Value) * 100;
        }

        // Display results
        Console.WriteLine("\n=== KPI Execution Results ===");
        Console.WriteLine($"Execution Time: {(endTime - startTime).TotalSeconds:F2} seconds");
        Console.WriteLine($"Key: {key}");
        Console.WriteLine($"Current Value: {currentValue?.ToString("N2") ?? "NULL"}");
        Console.WriteLine($"Historical Value: {historicalValue?.ToString("N2") ?? "NULL"}");
        
        if (deviation.HasValue)
        {
            Console.WriteLine($"Deviation: {deviation.Value:F2}%");
            
            // Determine if this would trigger an alert (assuming 15% threshold)
            var alertThreshold = 15.0m;
            if (deviation.Value > alertThreshold)
            {
                Console.WriteLine($"⚠️  ALERT: Deviation ({deviation.Value:F2}%) exceeds threshold ({alertThreshold}%)");
            }
            else
            {
                Console.WriteLine($"✅ OK: Deviation ({deviation.Value:F2}%) is within threshold ({alertThreshold}%)");
            }
        }
        
        Console.WriteLine("=== End Results ===");
    }
}
