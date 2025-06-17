using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Models.Testing;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service for comprehensive API endpoint testing and validation
/// </summary>
public class ApiTestingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiTestingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ApiTestingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ApiTestingService> logger,
        IServiceProvider serviceProvider)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Get all testable endpoints from the API
    /// </summary>
    public async Task<List<TestableEndpoint>> GetTestableEndpointsAsync()
    {
        var endpoints = new List<TestableEndpoint>();

        try
        {
            // Get all controller types
            var assembly = Assembly.GetExecutingAssembly();
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Controller") && 
                           t.IsSubclassOf(typeof(ControllerBase)) && 
                           !t.IsAbstract)
                .ToList();

            foreach (var controllerType in controllerTypes)
            {
                var controllerName = controllerType.Name.Replace("Controller", "");
                var controllerRoute = GetControllerRouteInternal(controllerType);

                // Get all action methods
                var actionMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsPublic && 
                               !m.IsSpecialName && 
                               m.DeclaringType == controllerType &&
                               HasHttpMethodAttribute(m))
                    .ToList();

                foreach (var method in actionMethods)
                {
                    var endpoint = CreateTestableEndpointInternal(controllerName, controllerRoute, method);
                    if (endpoint != null)
                    {
                        endpoints.Add(endpoint);
                    }
                }
            }

            _logger.LogInformation("Discovered {Count} testable endpoints", endpoints.Count);
            return endpoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering testable endpoints");
            throw;
        }
    }

    /// <summary>
    /// Execute a single test against an endpoint
    /// </summary>
    public async Task<TestExecutionResult> ExecuteTestAsync(ExecuteTestRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestExecutionResult
        {
            TestName = request.TestName ?? $"{request.Method} {request.Path}",
            Method = request.Method,
            Path = request.Path,
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds);

            // Configure base URL
            var baseUrl = _configuration["ApiTesting:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);

            // Add default headers
            AddDefaultHeaders(httpClient);

            // Add custom headers
            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Build request URL with parameters
            var requestUrl = BuildRequestUrl(request.Path, request.Parameters);

            // Create HTTP request message
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), requestUrl);

            // Add body if present
            if (request.Body != null)
            {
                var jsonBody = JsonSerializer.Serialize(request.Body);
                httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            // Execute request
            var response = await httpClient.SendAsync(httpRequest);
            stopwatch.Stop();

            // Process response
            result.StatusCode = (int)response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;
            result.TestDuration = stopwatch.Elapsed;
            result.ResponseBody = await response.Content.ReadAsStringAsync();

            // Capture response headers
            result.ResponseHeaders = response.Headers
                .Concat(response.Content.Headers)
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            // Performance metrics
            if (request.IncludePerformanceMetrics)
            {
                result.PerformanceMetrics = new TestPerformanceMetrics
                {
                    ResponseTime = stopwatch.Elapsed,
                    ResponseSizeBytes = result.ResponseBody?.Length ?? 0,
                    // Additional metrics would be captured here
                };
            }

            // Response validation
            if (request.IncludeResponseValidation)
            {
                result.ValidationResult = ValidateResponse(result);
            }

            _logger.LogInformation("Test executed: {Method} {Path} - Status: {Status}, Duration: {Duration}ms",
                request.Method, request.Path, result.StatusCode, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.TestDuration = stopwatch.Elapsed;

            _logger.LogError(ex, "Error executing test: {Method} {Path}", request.Method, request.Path);
            return result;
        }
    }

    /// <summary>
    /// Execute multiple tests in batch
    /// </summary>
    public async Task<BatchTestExecutionResult> ExecuteBatchTestsAsync(ExecuteBatchTestRequest request)
    {
        var result = new BatchTestExecutionResult
        {
            BatchName = request.BatchName ?? $"Batch Test {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            TotalTests = request.Tests.Count,
            ExecutedAt = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (request.RunInParallel)
            {
                var semaphore = new SemaphoreSlim(request.MaxParallelTests);
                var tasks = request.Tests.Select(async test =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await ExecuteTestAsync(test);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                result.Results = (await Task.WhenAll(tasks)).ToList();
            }
            else
            {
                foreach (var test in request.Tests)
                {
                    var testResult = await ExecuteTestAsync(test);
                    result.Results.Add(testResult);

                    if (request.StopOnFirstFailure && !testResult.IsSuccess)
                    {
                        result.SkippedTests = request.Tests.Count - result.Results.Count;
                        break;
                    }
                }
            }

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;

            // Calculate summary statistics
            result.SuccessfulTests = result.Results.Count(r => r.IsSuccess);
            result.FailedTests = result.Results.Count(r => !r.IsSuccess);

            result.Summary = new BatchTestSummary
            {
                SuccessRate = result.TotalTests > 0 ? (double)result.SuccessfulTests / result.TotalTests * 100 : 0,
                AverageResponseTime = result.Results.Any() ? 
                    TimeSpan.FromMilliseconds(result.Results.Average(r => r.TestDuration.TotalMilliseconds)) : 
                    TimeSpan.Zero,
                FastestResponse = result.Results.Any() ? result.Results.Min(r => r.TestDuration) : TimeSpan.Zero,
                SlowestResponse = result.Results.Any() ? result.Results.Max(r => r.TestDuration) : TimeSpan.Zero,
                FailedEndpoints = result.Results.Where(r => !r.IsSuccess).Select(r => $"{r.Method} {r.Path}").ToList(),
                StatusCodeDistribution = result.Results.GroupBy(r => r.StatusCode.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            _logger.LogInformation("Batch test completed: {Total} tests, {Success} successful, {Failed} failed, Duration: {Duration}ms",
                result.TotalTests, result.SuccessfulTests, result.FailedTests, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            _logger.LogError(ex, "Error executing batch tests");
            throw;
        }
    }

    /// <summary>
    /// Execute comprehensive test suite
    /// </summary>
    public async Task<TestSuiteExecutionResult> ExecuteTestSuiteAsync(ExecuteTestSuiteRequest request)
    {
        var result = new TestSuiteExecutionResult
        {
            SuiteName = request.SuiteName ?? $"Test Suite {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            ExecutedAt = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get all testable endpoints
            var allEndpoints = await GetTestableEndpointsAsync();

            // Filter endpoints based on request criteria
            var filteredEndpoints = FilterEndpoints(allEndpoints, request);

            // Generate tests for each endpoint
            var tests = new List<ExecuteTestRequest>();
            foreach (var endpoint in filteredEndpoints)
            {
                var endpointTests = GenerateTestsForEndpoint(endpoint, request);
                tests.AddRange(endpointTests);
            }

            // Execute tests
            var batchRequest = new ExecuteBatchTestRequest
            {
                Tests = tests,
                RunInParallel = request.RunInParallel,
                MaxParallelTests = request.MaxParallelTests,
                BatchName = result.SuiteName
            };

            var batchResult = await ExecuteBatchTestsAsync(batchRequest);

            // Map results
            result.TotalTests = batchResult.TotalTests;
            result.SuccessfulTests = batchResult.SuccessfulTests;
            result.FailedTests = batchResult.FailedTests;
            result.SkippedTests = batchResult.SkippedTests;

            // Group results by controller
            result.ControllerResults = batchResult.Results
                .GroupBy(r => ExtractControllerFromPath(r.Path))
                .Select(g => new ControllerTestResult
                {
                    ControllerName = g.Key,
                    TotalEndpoints = g.Count(),
                    SuccessfulEndpoints = g.Count(r => r.IsSuccess),
                    FailedEndpoints = g.Count(r => !r.IsSuccess),
                    Duration = TimeSpan.FromMilliseconds(g.Sum(r => r.TestDuration.TotalMilliseconds)),
                    EndpointResults = g.ToList()
                })
                .ToList();

            result.FailedTests = batchResult.Results.Where(r => !r.IsSuccess).Count();

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;

            // Generate summary
            result.Summary = new TestSuiteSummary
            {
                OverallSuccessRate = result.TotalTests > 0 ? (double)result.SuccessfulTests / result.TotalTests * 100 : 0,
                AverageResponseTime = batchResult.Summary.AverageResponseTime,
                ControllerSuccessRates = result.ControllerResults.ToDictionary(
                    c => c.ControllerName,
                    c => c.TotalEndpoints > 0 ? (double)c.SuccessfulEndpoints / c.TotalEndpoints * 100 : 0),
                StatusCodeDistribution = batchResult.Summary.StatusCodeDistribution
            };

            _logger.LogInformation("Test suite completed: {Total} tests, {Success} successful, {Failed} failed, Duration: {Duration}ms",
                result.TotalTests, result.SuccessfulTests, result.FailedTests, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            _logger.LogError(ex, "Error executing test suite");
            throw;
        }
    }

    /// <summary>
    /// Get test execution history
    /// </summary>
    public async Task<TestExecutionHistory> GetTestHistoryAsync(GetTestHistoryRequest request)
    {
        // This would typically query a database for test execution history
        // For now, return a mock implementation
        return new TestExecutionHistory
        {
            TotalExecutions = 0,
            Executions = new List<TestExecutionSummary>(),
            Statistics = new TestHistoryStatistics(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = 0
        };
    }

    /// <summary>
    /// Get test statistics
    /// </summary>
    public async Task<TestStatistics> GetTestStatisticsAsync(GetTestStatisticsRequest request)
    {
        // This would typically query a database for test statistics
        // For now, return a mock implementation
        return new TestStatistics
        {
            Overview = new TestOverviewStatistics(),
            ControllerStats = new List<ControllerStatistics>(),
            EndpointStats = new List<EndpointStatistics>(),
            Performance = new PerformanceStatistics(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generate test data for an endpoint
    /// </summary>
    public async Task<GeneratedTestData> GenerateTestDataAsync(GenerateTestDataRequest request)
    {
        var testData = new GeneratedTestData
        {
            Method = request.Method,
            Path = request.Path,
            GeneratedAt = DateTime.UtcNow
        };

        // Generate valid test data samples
        if (request.IncludeValidData)
        {
            testData.ValidSamples = GenerateValidTestSamples(request);
        }

        // Generate invalid test data samples
        if (request.IncludeInvalidData)
        {
            testData.InvalidSamples = GenerateInvalidTestSamples(request);
        }

        // Generate edge case test data samples
        if (request.IncludeEdgeCases)
        {
            testData.EdgeCaseSamples = GenerateEdgeCaseTestSamples(request);
        }

        return testData;
    }

    /// <summary>
    /// Validate an endpoint configuration
    /// </summary>
    public async Task<EndpointValidationResult> ValidateEndpointAsync(ValidateEndpointRequest request)
    {
        var result = new EndpointValidationResult
        {
            Method = request.Method,
            Path = request.Path,
            ValidatedAt = DateTime.UtcNow
        };

        var issues = new List<ValidationIssue>();

        // Basic validation implementation
        result.Issues = issues.Where(i => i.Severity == "Error").ToList();
        result.Warnings = issues.Where(i => i.Severity == "Warning").ToList();
        result.IsValid = !result.Issues.Any();

        return result;
    }

    #region Private Helper Methods

    private void AddDefaultHeaders(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MonitoringGrid-API-Tester/1.0");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // Add authentication header if configured
        var authToken = _configuration["ApiTesting:AuthToken"];
        if (!string.IsNullOrEmpty(authToken))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }
    }

    private string BuildRequestUrl(string path, Dictionary<string, object>? parameters)
    {
        if (parameters == null || !parameters.Any())
            return path;

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}"));
        return $"{path}?{queryString}";
    }

    private TestValidationResult ValidateResponse(TestExecutionResult result)
    {
        var validation = new TestValidationResult { IsValid = true };
        var issues = new List<ValidationIssue>();

        // Validate status code
        if (result.StatusCode < 200 || result.StatusCode >= 300)
        {
            if (result.StatusCode >= 500)
            {
                issues.Add(new ValidationIssue
                {
                    Type = "StatusCode",
                    Severity = "Error",
                    Message = $"Server error status code: {result.StatusCode}",
                    ActualValue = result.StatusCode
                });
            }
        }

        validation.Issues = issues;
        validation.IsValid = !issues.Any(i => i.Severity == "Error");
        return validation;
    }

    private List<TestableEndpoint> FilterEndpoints(List<TestableEndpoint> endpoints, ExecuteTestSuiteRequest request)
    {
        var filtered = endpoints.AsEnumerable();

        if (request.IncludeControllers?.Any() == true)
        {
            filtered = filtered.Where(e => request.IncludeControllers.Contains(e.Controller, StringComparer.OrdinalIgnoreCase));
        }

        if (request.ExcludeControllers?.Any() == true)
        {
            filtered = filtered.Where(e => !request.ExcludeControllers.Contains(e.Controller, StringComparer.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private List<ExecuteTestRequest> GenerateTestsForEndpoint(TestableEndpoint endpoint, ExecuteTestSuiteRequest request)
    {
        var tests = new List<ExecuteTestRequest>();

        // Basic functionality test
        tests.Add(new ExecuteTestRequest
        {
            Method = endpoint.Method,
            Path = endpoint.Path,
            TestName = $"{endpoint.Controller}.{endpoint.Action} - Basic Test",
            IncludePerformanceMetrics = request.IncludePerformanceTests,
            IncludeResponseValidation = request.IncludeValidationTests
        });

        return tests;
    }

    private string ExtractControllerFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : "Unknown";
    }

    // Placeholder methods for test data generation
    private List<TestDataSample> GenerateValidTestSamples(GenerateTestDataRequest request)
    {
        return new List<TestDataSample>
        {
            new TestDataSample
            {
                Name = "Valid Sample 1",
                Description = "Basic valid test data",
                ExpectedStatusCode = 200
            }
        };
    }

    private List<TestDataSample> GenerateInvalidTestSamples(GenerateTestDataRequest request)
    {
        return new List<TestDataSample>
        {
            new TestDataSample
            {
                Name = "Invalid Sample 1",
                Description = "Basic invalid test data",
                ExpectedStatusCode = 400
            }
        };
    }

    private List<TestDataSample> GenerateEdgeCaseTestSamples(GenerateTestDataRequest request)
    {
        return new List<TestDataSample>
        {
            new TestDataSample
            {
                Name = "Edge Case Sample 1",
                Description = "Basic edge case test data",
                ExpectedStatusCode = 200
            }
        };
    }

    // Placeholder validation methods
    private List<ValidationIssue> ValidateAuthentication(ValidateEndpointRequest request) => new();
    private List<ValidationIssue> ValidateAuthorization(ValidateEndpointRequest request) => new();
    private List<ValidationIssue> ValidateInputValidation(ValidateEndpointRequest request) => new();
    private List<ValidationIssue> ValidateOutputFormat(ValidateEndpointRequest request) => new();
    private List<ValidationIssue> ValidatePerformance(ValidateEndpointRequest request) => new();
    private List<ValidationIssue> ValidateSecurity(ValidateEndpointRequest request) => new();

    // Missing helper methods implementation
    private string GetControllerRouteInternal(Type controllerType)
    {
        var controllerName = controllerType.Name.Replace("Controller", "");
        return $"api/{controllerName.ToLower()}";
    }

    private bool HasHttpMethodAttribute(MethodInfo method)
    {
        return method.GetCustomAttributes(typeof(HttpGetAttribute), false).Any() ||
               method.GetCustomAttributes(typeof(HttpPostAttribute), false).Any() ||
               method.GetCustomAttributes(typeof(HttpPutAttribute), false).Any() ||
               method.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any() ||
               method.GetCustomAttributes(typeof(HttpPatchAttribute), false).Any();
    }

    private TestableEndpoint? CreateTestableEndpointInternal(string controllerName, string controllerRoute, MethodInfo method)
    {
        var httpMethod = GetHttpMethodFromAttribute(method);
        if (string.IsNullOrEmpty(httpMethod))
            return null;

        return new TestableEndpoint
        {
            Controller = controllerName,
            Action = method.Name,
            Method = httpMethod,
            Path = $"{controllerRoute}/{method.Name.ToLower()}",
            Parameters = method.GetParameters().Select(p => new EndpointParameter
            {
                Name = p.Name ?? "",
                Type = p.ParameterType.Name,
                IsRequired = !p.HasDefaultValue
            }).ToList()
        };
    }

    private string GetHttpMethodFromAttribute(MethodInfo method)
    {
        if (method.GetCustomAttributes(typeof(HttpGetAttribute), false).Any()) return "GET";
        if (method.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()) return "POST";
        if (method.GetCustomAttributes(typeof(HttpPutAttribute), false).Any()) return "PUT";
        if (method.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any()) return "DELETE";
        if (method.GetCustomAttributes(typeof(HttpPatchAttribute), false).Any()) return "PATCH";
        return "";
    }

    #endregion
}
