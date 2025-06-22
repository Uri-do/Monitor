using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents the execution history of worker integration tests
/// </summary>
public class WorkerTestExecutionHistory
{
    /// <summary>
    /// Unique identifier for the test execution history record
    /// </summary>
    public long TestExecutionHistoryID { get; set; }

    /// <summary>
    /// Test execution identifier (from WorkerTestExecution)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TestId { get; set; } = string.Empty;

    /// <summary>
    /// Type of test executed
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// When the test was started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the test was completed (null if still running)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration of the test execution in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether the test execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Test execution status
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if test failed
    /// </summary>
    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of indicators processed during test
    /// </summary>
    public int IndicatorsProcessed { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Average execution time per indicator in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Memory usage during test in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// CPU usage percentage during test
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Number of alerts triggered during test
    /// </summary>
    public int AlertsTriggered { get; set; }

    /// <summary>
    /// Number of worker processes used
    /// </summary>
    public int WorkerCount { get; set; }

    /// <summary>
    /// Number of concurrent workers
    /// </summary>
    public int ConcurrentWorkers { get; set; }

    /// <summary>
    /// Test configuration as JSON
    /// </summary>
    [MaxLength(4000)]
    public string? TestConfiguration { get; set; }

    /// <summary>
    /// Performance metrics as JSON
    /// </summary>
    [MaxLength(4000)]
    public string? PerformanceMetrics { get; set; }

    /// <summary>
    /// Detailed test results as JSON
    /// </summary>
    public string? DetailedResults { get; set; }

    /// <summary>
    /// Who executed the test
    /// </summary>
    [MaxLength(100)]
    public string? ExecutedBy { get; set; }

    /// <summary>
    /// Execution context (e.g., "Manual", "Scheduled", "API")
    /// </summary>
    [MaxLength(100)]
    public string ExecutionContext { get; set; } = "Manual";

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; set; }
}
