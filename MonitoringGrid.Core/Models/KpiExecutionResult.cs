namespace MonitoringGrid.Core.Models;

/// <summary>
/// Result of KPI execution
/// </summary>
public class KpiExecutionResult
{
    public string Key { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal DeviationPercent { get; set; }

    // Alias for compatibility with services that expect DeviationPercentage
    public decimal DeviationPercentage
    {
        get => DeviationPercent;
        set => DeviationPercent = value;
    }

    public bool ShouldAlert { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    public int? ExecutionTimeMs { get; set; }
    public string? ExecutionDetails { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    // Enhanced execution information
    public ExecutionTimingInfo? TimingInfo { get; set; }
    public DatabaseExecutionInfo? DatabaseInfo { get; set; }
    public List<ExecutionStepInfo>? ExecutionSteps { get; set; }

    // Domain methods
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);

    public string GetSummary()
    {
        if (!IsSuccessful)
            return $"Execution failed: {ErrorMessage}";

        return $"Current: {CurrentValue:N2}, Historical: {HistoricalValue:N2}, Deviation: {DeviationPercent:N2}%";
    }

    public string GetSeverity()
    {
        return DeviationPercent switch
        {
            >= 50 => "Critical",
            >= 25 => "High",
            >= 10 => "Medium",
            _ => "Low"
        };
    }
}

/// <summary>
/// Detailed timing information for KPI execution
/// </summary>
public class ExecutionTimingInfo
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalExecutionMs { get; set; }
    public int DatabaseConnectionMs { get; set; }
    public int StoredProcedureExecutionMs { get; set; }
    public int ResultProcessingMs { get; set; }
    public int HistoricalDataSaveMs { get; set; }
}

/// <summary>
/// Database execution information
/// </summary>
public class DatabaseExecutionInfo
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string SqlCommand { get; set; } = string.Empty;
    public string SqlParameters { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public int RowsReturned { get; set; }
    public int ResultSetsReturned { get; set; }
}

/// <summary>
/// Individual execution step information
/// </summary>
public class ExecutionStepInfo
{
    public string StepName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMs { get; set; }
    public string Status { get; set; } = string.Empty; // "Success", "Error", "Warning"
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? StepMetadata { get; set; }
}
