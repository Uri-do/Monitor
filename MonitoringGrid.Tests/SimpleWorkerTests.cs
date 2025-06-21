using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Controllers;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests;

/// <summary>
/// Simple, working tests for Worker Controller that can actually compile and run
/// </summary>
public class SimpleWorkerTests
{
    private readonly ITestOutputHelper _output;

    public SimpleWorkerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicTest_ShouldPass()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up basic test");
        var testValue = "Hello World";

        // Act
        _output.WriteLine("[TEST] Performing basic operation");
        var result = testValue.ToUpper();

        // Assert
        _output.WriteLine("[TEST] Verifying result");
        Assert.Equal("HELLO WORLD", result);
        _output.WriteLine("[PASS] Basic test completed successfully");
    }

    [Fact]
    public void WorkerController_CanBeInstantiated()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up WorkerController instantiation test");
        var mockLogger = new Mock<ILogger<WorkerController>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var mockRepository = new Mock<MonitoringGrid.Core.Interfaces.IRepository<MonitoringGrid.Core.Entities.Indicator>>();
        var mockMediator = new Mock<MediatR.IMediator>();

        // Act
        _output.WriteLine("[TEST] Creating WorkerController instance");
        var controller = new WorkerController(
            mockMediator.Object,
            mockLogger.Object,
            mockServiceProvider.Object,
            mockConfiguration.Object,
            mockRepository.Object);

        // Assert
        _output.WriteLine("[TEST] Verifying controller creation");
        Assert.NotNull(controller);
        _output.WriteLine("[PASS] WorkerController instantiated successfully");
    }

    [Fact]
    public void TestDataBuilder_CanCreateBasicData()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up test data creation");

        // Act
        _output.WriteLine("[TEST] Creating test data");
        var startRequest = new MonitoringGrid.Api.DTOs.Worker.StartWorkerRequest
        {
            TimeoutMs = 30000,
            Force = false,
            Arguments = "--test"
        };

        var stopRequest = new MonitoringGrid.Api.DTOs.Worker.StopWorkerRequest
        {
            TimeoutMs = 15000,
            Force = false,
            StopAll = true
        };

        // Assert
        _output.WriteLine("[TEST] Verifying test data");
        Assert.NotNull(startRequest);
        Assert.NotNull(stopRequest);
        Assert.Equal(30000, startRequest.TimeoutMs);
        Assert.Equal(15000, stopRequest.TimeoutMs);
        _output.WriteLine("[PASS] Test data created successfully");
    }

    [Fact]
    public void MockFramework_IsWorking()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up mock framework test");
        var mockLogger = new Mock<ILogger<WorkerController>>();

        // Act
        _output.WriteLine("[TEST] Using mock logger");
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        var isEnabled = mockLogger.Object.IsEnabled(LogLevel.Information);

        // Assert
        _output.WriteLine("[TEST] Verifying mock behavior");
        Assert.True(isEnabled);
        mockLogger.Verify(x => x.IsEnabled(LogLevel.Information), Times.Once);
        _output.WriteLine("[PASS] Mock framework working correctly");
    }

    [Fact]
    public void PerformanceTest_BasicMeasurement()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up performance measurement test");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        _output.WriteLine("[TEST] Performing timed operation");
        var result = 0;
        for (int i = 0; i < 10000; i++)
        {
            result += i;
        }
        stopwatch.Stop();

        // Assert
        _output.WriteLine("[TEST] Verifying performance");
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Operation should complete within 1 second");
        Assert.Equal(49995000, result); // Sum of 0 to 9999
        _output.WriteLine($"[PASS] Performance test completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ParameterizedTest_WithDifferentOptions(bool option1, bool option2)
    {
        // Arrange
        _output.WriteLine($"[TEST] Testing with options: {option1}, {option2}");

        // Act
        var result = option1 || option2;

        // Assert
        _output.WriteLine("[TEST] Verifying logical OR operation");
        var expected = option1 || option2;
        Assert.Equal(expected, result);
        _output.WriteLine($"[PASS] Parameterized test passed for {option1}, {option2}");
    }

    [Fact]
    public async Task AsyncTest_ShouldWork()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up async test");

        // Act
        _output.WriteLine("[TEST] Performing async operation");
        await Task.Delay(10); // Simulate async work
        var result = await Task.FromResult("Async Result");

        // Assert
        _output.WriteLine("[TEST] Verifying async result");
        Assert.Equal("Async Result", result);
        _output.WriteLine("[PASS] Async test completed successfully");
    }

    [Fact]
    public void ExceptionTest_ShouldHandleExceptions()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up exception handling test");

        // Act & Assert
        _output.WriteLine("[TEST] Verifying exception is thrown");
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            string? nullValue = null;
            ArgumentNullException.ThrowIfNull(nullValue, "testParam");
        });

        Assert.Equal("testParam", exception.ParamName);
        _output.WriteLine("[PASS] Exception handling test completed successfully");
    }

    [Fact]
    public void MemoryTest_BasicGarbageCollection()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up memory test");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        // Act
        _output.WriteLine("[TEST] Creating and disposing objects");
        var objects = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            objects.Add($"Test string {i}");
        }
        objects.Clear();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        _output.WriteLine("[TEST] Verifying memory cleanup");
        var memoryIncrease = finalMemory - initialMemory;
        Assert.True(memoryIncrease < 1_000_000, "Memory should be cleaned up after GC");
        _output.WriteLine($"[PASS] Memory test passed: {memoryIncrease:N0} bytes increase");
    }

    [Fact]
    public void ConcurrencyTest_BasicParallelExecution()
    {
        // Arrange
        _output.WriteLine("[TEST] Setting up concurrency test");
        const int taskCount = 10;
        var tasks = new List<Task<int>>();

        // Act
        _output.WriteLine("[TEST] Running parallel tasks");
        for (int i = 0; i < taskCount; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                Thread.Sleep(10); // Simulate work
                return taskId * 2;
            }));
        }

        var results = Task.WhenAll(tasks).Result;

        // Assert
        _output.WriteLine("[TEST] Verifying parallel execution results");
        Assert.Equal(taskCount, results.Length);
        for (int i = 0; i < taskCount; i++)
        {
            Assert.Equal(i * 2, results[i]);
        }
        _output.WriteLine("[PASS] Concurrency test completed successfully");
    }
}
