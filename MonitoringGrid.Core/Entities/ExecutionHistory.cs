namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents the execution history of an indicator
/// </summary>
public class ExecutionHistory
{
    /// <summary>
    /// Unique identifier for the execution history record
    /// </summary>
    public long ExecutionHistoryID { get; set; }

    /// <summary>
    /// Associated indicator identifier
    /// </summary>
    public long IndicatorID { get; set; }

    /// <summary>
    /// When the indicator was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Duration of the execution in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether the execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result of the execution
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records returned by the query
    /// </summary>
    public int? RecordCount { get; set; }

    /// <summary>
    /// Execution context (manual, scheduled, etc.)
    /// </summary>
    public string? ExecutionContext { get; set; }

    /// <summary>
    /// User who triggered the execution (if manual)
    /// </summary>
    public string? ExecutedBy { get; set; }

    /// <summary>
    /// Additional execution metadata
    /// </summary>
    public string? Metadata { get; set; }

    #region Navigation Properties

    /// <summary>
    /// Associated indicator
    /// </summary>
    public virtual Indicator Indicator { get; set; } = null!;

    #endregion
}
