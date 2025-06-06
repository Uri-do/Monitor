using Microsoft.Data.SqlClient;
using System.Data;

namespace TestKpi;

class TestEnhancedKpiExecution
{
    // Connection strings
    private static readonly string MonitoringConnectionString = 
        "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true";
    
    private static readonly string MainConnectionString = 
        "Data Source=192.168.166.11,1433;Initial Catalog=ProgressPlayDBTest;User ID=saturn;Password=Vt0zXXc800;MultipleActiveResultSets=true;TrustServerCertificate=true";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Enhanced KPI Execution Service");
        Console.WriteLine("======================================");
        Console.WriteLine();

        try
        {
            // Test 1: Add the KPI to the database
            await AddTransactionMonitoringKpi();
            
            // Test 2: Test the enhanced execution logic
            await TestKpiExecution();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task AddTransactionMonitoringKpi()
    {
        Console.WriteLine("Step 1: Adding Transaction Monitoring KPI to database...");
        
        using var connection = new SqlConnection(MonitoringConnectionString);
        await connection.OpenAsync();

        var sql = @"
            MERGE monitoring.KPIs AS target
            USING (VALUES 
                ('Transaction Success Rate', 'Gavriel', 1, 30, 5.00, '[stats].[stp_MonitorTransactions]', 
                 'Transaction success rate alert: {deviation}% deviation detected', 
                 'Transaction monitoring alert: Current success rate is {current}%, historical average is {historical}%. Deviation of {deviation}% detected.', 
                 1, NULL, 60, 90.00)
            ) AS source (Indicator, Owner, Priority, Frequency, Deviation, SpName, SubjectTemplate, DescriptionTemplate, IsActive, LastRun, CooldownMinutes, MinimumThreshold)
            ON target.Indicator = source.Indicator
            WHEN MATCHED THEN
                UPDATE SET 
                    Owner = source.Owner,
                    Priority = source.Priority,
                    Frequency = source.Frequency,
                    Deviation = source.Deviation,
                    SpName = source.SpName,
                    SubjectTemplate = source.SubjectTemplate,
                    DescriptionTemplate = source.DescriptionTemplate,
                    IsActive = source.IsActive,
                    CooldownMinutes = source.CooldownMinutes,
                    MinimumThreshold = source.MinimumThreshold,
                    ModifiedDate = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (Indicator, Owner, Priority, Frequency, Deviation, SpName, SubjectTemplate, DescriptionTemplate, IsActive, LastRun, CooldownMinutes, MinimumThreshold, CreatedDate, ModifiedDate)
                VALUES (source.Indicator, source.Owner, source.Priority, source.Frequency, source.Deviation, source.SpName, source.SubjectTemplate, source.DescriptionTemplate, source.IsActive, source.LastRun, source.CooldownMinutes, source.MinimumThreshold, GETUTCDATE(), GETUTCDATE());
        ";

        using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteNonQueryAsync();
        
        Console.WriteLine($"✅ KPI added/updated successfully. Rows affected: {result}");
        
        // Get the KPI ID
        var getIdSql = "SELECT KpiId FROM monitoring.KPIs WHERE Indicator = 'Transaction Success Rate'";
        using var getIdCommand = new SqlCommand(getIdSql, connection);
        var kpiId = await getIdCommand.ExecuteScalarAsync();
        
        Console.WriteLine($"✅ Transaction Success Rate KPI ID: {kpiId}");
        Console.WriteLine();
    }

    static async Task TestKpiExecution()
    {
        Console.WriteLine("Step 2: Testing KPI execution with result set processing...");
        
        // Simulate the enhanced KPI execution logic
        var kpi = new TestKpi
        {
            Indicator = "Transaction Success Rate",
            SpName = "[stats].[stp_MonitorTransactions]",
            Frequency = 1000 // Use 1000 minutes for testing
        };

        var result = await ExecuteKpiWithResultSet(kpi);
        
        Console.WriteLine("=== KPI Execution Results ===");
        Console.WriteLine($"Key: {result.Key}");
        Console.WriteLine($"Current Value: {result.CurrentValue:F2}%");
        Console.WriteLine($"Historical Value: {result.HistoricalValue:F2}%");
        Console.WriteLine($"Deviation: {result.DeviationPercent:F2}%");
        Console.WriteLine($"Should Alert: {result.ShouldAlert}");
        Console.WriteLine($"Execution Time: {result.ExecutionTime}");
        Console.WriteLine("=============================");
    }

    static async Task<TestKpiExecutionResult> ExecuteKpiWithResultSet(TestKpi kpi)
    {
        using var connection = new SqlConnection(MainConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(kpi.SpName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@ForLastMinutes", kpi.Frequency);

        using var reader = await command.ExecuteReaderAsync();
        
        // Process transaction monitoring result set
        int totalTransactions = 0;
        int successfulTransactions = 0;

        while (await reader.ReadAsync())
        {
            var itemName = reader.GetString("ItemName");
            var total = reader.GetInt32("Total");
            var successful = reader.GetInt32("Successful");

            totalTransactions += total;
            successfulTransactions += successful;

            Console.WriteLine($"  {itemName}: {successful}/{total} successful");
        }

        // Calculate success rate
        var successRate = totalTransactions > 0 ? (decimal)successfulTransactions / totalTransactions * 100 : 100;
        var historicalSuccessRate = 95.0m; // Baseline
        var deviation = Math.Abs((successRate - historicalSuccessRate) / historicalSuccessRate) * 100;
        
        // Determine if alert should be triggered (5% deviation threshold, 90% minimum threshold)
        var shouldAlert = deviation > 5.0m || successRate < 90.0m;

        return new TestKpiExecutionResult
        {
            Key = "TransactionSuccessRate",
            CurrentValue = successRate,
            HistoricalValue = historicalSuccessRate,
            DeviationPercent = deviation,
            ShouldAlert = shouldAlert,
            ExecutionTime = DateTime.UtcNow
        };
    }
}

// Test classes
class TestKpi
{
    public string Indicator { get; set; } = string.Empty;
    public string SpName { get; set; } = string.Empty;
    public int Frequency { get; set; }
}

class TestKpiExecutionResult
{
    public string Key { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal DeviationPercent { get; set; }
    public bool ShouldAlert { get; set; }
    public DateTime ExecutionTime { get; set; }
}
