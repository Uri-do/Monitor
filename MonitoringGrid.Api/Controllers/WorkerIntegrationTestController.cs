using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Worker;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for Worker Integration Testing - tests actual worker execution with real-time monitoring
/// </summary>
[ApiController]
[Route("api/worker-integration-test")]
public class WorkerIntegrationTestController : BaseApiController
{
    private readonly ILogger<WorkerIntegrationTestController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly IRepository<Indicator> _indicatorRepository;
    private readonly IWorkerProcessManager _workerProcessManager;
    private static readonly Dictionary<string, WorkerTestExecution> _activeTests = new();
    private static readonly object _testLock = new();

    public WorkerIntegrationTestController(
        IMediator mediator,
        ILogger<WorkerIntegrationTestController> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHubContext<MonitoringHub> hubContext,
        IRepository<Indicator> indicatorRepository,
        IWorkerProcessManager workerProcessManager) : base(mediator, logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _hubContext = hubContext;
        _indicatorRepository = indicatorRepository;
        _workerProcessManager = workerProcessManager;
    }

    /// <summary>
    /// Get the status of worker integration tests
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<WorkerIntegrationTestStatus>>> GetStatus()
    {
        try
        {
            var activeTests = _activeTests.Values.ToList();
            var availableIndicators = await _indicatorRepository.GetAllAsync();
            
            var status = new WorkerIntegrationTestStatus
            {
                IsRunning = activeTests.Any(t => t.IsRunning),
                ActiveTests = activeTests.Count,
                TotalIndicators = availableIndicators.Count(),
                AvailableIndicators = availableIndicators.Where(i => i.IsActive).Take(10).Select(i => new IndicatorSummary
                {
                    Id = i.IndicatorID,
                    Name = i.IndicatorName,
                    Description = i.IndicatorDesc,
                    Priority = i.Priority.ToString(),
                    LastRun = i.LastRun
                }).ToList(),
                RecentExecutions = activeTests.OrderByDescending(t => t.StartTime).Take(5).ToList()
            };

            return Ok(CreateSuccessResponse(status, "Worker integration test status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker integration test status");
            return StatusCode(500, CreateErrorResponse($"Error getting status: {ex.Message}", "STATUS_ERROR"));
        }
    }

    /// <summary>
    /// Start a worker integration test
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<WorkerTestExecutionResponse>>> StartTest([FromBody] StartWorkerTestRequest request)
    {
        try
        {
            var testId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting worker integration test {TestId} with type: {TestType}", testId, request.TestType);

            var execution = new WorkerTestExecution
            {
                Id = testId,
                TestType = request.TestType,
                IndicatorIds = request.IndicatorIds ?? new List<long>(),
                StartTime = DateTime.UtcNow,
                IsRunning = true,
                Status = "Starting",
                Progress = 0
            };

            lock (_testLock)
            {
                _activeTests[testId] = execution;
            }

            // Start the test execution in background
            _ = Task.Run(async () => await ExecuteWorkerTestAsync(execution));

            // Notify clients via SignalR
            await _hubContext.Clients.Groups("WorkerTests", "Dashboard").SendAsync("WorkerTestStarted", new
            {
                TestId = testId,
                TestType = request.TestType,
                StartTime = execution.StartTime
            });

            var response = new WorkerTestExecutionResponse
            {
                TestId = testId,
                Status = "Started",
                Message = $"Worker integration test {testId} started successfully",
                StartTime = execution.StartTime
            };

            return Ok(CreateSuccessResponse(response, "Worker integration test started successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting worker integration test");
            return StatusCode(500, CreateErrorResponse($"Error starting test: {ex.Message}", "TEST_START_ERROR"));
        }
    }

    /// <summary>
    /// Stop a running worker integration test
    /// </summary>
    [HttpPost("stop/{testId}")]
    public async Task<ActionResult<ApiResponse<WorkerTestExecutionResponse>>> StopTest(string testId)
    {
        try
        {
            lock (_testLock)
            {
                if (!_activeTests.TryGetValue(testId, out var execution))
                {
                    return NotFound(CreateErrorResponse($"Test {testId} not found", "TEST_NOT_FOUND"));
                }

                execution.CancellationTokenSource?.Cancel();
                execution.IsRunning = false;
                execution.Status = "Stopped";
                execution.EndTime = DateTime.UtcNow;
            }

            await _hubContext.Clients.All.SendAsync("WorkerTestStopped", new
            {
                TestId = testId,
                StoppedAt = DateTime.UtcNow
            });

            return Ok(CreateSuccessResponse(new WorkerTestExecutionResponse
            {
                TestId = testId,
                Status = "Stopped",
                Message = "Test execution stopped successfully"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping test {TestId}", testId);
            return StatusCode(500, CreateErrorResponse($"Failed to stop test: {ex.Message}", "TEST_STOP_ERROR"));
        }
    }

    /// <summary>
    /// Stop all running worker integration tests
    /// </summary>
    [HttpPost("stop")]
    public async Task<ActionResult<ApiResponse<WorkerTestExecutionResponse>>> StopAllTests()
    {
        try
        {
            var stoppedTests = new List<string>();

            lock (_testLock)
            {
                foreach (var kvp in _activeTests.ToList())
                {
                    var testId = kvp.Key;
                    var execution = kvp.Value;

                    execution.CancellationTokenSource?.Cancel();
                    execution.IsRunning = false;
                    execution.Status = "Stopped";
                    execution.EndTime = DateTime.UtcNow;
                    stoppedTests.Add(testId);

                    _ = Task.Run(async () =>
                    {
                        await _hubContext.Clients.All.SendAsync("WorkerTestStopped", new
                        {
                            TestId = testId,
                            StoppedAt = DateTime.UtcNow
                        });
                    });
                }
            }

            return Ok(CreateSuccessResponse(new WorkerTestExecutionResponse
            {
                TestId = "ALL",
                Status = "Stopped",
                Message = $"Stopped {stoppedTests.Count} test executions"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping all tests");
            return StatusCode(500, CreateErrorResponse($"Failed to stop tests: {ex.Message}", "TEST_STOP_ERROR"));
        }
    }

    /// <summary>
    /// Get results from recent test executions
    /// </summary>
    [HttpGet("results")]
    public ActionResult<ApiResponse<List<WorkerTestExecution>>> GetTestResults()
    {
        try
        {
            List<WorkerTestExecution> recentResults;
            lock (_testLock)
            {
                recentResults = _activeTests.Values
                    .Where(e => e.Results != null)
                    .OrderByDescending(e => e.StartTime)
                    .Take(10)
                    .ToList();
            }

            return Ok(CreateSuccessResponse(recentResults));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test results");
            return StatusCode(500, CreateErrorResponse($"Failed to get test results: {ex.Message}", "RESULTS_ERROR"));
        }
    }

    /// <summary>
    /// Get the status of a specific test execution
    /// </summary>
    [HttpGet("execution/{testId}")]
    public ActionResult<ApiResponse<WorkerTestExecution>> GetTestExecution(string testId)
    {
        try
        {
            lock (_testLock)
            {
                if (!_activeTests.TryGetValue(testId, out var execution))
                {
                    return NotFound(CreateErrorResponse($"Test {testId} not found", "TEST_NOT_FOUND"));
                }

                return Ok(CreateSuccessResponse(execution, "Test execution status retrieved successfully"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test execution {TestId}", testId);
            return StatusCode(500, CreateErrorResponse($"Error getting test execution: {ex.Message}", "EXECUTION_ERROR"));
        }
    }

    /// <summary>
    /// Execute the worker integration test
    /// </summary>
    private async Task ExecuteWorkerTestAsync(WorkerTestExecution execution)
    {
        var cancellationToken = execution.CancellationTokenSource.Token;
        
        try
        {
            _logger.LogInformation("Executing worker integration test {TestId} of type {TestType}", 
                execution.Id, execution.TestType);

            await UpdateTestStatus(execution, "Initializing", 5);

            switch (execution.TestType.ToLower())
            {
                case "indicator-execution":
                    await ExecuteIndicatorTest(execution, cancellationToken);
                    break;
                case "worker-lifecycle":
                    await ExecuteWorkerLifecycleTest(execution, cancellationToken);
                    break;
                case "real-time-monitoring":
                    await ExecuteRealTimeMonitoringTest(execution, cancellationToken);
                    break;
                case "stress-test":
                    await ExecuteStressTest(execution, cancellationToken);
                    break;
                case "worker-process-management":
                    await ExecuteWorkerProcessManagementTest(execution, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unknown test type: {execution.TestType}");
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await UpdateTestStatus(execution, "Completed", 100);
                execution.IsRunning = false;
                execution.EndTime = DateTime.UtcNow;
                execution.Success = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker integration test {TestId} was cancelled", execution.Id);
            await UpdateTestStatus(execution, "Cancelled", execution.Progress);
            execution.IsRunning = false;
            execution.EndTime = DateTime.UtcNow;
            execution.Success = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing worker integration test {TestId}", execution.Id);
            await UpdateTestStatus(execution, $"Failed: {ex.Message}", execution.Progress);
            execution.IsRunning = false;
            execution.EndTime = DateTime.UtcNow;
            execution.Success = false;
            execution.ErrorMessage = ex.Message;
        }
        finally
        {
            // Persist test results to database
            await PersistTestResultsAsync(execution);

            // Notify completion via SignalR
            await _hubContext.Clients.All.SendAsync("WorkerTestCompleted", new
            {
                TestId = execution.Id,
                Success = execution.Success,
                EndTime = execution.EndTime,
                Duration = execution.EndTime?.Subtract(execution.StartTime).TotalSeconds
            });
        }
    }

    /// <summary>
    /// Update test status and notify clients
    /// </summary>
    private async Task UpdateTestStatus(WorkerTestExecution execution, string status, int progress)
    {
        execution.Status = status;
        execution.Progress = progress;
        execution.LastUpdate = DateTime.UtcNow;

        // Notify clients via SignalR
        await _hubContext.Clients.Groups("WorkerTests", "Dashboard", $"WorkerTest_{execution.Id}").SendAsync("WorkerTestProgress", new
        {
            TestId = execution.Id,
            Status = status,
            Progress = progress,
            LastUpdate = execution.LastUpdate
        });

        _logger.LogInformation("Worker test {TestId}: {Status} ({Progress}%)",
            execution.Id, status, progress);
    }

    /// <summary>
    /// Execute indicator execution test - tests actual indicator processing
    /// </summary>
    private async Task ExecuteIndicatorTest(WorkerTestExecution execution, CancellationToken cancellationToken)
    {
        await UpdateTestStatus(execution, "Loading indicators", 10);

        using var scope = _serviceProvider.CreateScope();
        var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
        var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

        // Get indicators to test
        List<Indicator> indicatorList;
        if (execution.IndicatorIds.Any())
        {
            indicatorList = new List<Indicator>();
            foreach (var id in execution.IndicatorIds)
            {
                var result = await indicatorService.GetIndicatorByIdAsync(id);
                if (result.IsSuccess && result.Value != null)
                {
                    indicatorList.Add(result.Value);
                }
            }
        }
        else
        {
            var dueResult = await indicatorService.GetDueIndicatorsAsync();
            indicatorList = dueResult.IsSuccess ? dueResult.Value.Take(5).ToList() : new List<Indicator>();
        }
        await UpdateTestStatus(execution, $"Found {indicatorList.Count} indicators to test", 20);

        var results = new WorkerTestResults
        {
            IndicatorResults = new List<IndicatorExecutionResult>()
        };

        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        var process = Process.GetCurrentProcess();
        int processed = 0;

        foreach (var indicator in indicatorList)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var indicatorStopwatch = Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;

            await UpdateTestStatus(execution, $"Executing indicator: {indicator.IndicatorName}",
                20 + (processed * 60 / indicatorList.Count));

            try
            {
                _logger.LogInformation("Testing indicator {IndicatorId}: {Name}",
                    indicator.IndicatorID, indicator.IndicatorName);

                var executionResult = await indicatorExecutionService.ExecuteIndicatorAsync(indicator.IndicatorID, "WorkerIntegrationTest", true, cancellationToken);
                indicatorStopwatch.Stop();

                var result = new MonitoringGrid.Api.DTOs.Worker.IndicatorExecutionResult
                {
                    IndicatorId = indicator.IndicatorID,
                    IndicatorName = indicator.IndicatorName,
                    Success = executionResult.WasSuccessful,
                    ExecutionTimeMs = indicatorStopwatch.ElapsedMilliseconds,
                    ErrorMessage = executionResult.WasSuccessful ? null : executionResult.ErrorMessage,
                    RecordsProcessed = executionResult.RawData?.Count ?? 0,
                    AlertsTriggered = executionResult.AlertTriggered,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow
                };

                results.IndicatorResults.Add(result);

                if (executionResult.WasSuccessful)
                {
                    results.SuccessfulExecutions++;
                    _logger.LogInformation("✅ Indicator {IndicatorId} executed successfully in {Duration}ms",
                        indicator.IndicatorID, indicatorStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    results.FailedExecutions++;
                    _logger.LogWarning("❌ Indicator {IndicatorId} failed: {Error}",
                        indicator.IndicatorID, executionResult.ErrorMessage);
                }

                results.AlertsTriggered += executionResult.AlertTriggered ? 1 : 0;
            }
            catch (Exception ex)
            {
                indicatorStopwatch.Stop();
                results.FailedExecutions++;

                var result = new MonitoringGrid.Api.DTOs.Worker.IndicatorExecutionResult
                {
                    IndicatorId = indicator.IndicatorID,
                    IndicatorName = indicator.IndicatorName,
                    Success = false,
                    ExecutionTimeMs = indicatorStopwatch.ElapsedMilliseconds,
                    ErrorMessage = ex.Message,
                    RecordsProcessed = 0,
                    AlertsTriggered = false,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow
                };

                results.IndicatorResults.Add(result);
                _logger.LogError(ex, "❌ Error testing indicator {IndicatorId}", indicator.IndicatorID);
            }

            processed++;

            // Send real-time update via SignalR
            await _hubContext.Clients.All.SendAsync("IndicatorTestResult", new
            {
                TestId = execution.Id,
                IndicatorId = indicator.IndicatorID,
                IndicatorName = indicator.IndicatorName,
                Success = results.IndicatorResults.Last().Success,
                ExecutionTime = results.IndicatorResults.Last().ExecutionTimeMs,
                Progress = 20 + (processed * 60 / indicatorList.Count)
            }, cancellationToken);
        }

        stopwatch.Stop();

        // Calculate final metrics
        results.IndicatorsProcessed = processed;
        results.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        results.AverageExecutionTimeMs = results.IndicatorResults.Any()
            ? results.IndicatorResults.Average(r => r.ExecutionTimeMs)
            : 0;

        // Collect comprehensive performance metrics
        process.Refresh(); // Refresh the existing process instance
        var finalMemory = GC.GetTotalMemory(false);

        results.MemoryUsageBytes = finalMemory - initialMemory;

        // Calculate CPU usage (approximate)
        try
        {
            process.Refresh();
            var cpuUsage = process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / stopwatch.ElapsedMilliseconds * 100;
            results.CpuUsagePercent = Math.Min(cpuUsage, 100); // Cap at 100%
        }
        catch
        {
            results.CpuUsagePercent = 0; // Fallback if CPU measurement fails
        }

        // Add detailed performance metrics
        results.PerformanceMetrics["InitialMemoryBytes"] = initialMemory;
        results.PerformanceMetrics["FinalMemoryBytes"] = finalMemory;
        results.PerformanceMetrics["WorkingSetBytes"] = process.WorkingSet64;
        results.PerformanceMetrics["ProcessorCount"] = Environment.ProcessorCount;
        results.PerformanceMetrics["GCCollectionCount"] = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

        execution.Results = results;
        await UpdateTestStatus(execution, "Indicator test completed", 90);

        _logger.LogInformation("Indicator test completed: {Processed} processed, {Success} successful, {Failed} failed",
            results.IndicatorsProcessed, results.SuccessfulExecutions, results.FailedExecutions);
    }

    /// <summary>
    /// Execute worker lifecycle test - tests starting/stopping workers
    /// </summary>
    private async Task ExecuteWorkerLifecycleTest(WorkerTestExecution execution, CancellationToken cancellationToken)
    {
        await UpdateTestStatus(execution, "Testing worker lifecycle", 10);

        var results = new WorkerTestResults();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Test worker start
            await UpdateTestStatus(execution, "Starting worker process", 25);

            using var scope = _serviceProvider.CreateScope();
            var workerController = scope.ServiceProvider.GetService<WorkerController>();

            if (workerController != null)
            {
                var startRequest = new StartWorkerRequest { TimeoutMs = 30000 };
                var startResult = await workerController.StartWorker(startRequest, cancellationToken);

                results.SuccessfulExecutions++;
                await UpdateTestStatus(execution, "Worker started successfully", 50);

                // Wait a bit to let worker initialize
                await Task.Delay(5000, cancellationToken);

                // Test worker status
                await UpdateTestStatus(execution, "Checking worker status", 75);
                var statusResult = await workerController.GetStatus(new GetWorkerStatusRequest());

                if (statusResult != null)
                {
                    results.SuccessfulExecutions++;
                    await UpdateTestStatus(execution, "Worker status retrieved", 85);
                }

                // Test worker stop
                await UpdateTestStatus(execution, "Stopping worker process", 90);
                var stopRequest = new StopWorkerRequest { TimeoutMs = 15000 };
                var stopResult = workerController.StopWorker(stopRequest, cancellationToken);

                if (stopResult != null)
                {
                    results.SuccessfulExecutions++;
                    await UpdateTestStatus(execution, "Worker stopped successfully", 95);
                }
            }
            else
            {
                results.FailedExecutions++;
                throw new InvalidOperationException("WorkerController not available");
            }
        }
        catch (Exception ex)
        {
            results.FailedExecutions++;
            _logger.LogError(ex, "Worker lifecycle test failed");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            results.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            execution.Results = results;
        }
    }

    /// <summary>
    /// Execute real-time monitoring test - tests SignalR and live updates
    /// </summary>
    private async Task ExecuteRealTimeMonitoringTest(WorkerTestExecution execution, CancellationToken cancellationToken)
    {
        await UpdateTestStatus(execution, "Testing real-time monitoring", 10);

        var results = new WorkerTestResults();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Test SignalR connectivity
            await UpdateTestStatus(execution, "Testing SignalR connectivity", 25);

            for (int i = 0; i < 10; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await _hubContext.Clients.All.SendAsync("TestMessage", new
                {
                    TestId = execution.Id,
                    Message = $"Real-time test message {i + 1}",
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);

                await Task.Delay(1000, cancellationToken);
                await UpdateTestStatus(execution, $"Sent test message {i + 1}/10", 25 + (i * 5));
            }

            results.SuccessfulExecutions = 10;
            await UpdateTestStatus(execution, "Real-time monitoring test completed", 90);
        }
        catch (Exception ex)
        {
            results.FailedExecutions++;
            _logger.LogError(ex, "Real-time monitoring test failed");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            results.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            execution.Results = results;
        }
    }

    /// <summary>
    /// Execute stress test - tests system under load
    /// </summary>
    private async Task ExecuteStressTest(WorkerTestExecution execution, CancellationToken cancellationToken)
    {
        await UpdateTestStatus(execution, "Starting stress test", 10);

        var results = new WorkerTestResults();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get indicators first, then dispose scope to avoid disposal issues
            List<Indicator> indicators;
            using (var scope = _serviceProvider.CreateScope())
            {
                var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
                var dueResult = await indicatorService.GetDueIndicatorsAsync();
                indicators = dueResult.IsSuccess ? dueResult.Value.Take(3).ToList() : new List<Indicator>();
            }

            await UpdateTestStatus(execution, $"Running stress test with {indicators.Count} indicators", 20);

            var tasks = new List<Task>();
            var concurrentWorkers = 5; // Simulate multiple workers

            for (int worker = 0; worker < concurrentWorkers; worker++)
            {
                int workerId = worker;
                var task = Task.Run(async () =>
                {
                    for (int iteration = 0; iteration < 3; iteration++)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        foreach (var indicator in indicators)
                        {
                            try
                            {
                                // Create a new scope for each execution to avoid disposal issues
                                using var executionScope = _serviceProvider.CreateScope();
                                var indicatorExecutionService = executionScope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();
                                var result = await indicatorExecutionService.ExecuteIndicatorAsync(indicator.IndicatorID, "StressTest", true, cancellationToken);
                                lock (results)
                                {
                                    if (result.WasSuccessful)
                                    {
                                        results.SuccessfulExecutions++;
                                    }
                                    else
                                    {
                                        results.FailedExecutions++;
                                    }
                                }
                            }
                            catch
                            {
                                lock (results)
                                {
                                    results.FailedExecutions++;
                                }
                            }
                        }

                        await UpdateTestStatus(execution,
                            $"Worker {workerId + 1} completed iteration {iteration + 1}",
                            20 + ((worker * 3 + iteration + 1) * 60 / (concurrentWorkers * 3)));
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            await UpdateTestStatus(execution, "Stress test completed", 90);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stress test failed");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            results.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            results.IndicatorsProcessed = results.SuccessfulExecutions + results.FailedExecutions;
            execution.Results = results;
        }
    }

    /// <summary>
    /// Execute worker process management test - tests actual worker process creation, monitoring, and destruction
    /// </summary>
    private async Task ExecuteWorkerProcessManagementTest(WorkerTestExecution execution, CancellationToken cancellationToken)
    {
        await UpdateTestStatus(execution, "Starting worker process management test", 10);

        var results = new WorkerTestResults();
        var stopwatch = Stopwatch.StartNew();
        var workerProcesses = new List<WorkerProcessInfo>();

        try
        {
            // Test 1: Start multiple worker processes
            await UpdateTestStatus(execution, "Starting worker processes", 20);

            var processCount = 3; // Start 3 worker processes for testing
            for (int i = 0; i < processCount; i++)
            {
                var startRequest = new StartWorkerProcessRequest
                {
                    WorkerId = $"test-worker-{i + 1}",
                    TestType = "indicator-execution",
                    TestMode = true,
                    DurationSeconds = 60, // Run for 1 minute
                    IndicatorIds = execution.IndicatorIds.Take(2).Select(id => (int)id).ToList()
                };

                try
                {
                    var workerInfo = await _workerProcessManager.StartWorkerProcessAsync(startRequest, cancellationToken);
                    workerProcesses.Add(workerInfo);
                    results.SuccessfulExecutions++;

                    _logger.LogInformation("✅ Started worker process {WorkerId} with PID {ProcessId}",
                        workerInfo.WorkerId, workerInfo.ProcessId);
                }
                catch (Exception ex)
                {
                    results.FailedExecutions++;
                    _logger.LogError(ex, "❌ Failed to start worker process {WorkerId}", startRequest.WorkerId);
                }

                await UpdateTestStatus(execution, $"Started {i + 1}/{processCount} worker processes",
                    20 + ((i + 1) * 20 / processCount));
            }

            // Test 2: Monitor worker processes
            await UpdateTestStatus(execution, "Monitoring worker processes", 50);

            var monitoringDuration = TimeSpan.FromSeconds(30);
            var monitoringStart = DateTime.UtcNow;
            var monitoringTasks = new List<Task>();

            foreach (var workerInfo in workerProcesses)
            {
                var monitoringTask = Task.Run(async () =>
                {
                    while (DateTime.UtcNow - monitoringStart < monitoringDuration && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var status = await _workerProcessManager.GetWorkerProcessStatusAsync(workerInfo.WorkerId, cancellationToken);
                            if (status != null)
                            {
                                _logger.LogDebug("Worker {WorkerId} status: {State} - {Message}",
                                    status.WorkerId, status.State, status.Message);

                                // Send real-time update via SignalR
                                await _hubContext.Clients.Groups("WorkerTests", $"WorkerTest_{execution.Id}").SendAsync("WorkerProcessStatus", new
                                {
                                    TestId = execution.Id,
                                    WorkerId = status.WorkerId,
                                    ProcessId = status.ProcessId,
                                    State = status.State,
                                    Message = status.Message,
                                    IndicatorsProcessed = status.IndicatorsProcessed,
                                    MemoryUsageBytes = status.MemoryUsageBytes,
                                    IsHealthy = status.IsHealthy,
                                    Timestamp = DateTime.UtcNow
                                }, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error monitoring worker {WorkerId}", workerInfo.WorkerId);
                        }

                        await Task.Delay(2000, cancellationToken); // Check every 2 seconds
                    }
                }, cancellationToken);

                monitoringTasks.Add(monitoringTask);
            }

            // Wait for monitoring to complete
            await Task.WhenAll(monitoringTasks);
            await UpdateTestStatus(execution, "Worker monitoring completed", 70);

            // Test 3: Get final status of all workers
            await UpdateTestStatus(execution, "Getting final worker status", 80);

            var finalStatuses = new List<MonitoringGrid.Api.DTOs.Worker.WorkerProcessStatus>();
            foreach (var workerInfo in workerProcesses)
            {
                try
                {
                    var status = await _workerProcessManager.GetWorkerProcessStatusAsync(workerInfo.WorkerId, cancellationToken);
                    if (status != null)
                    {
                        finalStatuses.Add(status);
                        results.IndicatorsProcessed += status.IndicatorsProcessed;
                        results.SuccessfulExecutions += status.SuccessfulExecutions;
                        results.FailedExecutions += status.FailedExecutions;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get final status for worker {WorkerId}", workerInfo.WorkerId);
                }
            }

            // Test 4: Stop all worker processes
            await UpdateTestStatus(execution, "Stopping worker processes", 90);

            var stopTasks = workerProcesses.Select(async workerInfo =>
            {
                try
                {
                    var stopped = await _workerProcessManager.StopWorkerProcessAsync(workerInfo.WorkerId, cancellationToken);
                    if (stopped)
                    {
                        _logger.LogInformation("✅ Successfully stopped worker process {WorkerId}", workerInfo.WorkerId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to stop worker process {WorkerId}", workerInfo.WorkerId);
                    }
                    return stopped;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error stopping worker process {WorkerId}", workerInfo.WorkerId);
                    return false;
                }
            });

            var stopResults = await Task.WhenAll(stopTasks);
            var successfulStops = stopResults.Count(r => r);

            _logger.LogInformation("Worker process management test completed: {SuccessfulStops}/{TotalProcesses} processes stopped successfully",
                successfulStops, workerProcesses.Count);

            await UpdateTestStatus(execution, "Worker process management test completed", 95);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in worker process management test");
            throw;
        }
        finally
        {
            // Emergency cleanup: ensure all test worker processes are stopped
            foreach (var workerInfo in workerProcesses)
            {
                try
                {
                    await _workerProcessManager.StopWorkerProcessAsync(workerInfo.WorkerId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup worker process {WorkerId}", workerInfo.WorkerId);
                }
            }

            stopwatch.Stop();
            results.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            execution.Results = results;
        }
    }

    /// <summary>
    /// Persist test results to database for historical tracking
    /// </summary>
    private async Task PersistTestResultsAsync(WorkerTestExecution execution)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

            var historyRecord = new WorkerTestExecutionHistory
            {
                TestId = execution.Id,
                TestType = execution.TestType,
                StartedAt = execution.StartTime,
                CompletedAt = execution.EndTime,
                DurationMs = execution.EndTime?.Subtract(execution.StartTime).Milliseconds ?? 0,
                Success = execution.Success ?? false,
                Status = execution.Status,
                ErrorMessage = execution.ErrorMessage,
                IndicatorsProcessed = execution.Results?.IndicatorsProcessed ?? 0,
                SuccessfulExecutions = execution.Results?.SuccessfulExecutions ?? 0,
                FailedExecutions = execution.Results?.FailedExecutions ?? 0,
                AverageExecutionTimeMs = execution.Results?.AverageExecutionTimeMs ?? 0,
                MemoryUsageBytes = execution.Results?.MemoryUsageBytes ?? 0,
                CpuUsagePercent = execution.Results?.CpuUsagePercent ?? 0,
                AlertsTriggered = execution.Results?.AlertsTriggered ?? 0,
                WorkerCount = 1, // Default for most tests
                ConcurrentWorkers = 1, // Default for most tests
                TestConfiguration = JsonConvert.SerializeObject(new
                {
                    TestType = execution.TestType,
                    IndicatorIds = execution.IndicatorIds,
                    StartTime = execution.StartTime
                }),
                PerformanceMetrics = execution.Results?.PerformanceMetrics != null
                    ? JsonConvert.SerializeObject(execution.Results.PerformanceMetrics)
                    : null,
                DetailedResults = execution.Results != null
                    ? JsonConvert.SerializeObject(execution.Results)
                    : null,
                ExecutedBy = "System", // Could be enhanced to track actual user
                ExecutionContext = "WorkerIntegrationTest",
                Metadata = JsonConvert.SerializeObject(new
                {
                    Progress = execution.Progress,
                    LastUpdate = execution.LastUpdate
                })
            };

            context.WorkerTestExecutionHistory.Add(historyRecord);
            await context.SaveChangesAsync();

            _logger.LogInformation("✅ Test results persisted to database for test {TestId}", execution.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to persist test results for test {TestId}", execution.Id);
            // Don't throw - persistence failure shouldn't break the test execution
        }
    }
}
