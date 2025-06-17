using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Api.Models.Testing;
using MediatR;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Comprehensive API testing controller for automated endpoint testing and validation
/// </summary>
[ApiController]
[Route("api/testing")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class ApiTestingController : BaseApiController
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ApiTestingService _testingService;

    public ApiTestingController(
        IMediator mediator,
        ILogger<ApiTestingController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ApiTestingService testingService)
        : base(mediator, logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _testingService = testingService;
    }

    /// <summary>
    /// Get all available API endpoints for testing
    /// </summary>
    /// <returns>List of all testable endpoints</returns>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(List<TestableEndpoint>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TestableEndpoint>>> GetTestableEndpoints()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var endpoints = await _testingService.GetTestableEndpointsAsync();
            stopwatch.Stop();

            Logger.LogInformation("Retrieved {Count} testable endpoints in {Duration}ms", 
                endpoints.Count, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(endpoints, $"Retrieved {endpoints.Count} testable endpoints"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving testable endpoints: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve testable endpoints", "GET_ENDPOINTS_ERROR"));
        }
    }

    /// <summary>
    /// Execute a single endpoint test
    /// </summary>
    /// <param name="request">Test execution request</param>
    /// <returns>Test execution result</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(TestExecutionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestExecutionResult>> ExecuteTest([FromBody] ExecuteTestRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Executing test for endpoint {Method} {Path}", request.Method, request.Path);

            var result = await _testingService.ExecuteTestAsync(request);
            stopwatch.Stop();

            result.TestDuration = stopwatch.Elapsed;

            Logger.LogInformation("Executed test for {Method} {Path} - Status: {Status}, Duration: {Duration}ms", 
                request.Method, request.Path, result.StatusCode, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(result, $"Test executed - {result.StatusCode}"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error executing test for {Method} {Path}: {Message}", 
                request.Method, request.Path, ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to execute test", "EXECUTE_TEST_ERROR"));
        }
    }

    /// <summary>
    /// Execute multiple endpoint tests in sequence
    /// </summary>
    /// <param name="request">Batch test execution request</param>
    /// <returns>Batch test execution results</returns>
    [HttpPost("execute-batch")]
    [ProducesResponseType(typeof(BatchTestExecutionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchTestExecutionResult>> ExecuteBatchTests([FromBody] ExecuteBatchTestRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Executing batch test with {Count} tests", request.Tests.Count);

            var result = await _testingService.ExecuteBatchTestsAsync(request);
            stopwatch.Stop();

            result.TotalDuration = stopwatch.Elapsed;

            Logger.LogInformation("Executed batch test with {Total} tests - Success: {Success}, Failed: {Failed}, Duration: {Duration}ms", 
                result.TotalTests, result.SuccessfulTests, result.FailedTests, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(result, $"Batch test completed - {result.SuccessfulTests}/{result.TotalTests} successful"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error executing batch tests: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to execute batch tests", "EXECUTE_BATCH_TEST_ERROR"));
        }
    }

    /// <summary>
    /// Execute comprehensive test suite for all endpoints
    /// </summary>
    /// <param name="request">Test suite execution request</param>
    /// <returns>Comprehensive test suite results</returns>
    [HttpPost("execute-suite")]
    [ProducesResponseType(typeof(TestSuiteExecutionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestSuiteExecutionResult>> ExecuteTestSuite([FromBody] ExecuteTestSuiteRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Executing comprehensive test suite - IncludePerformance: {Performance}, IncludeSecurity: {Security}", 
                request.IncludePerformanceTests, request.IncludeSecurityTests);

            var result = await _testingService.ExecuteTestSuiteAsync(request);
            stopwatch.Stop();

            result.TotalDuration = stopwatch.Elapsed;

            Logger.LogInformation("Executed test suite - Total: {Total}, Success: {Success}, Failed: {Failed}, Duration: {Duration}ms", 
                result.TotalTests, result.SuccessfulTests, result.FailedTests, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(result, $"Test suite completed - {result.SuccessfulTests}/{result.TotalTests} successful"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error executing test suite: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to execute test suite", "EXECUTE_TEST_SUITE_ERROR"));
        }
    }

    /// <summary>
    /// Get test execution history
    /// </summary>
    /// <param name="request">Test history request</param>
    /// <returns>Test execution history</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(TestExecutionHistory), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestExecutionHistory>> GetTestHistory([FromQuery] GetTestHistoryRequest? request = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            request ??= new GetTestHistoryRequest();
            
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Getting test history - Days: {Days}, IncludeDetails: {Details}", 
                request.Days, request.IncludeDetails);

            var history = await _testingService.GetTestHistoryAsync(request);
            stopwatch.Stop();

            Logger.LogInformation("Retrieved test history with {Count} executions in {Duration}ms", 
                history.Executions.Count, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(history, $"Retrieved {history.Executions.Count} test executions"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving test history: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve test history", "GET_TEST_HISTORY_ERROR"));
        }
    }

    /// <summary>
    /// Get test statistics and analytics
    /// </summary>
    /// <param name="request">Test statistics request</param>
    /// <returns>Test statistics and analytics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TestStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestStatistics>> GetTestStatistics([FromQuery] GetTestStatisticsRequest? request = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            request ??= new GetTestStatisticsRequest();
            
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Getting test statistics - Days: {Days}", request.Days);

            var statistics = await _testingService.GetTestStatisticsAsync(request);
            stopwatch.Stop();

            Logger.LogInformation("Retrieved test statistics in {Duration}ms", stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(statistics, "Test statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving test statistics: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve test statistics", "GET_TEST_STATISTICS_ERROR"));
        }
    }

    /// <summary>
    /// Generate test data for endpoints
    /// </summary>
    /// <param name="request">Test data generation request</param>
    /// <returns>Generated test data</returns>
    [HttpPost("generate-test-data")]
    [ProducesResponseType(typeof(GeneratedTestData), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeneratedTestData>> GenerateTestData([FromBody] GenerateTestDataRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Generating test data for endpoint {Method} {Path}", request.Method, request.Path);

            var testData = await _testingService.GenerateTestDataAsync(request);
            stopwatch.Stop();

            Logger.LogInformation("Generated test data for {Method} {Path} in {Duration}ms", 
                request.Method, request.Path, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(testData, "Test data generated successfully"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error generating test data for {Method} {Path}: {Message}", 
                request.Method, request.Path, ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to generate test data", "GENERATE_TEST_DATA_ERROR"));
        }
    }

    /// <summary>
    /// Validate API endpoint configuration
    /// </summary>
    /// <param name="request">Endpoint validation request</param>
    /// <returns>Endpoint validation result</returns>
    [HttpPost("validate-endpoint")]
    [ProducesResponseType(typeof(EndpointValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EndpointValidationResult>> ValidateEndpoint([FromBody] ValidateEndpointRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Validating endpoint {Method} {Path}", request.Method, request.Path);

            var result = await _testingService.ValidateEndpointAsync(request);
            stopwatch.Stop();

            Logger.LogInformation("Validated endpoint {Method} {Path} - Valid: {Valid}, Issues: {Issues}, Duration: {Duration}ms", 
                request.Method, request.Path, result.IsValid, result.Issues.Count, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(result, $"Endpoint validation completed - {(result.IsValid ? "Valid" : $"{result.Issues.Count} issues found")}"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error validating endpoint {Method} {Path}: {Message}", 
                request.Method, request.Path, ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to validate endpoint", "VALIDATE_ENDPOINT_ERROR"));
        }
    }
}
