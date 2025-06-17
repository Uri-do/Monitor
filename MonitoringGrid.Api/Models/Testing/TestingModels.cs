using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.Models.Testing;

/// <summary>
/// Represents a testable API endpoint
/// </summary>
public class TestableEndpoint
{
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EndpointParameter> Parameters { get; set; } = new();
    public List<string> RequiredRoles { get; set; } = new();
    public bool RequiresAuthentication { get; set; }
    public List<ResponseExample> ResponseExamples { get; set; } = new();
    public TestComplexity Complexity { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Endpoint parameter information
/// </summary>
public class EndpointParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Query, Body, Route, Header
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public List<string>? AllowedValues { get; set; }
    public string? ValidationRules { get; set; }
}

/// <summary>
/// Response example for endpoint
/// </summary>
public class ResponseExample
{
    public int StatusCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public object? ExampleData { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Test complexity levels
/// </summary>
public enum TestComplexity
{
    Simple,
    Medium,
    Complex,
    Advanced
}

/// <summary>
/// Request to execute a single test
/// </summary>
public class ExecuteTestRequest
{
    [Required]
    public string Method { get; set; } = string.Empty;

    [Required]
    public string Path { get; set; } = string.Empty;

    public Dictionary<string, object>? Parameters { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public object? Body { get; set; }
    public bool IncludePerformanceMetrics { get; set; } = true;
    public bool IncludeResponseValidation { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public string? TestName { get; set; }
    public string? TestDescription { get; set; }
}

/// <summary>
/// Result of test execution
/// </summary>
public class TestExecutionResult
{
    public string TestName { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResponseBody { get; set; }
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public TimeSpan TestDuration { get; set; }
    public string? ErrorMessage { get; set; }
    public TestPerformanceMetrics? PerformanceMetrics { get; set; }
    public TestValidationResult? ValidationResult { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? ExecutedBy { get; set; }
}

/// <summary>
/// Performance metrics for test execution
/// </summary>
public class TestPerformanceMetrics
{
    public TimeSpan ResponseTime { get; set; }
    public long ResponseSizeBytes { get; set; }
    public TimeSpan DnsLookupTime { get; set; }
    public TimeSpan ConnectionTime { get; set; }
    public TimeSpan SslHandshakeTime { get; set; }
    public TimeSpan TimeToFirstByte { get; set; }
    public TimeSpan ContentDownloadTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
}

/// <summary>
/// Test validation result
/// </summary>
public class TestValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public SchemaValidationResult? SchemaValidation { get; set; }
    public SecurityValidationResult? SecurityValidation { get; set; }
    public PerformanceValidationResult? PerformanceValidation { get; set; }
}

/// <summary>
/// Validation issue
/// </summary>
public class ValidationIssue
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Field { get; set; }
    public object? ExpectedValue { get; set; }
    public object? ActualValue { get; set; }
}

/// <summary>
/// Schema validation result
/// </summary>
public class SchemaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? ExpectedSchema { get; set; }
    public string? ActualSchema { get; set; }
}

/// <summary>
/// Security validation result
/// </summary>
public class SecurityValidationResult
{
    public bool IsSecure { get; set; }
    public List<SecurityIssue> Issues { get; set; } = new();
    public bool HasProperAuthentication { get; set; }
    public bool HasProperAuthorization { get; set; }
    public bool HasInputValidation { get; set; }
    public bool HasOutputSanitization { get; set; }
}

/// <summary>
/// Security issue
/// </summary>
public class SecurityIssue
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Performance validation result
/// </summary>
public class PerformanceValidationResult
{
    public bool MeetsPerformanceThresholds { get; set; }
    public List<PerformanceIssue> Issues { get; set; } = new();
    public TimeSpan ResponseTimeThreshold { get; set; }
    public TimeSpan ActualResponseTime { get; set; }
    public long MemoryThresholdBytes { get; set; }
    public long ActualMemoryBytes { get; set; }
}

/// <summary>
/// Performance issue
/// </summary>
public class PerformanceIssue
{
    public string Metric { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? ThresholdValue { get; set; }
    public object? ActualValue { get; set; }
}

/// <summary>
/// Request to execute batch tests
/// </summary>
public class ExecuteBatchTestRequest
{
    [Required]
    public List<ExecuteTestRequest> Tests { get; set; } = new();

    public bool StopOnFirstFailure { get; set; } = false;
    public bool RunInParallel { get; set; } = false;
    public int MaxParallelTests { get; set; } = 5;
    public bool IncludeDetailedResults { get; set; } = true;
    public string? BatchName { get; set; }
    public string? BatchDescription { get; set; }
}

/// <summary>
/// Result of batch test execution
/// </summary>
public class BatchTestExecutionResult
{
    public string BatchName { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<TestExecutionResult> Results { get; set; } = new();
    public BatchTestSummary Summary { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? ExecutedBy { get; set; }
}

/// <summary>
/// Batch test summary
/// </summary>
public class BatchTestSummary
{
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan FastestResponse { get; set; }
    public TimeSpan SlowestResponse { get; set; }
    public List<string> FailedEndpoints { get; set; } = new();
    public List<string> SlowEndpoints { get; set; } = new();
    public Dictionary<string, int> StatusCodeDistribution { get; set; } = new();
}

/// <summary>
/// Request to execute test suite
/// </summary>
public class ExecuteTestSuiteRequest
{
    public List<string>? IncludeControllers { get; set; }
    public List<string>? ExcludeControllers { get; set; }
    public List<string>? IncludeEndpoints { get; set; }
    public List<string>? ExcludeEndpoints { get; set; }
    public bool IncludePerformanceTests { get; set; } = true;
    public bool IncludeSecurityTests { get; set; } = true;
    public bool IncludeValidationTests { get; set; } = true;
    public bool RunInParallel { get; set; } = false;
    public int MaxParallelTests { get; set; } = 3;
    public string? SuiteName { get; set; }
    public string? SuiteDescription { get; set; }
}

/// <summary>
/// Result of test suite execution
/// </summary>
public class TestSuiteExecutionResult
{
    public string SuiteName { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<ControllerTestResult> ControllerResults { get; set; } = new();
    public TestSuiteSummary Summary { get; set; } = new();
    public List<TestExecutionResult> FailedTestDetails { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? ExecutedBy { get; set; }
}

/// <summary>
/// Controller test result
/// </summary>
public class ControllerTestResult
{
    public string ControllerName { get; set; } = string.Empty;
    public int TotalEndpoints { get; set; }
    public int SuccessfulEndpoints { get; set; }
    public int FailedEndpoints { get; set; }
    public TimeSpan Duration { get; set; }
    public List<TestExecutionResult> EndpointResults { get; set; } = new();
}

/// <summary>
/// Test suite summary
/// </summary>
public class TestSuiteSummary
{
    public double OverallSuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public Dictionary<string, double> ControllerSuccessRates { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
    public List<string> PerformanceIssues { get; set; } = new();
    public List<string> SecurityIssues { get; set; } = new();
    public Dictionary<string, int> StatusCodeDistribution { get; set; } = new();
}

/// <summary>
/// Request to get test history
/// </summary>
public class GetTestHistoryRequest
{
    public int Days { get; set; } = 7;
    public string? Controller { get; set; }
    public string? Endpoint { get; set; }
    public bool? SuccessOnly { get; set; }
    public bool IncludeDetails { get; set; } = false;
    public int PageSize { get; set; } = 50;
    public int Page { get; set; } = 1;
}

/// <summary>
/// Test execution history
/// </summary>
public class TestExecutionHistory
{
    public int TotalExecutions { get; set; }
    public List<TestExecutionSummary> Executions { get; set; } = new();
    public TestHistoryStatistics Statistics { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Test execution summary
/// </summary>
public class TestExecutionSummary
{
    public string Id { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? ExecutedBy { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Test history statistics
/// </summary>
public class TestHistoryStatistics
{
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public Dictionary<string, int> ControllerExecutions { get; set; } = new();
    public Dictionary<string, double> ControllerSuccessRates { get; set; } = new();
    public List<TrendDataPoint> SuccessRateTrend { get; set; } = new();
    public List<TrendDataPoint> ResponseTimeTrend { get; set; } = new();
}

/// <summary>
/// Trend data point
/// </summary>
public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Request to get test statistics
/// </summary>
public class GetTestStatisticsRequest
{
    public int Days { get; set; } = 30;
    public string? Controller { get; set; }
    public bool IncludeTrends { get; set; } = true;
    public bool IncludePerformanceMetrics { get; set; } = true;
}

/// <summary>
/// Test statistics
/// </summary>
public class TestStatistics
{
    public TestOverviewStatistics Overview { get; set; } = new();
    public List<ControllerStatistics> ControllerStats { get; set; } = new();
    public List<EndpointStatistics> EndpointStats { get; set; } = new();
    public PerformanceStatistics Performance { get; set; } = new();
    public List<TrendDataPoint> SuccessRateTrend { get; set; } = new();
    public List<TrendDataPoint> ResponseTimeTrend { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Test overview statistics
/// </summary>
public class TestOverviewStatistics
{
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int TotalEndpoints { get; set; }
    public int TestedEndpoints { get; set; }
    public double EndpointCoverage { get; set; }
}

/// <summary>
/// Controller statistics
/// </summary>
public class ControllerStatistics
{
    public string ControllerName { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int TotalEndpoints { get; set; }
    public int TestedEndpoints { get; set; }
    public List<string> MostFailedEndpoints { get; set; } = new();
    public List<string> SlowestEndpoints { get; set; } = new();
}

/// <summary>
/// Endpoint statistics
/// </summary>
public class EndpointStatistics
{
    public string Controller { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public DateTime LastTested { get; set; }
    public List<string> CommonErrors { get; set; } = new();
}

/// <summary>
/// Performance statistics
/// </summary>
public class PerformanceStatistics
{
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public TimeSpan FastestResponse { get; set; }
    public TimeSpan SlowestResponse { get; set; }
    public double RequestsPerSecond { get; set; }
    public long AverageResponseSize { get; set; }
    public List<string> SlowestEndpoints { get; set; } = new();
    public List<string> FastestEndpoints { get; set; } = new();
}
