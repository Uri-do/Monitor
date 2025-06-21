namespace MonitoringGrid.Api.DTOs.TestSuite;

/// <summary>
/// Request to run tests
/// </summary>
public class RunTestsRequest
{
    /// <summary>
    /// Test categories to run (e.g., "Framework", "Unit", "Performance", "all")
    /// </summary>
    public string[] Categories { get; set; } = new[] { "all" };

    /// <summary>
    /// Run tests in parallel
    /// </summary>
    public bool Parallel { get; set; } = true;

    /// <summary>
    /// Maximum number of parallel tests
    /// </summary>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>
    /// Test timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Retry failed tests
    /// </summary>
    public bool RetryFailedTests { get; set; } = false;

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Generate detailed report
    /// </summary>
    public bool GenerateReport { get; set; } = true;

    /// <summary>
    /// Verbose output
    /// </summary>
    public bool VerboseOutput { get; set; } = false;

    /// <summary>
    /// Stop on first failure
    /// </summary>
    public bool StopOnFirstFailure { get; set; } = false;
}
