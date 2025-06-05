using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Net.Http.Json;
using MonitoringGrid.Core.Security;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.PerformanceTests;

public class LoadTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public LoadTests(TestWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task LoadTest_GetKpis_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var authToken = await GetAuthTokenAsync(httpClient);

        var scenario = Scenario.Create("get_kpis_load_test", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/kpis")
                .WithHeader("Authorization", $"Bearer {authToken}");

            var response = await Http.Send(httpClient, request);

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var sceneStats = stats.AllOkCount;
        var errorRate = stats.AllFailCount / (double)(stats.AllOkCount + stats.AllFailCount) * 100;

        _output.WriteLine($"Total requests: {stats.AllOkCount + stats.AllFailCount}");
        _output.WriteLine($"Successful requests: {stats.AllOkCount}");
        _output.WriteLine($"Failed requests: {stats.AllFailCount}");
        _output.WriteLine($"Error rate: {errorRate:F2}%");

        // Performance assertions
        errorRate.Should().BeLessThan(5.0); // Less than 5% error rate
        stats.ScenarioStats[0].Ok.Response.Mean.Should().BeLessThan(TimeSpan.FromSeconds(2)); // Average response time under 2 seconds
    }

    [Fact]
    public async Task LoadTest_Authentication_ShouldHandleMultipleLogins()
    {
        // Arrange
        var httpClient = _factory.CreateClient();

        var scenario = Scenario.Create("authentication_load_test", async context =>
        {
            var loginRequest = new LoginRequest
            {
                Username = "testuser1",
                Password = "TestPassword123!"
            };

            var request = Http.CreateRequest("POST", "/api/auth/login")
                .WithJsonBody(loginRequest);

            var response = await Http.Send(httpClient, request);

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(20))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var errorRate = stats.AllFailCount / (double)(stats.AllOkCount + stats.AllFailCount) * 100;

        _output.WriteLine($"Authentication requests: {stats.AllOkCount + stats.AllFailCount}");
        _output.WriteLine($"Successful logins: {stats.AllOkCount}");
        _output.WriteLine($"Failed logins: {stats.AllFailCount}");
        _output.WriteLine($"Error rate: {errorRate:F2}%");

        errorRate.Should().BeLessThan(1.0); // Less than 1% error rate for authentication
        stats.ScenarioStats[0].Ok.Response.Mean.Should().BeLessThan(TimeSpan.FromSeconds(1)); // Login should be fast
    }

    [Fact]
    public async Task LoadTest_KpiExecution_ShouldHandleConcurrentExecutions()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var authToken = await GetAuthTokenAsync(httpClient);

        // Get a KPI ID for testing
        var kpiId = await GetFirstKpiIdAsync(httpClient, authToken);

        var scenario = Scenario.Create("kpi_execution_load_test", async context =>
        {
            var request = Http.CreateRequest("POST", $"/api/kpis/{kpiId}/execute")
                .WithHeader("Authorization", $"Bearer {authToken}");

            var response = await Http.Send(httpClient, request);

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 2, during: TimeSpan.FromSeconds(30)) // Lower rate for execution
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var errorRate = stats.AllFailCount / (double)(stats.AllOkCount + stats.AllFailCount) * 100;

        _output.WriteLine($"KPI execution requests: {stats.AllOkCount + stats.AllFailCount}");
        _output.WriteLine($"Successful executions: {stats.AllOkCount}");
        _output.WriteLine($"Failed executions: {stats.AllFailCount}");
        _output.WriteLine($"Error rate: {errorRate:F2}%");

        errorRate.Should().BeLessThan(10.0); // Allow higher error rate for KPI execution
        stats.ScenarioStats[0].Ok.Response.Mean.Should().BeLessThan(TimeSpan.FromSeconds(5)); // KPI execution can take longer
    }

    [Fact]
    public async Task StressTest_MixedWorkload_ShouldMaintainPerformance()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var authToken = await GetAuthTokenAsync(httpClient);
        var kpiId = await GetFirstKpiIdAsync(httpClient, authToken);

        // Scenario 1: Read KPIs
        var readScenario = Scenario.Create("read_kpis", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/kpis")
                .WithHeader("Authorization", $"Bearer {authToken}");

            var response = await Http.Send(httpClient, request);
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithWeight(70) // 70% of traffic
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(1))
        );

        // Scenario 2: Get specific KPI
        var getKpiScenario = Scenario.Create("get_kpi", async context =>
        {
            var request = Http.CreateRequest("GET", $"/api/kpis/{kpiId}")
                .WithHeader("Authorization", $"Bearer {authToken}");

            var response = await Http.Send(httpClient, request);
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithWeight(20) // 20% of traffic
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 3, during: TimeSpan.FromMinutes(1))
        );

        // Scenario 3: Execute KPI
        var executeScenario = Scenario.Create("execute_kpi", async context =>
        {
            var request = Http.CreateRequest("POST", $"/api/kpis/{kpiId}/execute")
                .WithHeader("Authorization", $"Bearer {authToken}");

            var response = await Http.Send(httpClient, request);
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithWeight(10) // 10% of traffic
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 1, during: TimeSpan.FromMinutes(1))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(readScenario, getKpiScenario, executeScenario)
            .Run();

        // Assert
        var overallErrorRate = stats.AllFailCount / (double)(stats.AllOkCount + stats.AllFailCount) * 100;

        _output.WriteLine($"Overall requests: {stats.AllOkCount + stats.AllFailCount}");
        _output.WriteLine($"Overall error rate: {overallErrorRate:F2}%");

        foreach (var scenario in stats.ScenarioStats)
        {
            _output.WriteLine($"Scenario '{scenario.ScenarioName}': {scenario.Ok.Request.Count} requests, " +
                            $"Mean response time: {scenario.Ok.Response.Mean.TotalMilliseconds:F0}ms");
        }

        overallErrorRate.Should().BeLessThan(5.0);
        
        // Each scenario should maintain reasonable performance
        foreach (var scenario in stats.ScenarioStats)
        {
            scenario.Ok.Response.Mean.Should().BeLessThan(TimeSpan.FromSeconds(3));
        }
    }

    [Fact]
    public async Task MemoryLeakTest_LongRunningRequests_ShouldNotLeakMemory()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var authToken = await GetAuthTokenAsync(httpClient);

        var initialMemory = GC.GetTotalMemory(true);

        var scenario = Scenario.Create("memory_leak_test", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/kpis")
                .WithHeader("Authorization", $"Bearer {authToken}");

            var response = await Http.Send(httpClient, request);

            // Force garbage collection periodically
            if (context.InvocationNumber % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(2))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerRequest = memoryIncrease / (double)stats.AllOkCount;

        _output.WriteLine($"Initial memory: {initialMemory / 1024 / 1024:F2} MB");
        _output.WriteLine($"Final memory: {finalMemory / 1024 / 1024:F2} MB");
        _output.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024:F2} MB");
        _output.WriteLine($"Memory per request: {memoryIncreasePerRequest:F2} bytes");

        // Memory increase should be reasonable (less than 100MB for the test)
        (memoryIncrease / 1024 / 1024).Should().BeLessThan(100);
        
        // Memory per request should be minimal
        memoryIncreasePerRequest.Should().BeLessThan(10000); // Less than 10KB per request
    }

    private async Task<string> GetAuthTokenAsync(HttpClient httpClient)
    {
        var loginRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "TestPassword123!"
        };

        var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponse!.Token!.AccessToken;
    }

    private async Task<int> GetFirstKpiIdAsync(HttpClient httpClient, string authToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var response = await httpClient.GetAsync("/api/kpis");
        response.EnsureSuccessStatusCode();

        var kpis = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        return ((dynamic)kpis!.First()).KpiId;
    }
}

