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
