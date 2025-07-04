using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents an Indicator configuration (Aggregate Root)
/// Replaces the KPI entity with enhanced functionality for ProgressPlayDB integration
/// </summary>
public class Indicator : AggregateRoot
{
    public long IndicatorID { get; set; }

    [Required]
    [MaxLength(255)]
    public string IndicatorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string IndicatorCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? IndicatorDesc { get; set; }

    /// <summary>
    /// Reference to the collector in ProgressPlayDB.stats.tbl_Monitor_StatisticsCollectors
    /// </summary>
    public long CollectorID { get; set; }

    /// <summary>
    /// The specific ItemName from ProgressPlayDB.stats.tbl_Monitor_Statistics for this collector
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string CollectorItemName { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the scheduler configuration
    /// </summary>
    public int? SchedulerID { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Time window in minutes for data analysis (how far back to look for data)
    /// </summary>
    public int LastMinutes { get; set; } = 60;

    /// <summary>
    /// Threshold type: volume_average, threshold_value, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ThresholdType { get; set; } = string.Empty;

    /// <summary>
    /// Field to evaluate: Total, Marked, MarkedPercent
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ThresholdField { get; set; } = string.Empty;

    /// <summary>
    /// Comparison operator: gt, gte, lt, lte, eq
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string ThresholdComparison { get; set; } = string.Empty;

    /// <summary>
    /// Threshold value for comparison
    /// </summary>
    public decimal ThresholdValue { get; set; }

    /// <summary>
    /// Priority level: high, medium, low
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// Owner contact ID reference
    /// </summary>
    public int OwnerContactId { get; set; }

    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastRun { get; set; }
    public string? LastRunResult { get; set; }

    /// <summary>
    /// Average value for the current hour (for volume_average threshold type)
    /// </summary>
    public decimal? AverageHour { get; set; }

    /// <summary>
    /// Number of days to look back for average calculation
    /// </summary>
    public int? AverageLastDays { get; set; }



    /// <summary>
    /// Execution state tracking for real-time monitoring
    /// </summary>
    public bool IsCurrentlyRunning { get; set; } = false;
    public DateTime? ExecutionStartTime { get; set; }
    public string? ExecutionContext { get; set; } // "Manual", "Scheduled", "Test"

    // Navigation properties
    public virtual Contact? OwnerContact { get; set; }
    public virtual Scheduler? Scheduler { get; set; }
    public virtual ICollection<IndicatorContact> IndicatorContacts { get; set; } = new List<IndicatorContact>();
    public virtual ICollection<AlertLog> AlertLogs { get; set; } = new List<AlertLog>();

    // Note: Collector relationship removed since MonitorStatisticsCollector is in a different database (ProgressPlayDBTest)

    // Domain methods
    public bool IsDue()
    {
        if (!IsActive || Scheduler == null)
            return false;

        if (!Scheduler.IsEnabled || !Scheduler.IsCurrentlyActive())
            return false;

        if (!LastRun.HasValue)
            return true;

        var nextExecution = Scheduler.GetNextExecutionTime(LastRun);
        return nextExecution.HasValue && DateTime.UtcNow >= nextExecution.Value;
    }

    public DateTime? GetNextRunTime()
    {
        if (Scheduler == null || !Scheduler.IsEnabled)
            return null;

        return Scheduler.GetNextExecutionTime(LastRun);
    }

    public bool IsOverdue()
    {
        if (!IsActive || Scheduler == null || !Scheduler.IsEnabled)
            return false;

        var nextExecution = GetNextRunTime();
        return nextExecution.HasValue && DateTime.UtcNow > nextExecution.Value;
    }

    public int GetMinutesOverdue()
    {
        if (!IsOverdue())
            return 0;

        var nextExecution = GetNextRunTime();
        if (!nextExecution.HasValue)
            return 0;

        var overdueDuration = DateTime.UtcNow - nextExecution.Value;
        return (int)Math.Max(0, overdueDuration.TotalMinutes);
    }

    /// <summary>
    /// Marks the indicator as currently executing
    /// </summary>
    public void StartExecution(string context = "Manual")
    {
        IsCurrentlyRunning = true;
        ExecutionStartTime = DateTime.UtcNow;
        ExecutionContext = context;
        MarkAsModified();

        // Raise domain event for real-time updates
        var startedEvent = new IndicatorExecutionStartedEvent(IndicatorID, IndicatorName, OwnerContactId.ToString(), context);
        AddDomainEvent(startedEvent);
    }

    /// <summary>
    /// Marks the indicator execution as completed
    /// </summary>
    public void CompleteExecution()
    {
        IsCurrentlyRunning = false;
        ExecutionStartTime = null;
        ExecutionContext = null;
        MarkAsModified();

        // Raise domain event for real-time updates
        var completedEvent = new IndicatorExecutionCompletedEvent(IndicatorID, IndicatorName, OwnerContactId.ToString());
        AddDomainEvent(completedEvent);
    }

    /// <summary>
    /// Gets the execution duration if currently running
    /// </summary>
    public TimeSpan? GetExecutionDuration()
    {
        if (!IsCurrentlyRunning || !ExecutionStartTime.HasValue)
            return null;

        return DateTime.UtcNow - ExecutionStartTime.Value;
    }

    /// <summary>
    /// Executes the indicator and raises appropriate domain events
    /// </summary>
    public void MarkAsExecuted(bool wasSuccessful, decimal? currentValue = null,
        decimal? historicalValue = null, string? errorMessage = null)
    {
        UpdateLastRun();

        // Raise domain event
        var executedEvent = new IndicatorExecutedEvent(
            IndicatorID, IndicatorName, OwnerContactId.ToString(), wasSuccessful,
            currentValue, historicalValue, errorMessage);

        AddDomainEvent(executedEvent);
    }

    private void UpdateLastRun()
    {
        LastRun = DateTime.UtcNow;
        UpdatedDate = DateTime.UtcNow;
        MarkAsModified();
    }
}
