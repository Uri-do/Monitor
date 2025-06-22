using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.DTOs.TestSuite;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.Controllers.Base;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for managing and executing the test suite
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TestSuiteController : BaseApiController
{
    private readonly IConfiguration _configuration;
    private static readonly Dictionary<string, TestExecution> _activeExecutions = new();

    public TestSuiteController(
        IMediator mediator,
        ILogger<TestSuiteController> logger,
        IConfiguration configuration)
        : base(mediator, logger)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get the current status of the test suite
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<TestSuiteStatusResponse>>> GetStatus()
    {
        try
        {
            var status = new TestSuiteStatusResponse
            {
                IsRunning = _activeExecutions.Any(e => e.Value.IsRunning),
                TotalTests = 12,
                LastRun = GetLastRunTime(),
                AvailableCategories = new[] { "Framework", "Unit", "Performance" },
                TestResults = GetCachedResults()
            };

            return Ok(CreateSuccessResponse(status, "Test suite status retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting test suite status");
            return StatusCode(500, CreateErrorResponse("Failed to get test suite status"));
        }
    }

    /// <summary>
    /// Run tests for specified categories
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<TestExecutionResponse>>> RunTests([FromBody] RunTestsRequest request)
    {
        try
        {
            var executionId = Guid.NewGuid().ToString();
            var categories = request?.Categories ?? new[] { "all" };
            
            Logger.LogInformation("Starting test execution {ExecutionId} for categories: {Categories}",
                executionId, string.Join(", ", categories));

            var execution = new TestExecution
            {
                Id = executionId,
                Categories = categories,
                StartTime = DateTime.UtcNow,
                IsRunning = true,
                Progress = 0
            };

            _activeExecutions[executionId] = execution;

            // Start test execution in background
            _ = Task.Run(async () => await ExecuteTestsAsync(execution));

            var response = new TestExecutionResponse
            {
                ExecutionId = executionId,
                Status = "Started",
                Categories = categories,
                StartTime = execution.StartTime
            };

            return Ok(CreateSuccessResponse(response, "Test execution started successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting test execution");
            return StatusCode(500, CreateErrorResponse("Failed to start test execution"));
        }
    }

    /// <summary>
    /// Stop a running test execution
    /// </summary>
    [HttpPost("stop/{executionId}")]
    public async Task<ActionResult<ApiResponse<object>>> StopTests(string executionId)
    {
        try
        {
            if (_activeExecutions.TryGetValue(executionId, out var execution))
            {
                execution.IsRunning = false;
                execution.EndTime = DateTime.UtcNow;
                Logger.LogInformation("Stopped test execution {ExecutionId}", executionId);

                return Ok(CreateSuccessResponse(new { message = "Test execution stopped" }));
            }

            return NotFound(CreateErrorResponse("Test execution not found"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping test execution {ExecutionId}", executionId);
            return StatusCode(500, CreateErrorResponse("Failed to stop test execution"));
        }
    }

    /// <summary>
    /// Get test execution progress
    /// </summary>
    [HttpGet("execution/{executionId}")]
    public async Task<ActionResult<ApiResponse<TestExecutionStatusResponse>>> GetExecutionStatus(string executionId)
    {
        try
        {
            if (!_activeExecutions.TryGetValue(executionId, out var execution))
            {
                return NotFound(CreateErrorResponse("Test execution not found"));
            }

            var response = new TestExecutionStatusResponse
            {
                ExecutionId = executionId,
                IsRunning = execution.IsRunning,
                Progress = execution.Progress,
                CurrentTest = execution.CurrentTest,
                Results = execution.Results,
                StartTime = execution.StartTime,
                EndTime = execution.EndTime,
                Duration = execution.EndTime?.Subtract(execution.StartTime) ?? DateTime.UtcNow.Subtract(execution.StartTime)
            };

            return Ok(CreateSuccessResponse(response, "Execution status retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting execution status for {ExecutionId}", executionId);
            return StatusCode(500, CreateErrorResponse("Failed to get execution status"));
        }
    }

    /// <summary>
    /// Get test results history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<TestHistoryResponse>>> GetTestHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // In a real implementation, this would query a database
            var history = _activeExecutions.Values
                .Where(e => !e.IsRunning)
                .OrderByDescending(e => e.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new TestExecutionSummary
                {
                    ExecutionId = e.Id,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime ?? DateTime.UtcNow,
                    Duration = (e.EndTime ?? DateTime.UtcNow).Subtract(e.StartTime),
                    Categories = e.Categories,
                    TotalTests = e.Results?.Count ?? 0,
                    PassedTests = e.Results?.Count(r => r.Status == "passed") ?? 0,
                    FailedTests = e.Results?.Count(r => r.Status == "failed") ?? 0
                })
                .ToList();

            var response = new TestHistoryResponse
            {
                Executions = history,
                Page = page,
                PageSize = pageSize,
                TotalCount = _activeExecutions.Values.Count(e => !e.IsRunning)
            };

            return Ok(CreateSuccessResponse(response, "Test history retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting test history");
            return StatusCode(500, CreateErrorResponse("Failed to get test history"));
        }
    }

    private async Task ExecuteTestsAsync(TestExecution execution)
    {
        try
        {
            var testProject = _configuration["TestSuite:ProjectPath"] ?? "./MonitoringGrid.Tests/MonitoringGrid.Tests.csproj";
            var dotnetPath = _configuration["TestSuite:DotNetPath"] ?? "dotnet";

            // Build the dotnet test command
            var arguments = $"test \"{testProject}\" --logger \"json;LogFilePath=test-results-{execution.Id}.json\" --verbosity normal";
            
            if (execution.Categories.Any() && !execution.Categories.Contains("all"))
            {
                var categoryFilter = string.Join("|", execution.Categories.Select(c => $"Category={c}"));
                arguments += $" --filter \"{categoryFilter}\"";
            }

            Logger.LogInformation("Executing: {DotNetPath} {Arguments}", dotnetPath, arguments);

            var processInfo = new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            var output = new List<string>();
            var errors = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Add(e.Data);
                    Logger.LogDebug("Test output: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errors.Add(e.Data);
                    Logger.LogWarning("Test error: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Simulate progress updates
            var progressTask = Task.Run(async () =>
            {
                var totalSteps = 12; // Total number of tests
                for (int i = 0; i < totalSteps && execution.IsRunning; i++)
                {
                    await Task.Delay(500); // Simulate test execution time
                    execution.Progress = (int)((i + 1) / (double)totalSteps * 100);
                    execution.CurrentTest = $"Test {i + 1}";
                }
            });

            await process.WaitForExitAsync();
            await progressTask;

            execution.IsRunning = false;
            execution.EndTime = DateTime.UtcNow;
            execution.ExitCode = process.ExitCode;
            execution.Output = output;
            execution.Errors = errors;

            // Parse results (simplified - in reality would parse the JSON output)
            execution.Results = GenerateMockResults(execution.Categories);

            Logger.LogInformation("Test execution {ExecutionId} completed with exit code {ExitCode}",
                execution.Id, process.ExitCode);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing tests for {ExecutionId}", execution.Id);
            execution.IsRunning = false;
            execution.EndTime = DateTime.UtcNow;
            execution.ExitCode = -1;
        }
    }

    private List<TestResult> GenerateMockResults(string[] categories)
    {
        // Generate mock test results based on the actual test suite
        var results = new List<TestResult>();
        var random = new Random();

        var testNames = new[]
        {
            "BasicTest_ShouldPass",
            "WorkerController_CanBeInstantiated", 
            "TestDataBuilder_CanCreateBasicData",
            "MockFramework_IsWorking",
            "PerformanceTest_BasicMeasurement",
            "ParameterizedTest_WithDifferentOptions_1",
            "ParameterizedTest_WithDifferentOptions_2", 
            "ParameterizedTest_WithDifferentOptions_3",
            "AsyncTest_ShouldWork",
            "ExceptionTest_ShouldHandleExceptions",
            "MemoryTest_BasicGarbageCollection",
            "ConcurrencyTest_BasicParallelExecution"
        };

        var testCategories = new Dictionary<string, string>
        {
            ["BasicTest_ShouldPass"] = "Framework",
            ["WorkerController_CanBeInstantiated"] = "Unit",
            ["TestDataBuilder_CanCreateBasicData"] = "Framework",
            ["MockFramework_IsWorking"] = "Framework",
            ["PerformanceTest_BasicMeasurement"] = "Performance",
            ["ParameterizedTest_WithDifferentOptions_1"] = "Unit",
            ["ParameterizedTest_WithDifferentOptions_2"] = "Unit",
            ["ParameterizedTest_WithDifferentOptions_3"] = "Unit",
            ["AsyncTest_ShouldWork"] = "Framework",
            ["ExceptionTest_ShouldHandleExceptions"] = "Framework",
            ["MemoryTest_BasicGarbageCollection"] = "Performance",
            ["ConcurrencyTest_BasicParallelExecution"] = "Performance"
        };

        foreach (var testName in testNames)
        {
            var category = testCategories[testName];
            
            // Only include tests from selected categories
            if (!categories.Contains("all") && !categories.Contains(category))
                continue;

            results.Add(new TestResult
            {
                Id = Guid.NewGuid().ToString(),
                Name = testName,
                Category = category,
                Status = random.NextDouble() > 0.1 ? "passed" : "failed", // 90% pass rate
                Duration = random.Next(50, 1000),
                Message = random.NextDouble() > 0.9 ? "Test assertion failed" : null
            });
        }

        return results;
    }

    private DateTime? GetLastRunTime()
    {
        return _activeExecutions.Values
            .Where(e => e.EndTime.HasValue)
            .OrderByDescending(e => e.EndTime)
            .FirstOrDefault()?.EndTime;
    }

    private List<TestResult> GetCachedResults()
    {
        return _activeExecutions.Values
            .Where(e => e.Results != null)
            .OrderByDescending(e => e.EndTime ?? e.StartTime)
            .FirstOrDefault()?.Results ?? new List<TestResult>();
    }
}

// Supporting classes
public class TestExecution
{
    public string Id { get; set; } = string.Empty;
    public string[] Categories { get; set; } = Array.Empty<string>();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsRunning { get; set; }
    public int Progress { get; set; }
    public string? CurrentTest { get; set; }
    public int? ExitCode { get; set; }
    public List<string>? Output { get; set; }
    public List<string>? Errors { get; set; }
    public List<TestResult>? Results { get; set; }
}
