namespace MonitoringGrid.Api.DTOs.TestSuite;

/// <summary>
/// Test suite status response
/// </summary>
public class TestSuiteStatusResponse
{
    /// <summary>
    /// Whether tests are currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Total number of tests available
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Last test run time
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Available test categories
    /// </summary>
    public string[] AvailableCategories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Latest test results
    /// </summary>
    public List<TestResult> TestResults { get; set; } = new();
}

/// <summary>
/// Test execution response
/// </summary>
public class TestExecutionResponse
{
    /// <summary>
    /// Unique execution identifier
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Categories being tested
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; set; }
}

/// <summary>
/// Test execution status response
/// </summary>
public class TestExecutionStatusResponse
{
    /// <summary>
    /// Execution identifier
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution is running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Currently executing test
    /// </summary>
    public string? CurrentTest { get; set; }

    /// <summary>
    /// Test results
    /// </summary>
    public List<TestResult>? Results { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time (if completed)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Test history response
/// </summary>
public class TestHistoryResponse
{
    /// <summary>
    /// Test execution summaries
    /// </summary>
    public List<TestExecutionSummary> Executions { get; set; } = new();

    /// <summary>
    /// Current page
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Individual test result
/// </summary>
public class TestResult
{
    /// <summary>
    /// Test identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Test name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Test category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Test status (passed, failed, skipped, running)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Stack trace (if failed)
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Test execution summary
/// </summary>
public class TestExecutionSummary
{
    /// <summary>
    /// Execution identifier
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Categories tested
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Total number of tests
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Number of passed tests
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// Number of failed tests
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => TotalTests > 0 ? (PassedTests / (double)TotalTests) * 100 : 0;
}