/// <summary>
/// Performance benchmarks for critical operations
/// </summary>
public class PerformanceBenchmarks : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public PerformanceBenchmarks(TestWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Benchmark_DatabaseQueries_ShouldMeetPerformanceTargets()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act & Assert - Test various database operations
        
        // 1. Simple KPI query
        stopwatch.Restart();
        var kpis = await context.KPIs.Take(100).ToListAsync();
        stopwatch.Stop();
        
        _output.WriteLine($"KPI query (100 records): {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be under 100ms

        // 2. Complex join query
        stopwatch.Restart();
        var kpisWithContacts = await context.KPIs
            .Include(k => k.KpiContacts)
            .ThenInclude(kc => kc.Contact)
            .Take(50)
            .ToListAsync();
        stopwatch.Stop();
        
        _output.WriteLine($"KPI with contacts query (50 records): {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200); // Should be under 200ms

        // 3. Historical data query
        stopwatch.Restart();
        var historicalData = await context.HistoricalData
            .Where(h => h.ExecutionTime >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(h => h.ExecutionTime)
            .Take(1000)
            .ToListAsync();
        stopwatch.Stop();
        
        _output.WriteLine($"Historical data query (1000 records): {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(300); // Should be under 300ms
    }

    [Fact]
    public async Task Benchmark_JwtTokenOperations_ShouldBeFast()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        
        var user = new AuthUser
        {
            UserId = "benchmark-user",
            Username = "benchmarkuser",
            Email = "benchmark@example.com",
            DisplayName = "Benchmark User",
            Roles = new List<string> { "Admin", "User" },
            Permissions = new List<string> { "read:kpis", "write:kpis" }
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act & Assert
        
        // 1. Token generation
        stopwatch.Restart();
        var token = jwtService.GenerateAccessToken(user);
        stopwatch.Stop();
        
        _output.WriteLine($"Token generation: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10); // Should be under 10ms

        // 2. Token validation
        stopwatch.Restart();
        var principal = jwtService.ValidateToken(token);
        stopwatch.Stop();
        
        _output.WriteLine($"Token validation: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5); // Should be under 5ms

        // 3. Multiple token operations
        stopwatch.Restart();
        for (int i = 0; i < 100; i++)
        {
            var testToken = jwtService.GenerateAccessToken(user);
            jwtService.ValidateToken(testToken);
        }
        stopwatch.Stop();
        
        _output.WriteLine($"100 token operations: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be under 100ms for 100 operations
    }
}
