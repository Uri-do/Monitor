using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service for managing KPI scheduling with Quartz.NET
/// </summary>
public interface IKpiSchedulingService
{
    /// <summary>
    /// Schedules a KPI for execution based on its configuration
    /// </summary>
    Task ScheduleKpiAsync(KPI kpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the schedule for an existing KPI
    /// </summary>
    Task UpdateKpiScheduleAsync(KPI kpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a KPI from the scheduler
    /// </summary>
    Task UnscheduleKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all scheduled KPIs
    /// </summary>
    Task<List<ScheduledKpiInfo>> GetScheduledKpisAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next execution time for a specific KPI
    /// </summary>
    Task<DateTime?> GetNextExecutionTimeAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses scheduling for a specific KPI
    /// </summary>
    Task PauseKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes scheduling for a specific KPI
    /// </summary>
    Task ResumeKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers immediate execution of a KPI (outside of schedule)
    /// </summary>
    Task TriggerKpiExecutionAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a schedule configuration
    /// </summary>
    Task<ScheduleValidationResult> ValidateScheduleConfigurationAsync(string scheduleConfiguration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a scheduled KPI
/// </summary>
public class ScheduledKpiInfo
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string TriggerName { get; set; } = string.Empty;
    public DateTime? NextFireTime { get; set; }
    public DateTime? PreviousFireTime { get; set; }
    public string ScheduleType { get; set; } = string.Empty;
    public string ScheduleDescription { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
}

/// <summary>
/// Result of schedule configuration validation
/// </summary>
public class ScheduleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime? NextExecutionTime { get; set; }
    public string? ScheduleDescription { get; set; }
}
