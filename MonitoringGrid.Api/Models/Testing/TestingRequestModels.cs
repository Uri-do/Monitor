using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.Models.Testing;

/// <summary>
/// Request to generate test data
/// </summary>
public class GenerateTestDataRequest
{
    [Required]
    public string Method { get; set; } = string.Empty;

    [Required]
    public string Path { get; set; } = string.Empty;

    public int NumberOfSamples { get; set; } = 5;
    public bool IncludeValidData { get; set; } = true;
    public bool IncludeInvalidData { get; set; } = true;
    public bool IncludeEdgeCases { get; set; } = true;
    public string? DataType { get; set; }
}

/// <summary>
/// Generated test data
/// </summary>
public class GeneratedTestData
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<TestDataSample> ValidSamples { get; set; } = new();
    public List<TestDataSample> InvalidSamples { get; set; } = new();
    public List<TestDataSample> EdgeCaseSamples { get; set; } = new();
    public Dictionary<string, object> ParameterSchemas { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Test data sample
/// </summary>
public class TestDataSample
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public object? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int ExpectedStatusCode { get; set; }
    public string? ExpectedResult { get; set; }
}

/// <summary>
/// Request to validate endpoint
/// </summary>
public class ValidateEndpointRequest
{
    [Required]
    public string Method { get; set; } = string.Empty;

    [Required]
    public string Path { get; set; } = string.Empty;

    public bool ValidateAuthentication { get; set; } = true;
    public bool ValidateAuthorization { get; set; } = true;
    public bool ValidateInputValidation { get; set; } = true;
    public bool ValidateOutputFormat { get; set; } = true;
    public bool ValidatePerformance { get; set; } = true;
    public bool ValidateSecurity { get; set; } = true;
}

/// <summary>
/// Endpoint validation result
/// </summary>
public class EndpointValidationResult
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<ValidationIssue> Warnings { get; set; } = new();
    public EndpointValidationDetails Details { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Endpoint validation details
/// </summary>
public class EndpointValidationDetails
{
    public bool HasAuthentication { get; set; }
    public bool HasAuthorization { get; set; }
    public bool HasInputValidation { get; set; }
    public bool HasOutputValidation { get; set; }
    public bool HasErrorHandling { get; set; }
    public bool HasDocumentation { get; set; }
    public bool HasRateLimiting { get; set; }
    public bool HasCaching { get; set; }
    public List<string> SecurityHeaders { get; set; } = new();
    public List<string> SupportedContentTypes { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// API endpoint test configuration
/// </summary>
public class EndpointTestConfiguration
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public TestPriority Priority { get; set; } = TestPriority.Medium;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public object? DefaultBody { get; set; }
    public List<TestScenario> TestScenarios { get; set; } = new();
    public PerformanceThresholds PerformanceThresholds { get; set; } = new();
    public SecurityTestConfiguration SecurityTests { get; set; } = new();
}

/// <summary>
/// Test priority levels
/// </summary>
public enum TestPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Test scenario for endpoint
/// </summary>
public class TestScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public object? Body { get; set; }
    public int ExpectedStatusCode { get; set; } = 200;
    public string? ExpectedResponsePattern { get; set; }
    public bool ShouldFail { get; set; } = false;
    public string? FailureReason { get; set; }
    public List<ResponseAssertion> Assertions { get; set; } = new();
}

/// <summary>
/// Response assertion for testing
/// </summary>
public class ResponseAssertion
{
    public string Type { get; set; } = string.Empty; // StatusCode, Header, Body, Schema, Performance
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty; // Equals, Contains, GreaterThan, LessThan, Matches
    public object? ExpectedValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Performance thresholds for endpoint testing
/// </summary>
public class PerformanceThresholds
{
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan WarningResponseTime { get; set; } = TimeSpan.FromSeconds(2);
    public long MaxResponseSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public long MaxMemoryUsageBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public double MaxCpuUsagePercent { get; set; } = 80.0;
    public int MinRequestsPerSecond { get; set; } = 10;
}

/// <summary>
/// Security test configuration
/// </summary>
public class SecurityTestConfiguration
{
    public bool TestAuthentication { get; set; } = true;
    public bool TestAuthorization { get; set; } = true;
    public bool TestInputValidation { get; set; } = true;
    public bool TestSqlInjection { get; set; } = true;
    public bool TestXssAttacks { get; set; } = true;
    public bool TestCsrfProtection { get; set; } = true;
    public bool TestRateLimiting { get; set; } = true;
    public bool TestSecurityHeaders { get; set; } = true;
    public List<string> RequiredSecurityHeaders { get; set; } = new()
    {
        "X-Content-Type-Options",
        "X-Frame-Options",
        "X-XSS-Protection",
        "Strict-Transport-Security"
    };
}

/// <summary>
/// Load test configuration
/// </summary>
public class LoadTestConfiguration
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int ConcurrentUsers { get; set; } = 10;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RampUpTime { get; set; } = TimeSpan.FromMinutes(1);
    public int RequestsPerSecond { get; set; } = 10;
    public Dictionary<string, object>? Parameters { get; set; }
    public object? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public PerformanceThresholds Thresholds { get; set; } = new();
}

/// <summary>
/// Load test result
/// </summary>
public class LoadTestResult
{
    public string TestName { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double RequestsPerSecond { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan P50ResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
    public List<LoadTestError> Errors { get; set; } = new();
    public List<PerformanceDataPoint> PerformanceData { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Load test error
/// </summary>
public class LoadTestError
{
    public DateTime Timestamp { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Performance data point for load testing
/// </summary>
public class PerformanceDataPoint
{
    public DateTime Timestamp { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int ActiveUsers { get; set; }
    public double RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
}

/// <summary>
/// Test environment configuration
/// </summary>
public class TestEnvironmentConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public AuthenticationConfiguration? Authentication { get; set; }
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool VerifySslCertificate { get; set; } = true;
    public ProxyConfiguration? Proxy { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Authentication configuration for testing
/// </summary>
public class AuthenticationConfiguration
{
    public string Type { get; set; } = string.Empty; // Bearer, Basic, ApiKey, Custom
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiKeyHeader { get; set; }
    public Dictionary<string, string>? CustomHeaders { get; set; }
}

/// <summary>
/// Proxy configuration for testing
/// </summary>
public class ProxyConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSystemProxy { get; set; } = false;
}
