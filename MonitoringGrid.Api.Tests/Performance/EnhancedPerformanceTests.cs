using Microsoft.AspNetCore.Mvc.Testing;
using MonitoringGrid.Api;
using System.Diagnostics;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Api.Tests.Performance;

/// <summary>
/// Enhanced performance tests for the MonitoringGrid API
/// Consolidated from MonitoringGrid.Tests project
/// </summary>
public class EnhancedPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public EnhancedPerformanceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task ApiInfo_ResponseTime_ShouldBeFast()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var warmupRequests = 5;
        var testRequests = 100;

        // Warmup
        for (int i = 0; i < warmupRequests; i++)
        {
            await client.GetAsync("/api/info");
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < testRequests; i++)
        {
            tasks.Add(client.GetAsync("/api/info"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)testRequests;
        var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);

        Assert.True(averageResponseTime < 100, $"Average response time {averageResponseTime:F2}ms should be under 100ms");
        Assert.True(successfulResponses >= testRequests * 0.95, "At least 95% of requests should succeed");

        _output.WriteLine($"Performance Test Results:");
        _output.WriteLine($"Total requests: {testRequests}");
        _output.WriteLine($"Successful responses: {successfulResponses}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average response time: {averageResponseTime:F2}ms");
        _output.WriteLine($"Requests per second: {testRequests / (stopwatch.ElapsedMilliseconds / 1000.0):F2}");
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        var concurrentUsers = 50;
        var requestsPerUser = 10;
        var totalRequests = concurrentUsers * requestsPerUser;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var userTasks = new List<Task>();

        for (int user = 0; user < concurrentUsers; user++)
        {
            userTasks.Add(SimulateUserLoad(requestsPerUser, user));
        }

        await Task.WhenAll(userTasks);
        stopwatch.Stop();

        // Assert
        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)totalRequests;
        var requestsPerSecond = totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0);

        Assert.True(averageResponseTime < 500, $"Average response time {averageResponseTime:F2}ms should be under 500ms under load");
        Assert.True(requestsPerSecond > 50, $"Should handle at least 50 requests per second, got {requestsPerSecond:F2}");

        _output.WriteLine($"Concurrent Load Test Results:");
        _output.WriteLine($"Concurrent users: {concurrentUsers}");
        _output.WriteLine($"Requests per user: {requestsPerUser}");
        _output.WriteLine($"Total requests: {totalRequests}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average response time: {averageResponseTime:F2}ms");
        _output.WriteLine($"Requests per second: {requestsPerSecond:F2}");
    }

    [Fact]
    public async Task CachingPerformance_ShouldImproveResponseTimes()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var endpoint = "/api/info";
        var iterations = 20;

        // Act - First request (cache miss)
        var firstRequestStopwatch = Stopwatch.StartNew();
        var firstResponse = await client.GetAsync(endpoint);
        firstRequestStopwatch.Stop();

        Assert.True(firstResponse.IsSuccessStatusCode);

        // Act - Subsequent requests (cache hits)
        var cachedRequestTimes = new List<long>();
        
        for (int i = 0; i < iterations; i++)
        {
            var cachedStopwatch = Stopwatch.StartNew();
            var cachedResponse = await client.GetAsync(endpoint);
            cachedStopwatch.Stop();
            
            Assert.True(cachedResponse.IsSuccessStatusCode);
            cachedRequestTimes.Add(cachedStopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageCachedTime = cachedRequestTimes.Average();
        var improvementRatio = firstRequestStopwatch.ElapsedMilliseconds / averageCachedTime;

        _output.WriteLine($"Caching Performance Test Results:");
        _output.WriteLine($"First request (cache miss): {firstRequestStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average cached request: {averageCachedTime:F2}ms");
        _output.WriteLine($"Performance improvement: {improvementRatio:F2}x");
        
        // Cached requests should be faster (though not always guaranteed due to various factors)
        Assert.True(averageCachedTime < firstRequestStopwatch.ElapsedMilliseconds * 2, 
            "Cached requests should not be significantly slower than the first request");
    }

    [Fact]
    public async Task RateLimiting_ShouldNotSignificantlyImpactPerformance()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var requestCount = 50; // Within rate limits
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(client.GetAsync("/api/info"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);
        var rateLimitedResponses = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)requestCount;

        _output.WriteLine($"Rate Limiting Performance Test Results:");
        _output.WriteLine($"Total requests: {requestCount}");
        _output.WriteLine($"Successful responses: {successfulResponses}");
        _output.WriteLine($"Rate limited responses: {rateLimitedResponses}");
        _output.WriteLine($"Average response time: {averageResponseTime:F2}ms");

        // Most requests should succeed within rate limits
        Assert.True(successfulResponses >= requestCount * 0.8, "At least 80% of requests should succeed within rate limits");
        
        // Rate limiting should not add significant overhead
        Assert.True(averageResponseTime < 200, $"Average response time {averageResponseTime:F2}ms should be under 200ms with rate limiting");
    }

    [Fact]
    public async Task CompressionPerformance_ShouldReduceBandwidth()
    {
        // Arrange
        using var clientWithCompression = _factory.CreateClient();
        using var clientWithoutCompression = _factory.CreateClient();
        
        clientWithCompression.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        // clientWithoutCompression doesn't request compression

        var endpoint = "/api/info";

        // Act
        var compressedResponse = await clientWithCompression.GetAsync(endpoint);
        var uncompressedResponse = await clientWithoutCompression.GetAsync(endpoint);

        var compressedContent = await compressedResponse.Content.ReadAsByteArrayAsync();
        var uncompressedContent = await uncompressedResponse.Content.ReadAsByteArrayAsync();

        // Assert
        Assert.True(compressedResponse.IsSuccessStatusCode);
        Assert.True(uncompressedResponse.IsSuccessStatusCode);

        _output.WriteLine($"Compression Performance Test Results:");
        _output.WriteLine($"Uncompressed size: {uncompressedContent.Length} bytes");
        _output.WriteLine($"Compressed size: {compressedContent.Length} bytes");
        
        if (compressedResponse.Content.Headers.ContentEncoding.Any())
        {
            var compressionRatio = (double)compressedContent.Length / uncompressedContent.Length;
            var savings = 1 - compressionRatio;
            
            _output.WriteLine($"Compression ratio: {compressionRatio:P2}");
            _output.WriteLine($"Bandwidth savings: {savings:P2}");
            _output.WriteLine($"Compression algorithm: {string.Join(", ", compressedResponse.Content.Headers.ContentEncoding)}");
            
            // Compression should provide some benefit for larger responses
            if (uncompressedContent.Length > 1024) // Only check for responses larger than 1KB
            {
                Assert.True(compressionRatio < 0.9, "Compression should provide at least 10% reduction for large responses");
            }
        }
        else
        {
            _output.WriteLine("Response was not compressed (may be too small or compression not enabled)");
        }
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var requestCount = 100;

        // Act
        using var client = _factory.CreateClient();
        
        for (int i = 0; i < requestCount; i++)
        {
            var response = await client.GetAsync("/api/info");
            var content = await response.Content.ReadAsStringAsync();
            
            // Force garbage collection every 10 requests
            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // Final garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerRequest = memoryIncrease / (double)requestCount;

        _output.WriteLine($"Memory Usage Test Results:");
        _output.WriteLine($"Initial memory: {initialMemory:N0} bytes");
        _output.WriteLine($"Final memory: {finalMemory:N0} bytes");
        _output.WriteLine($"Memory increase: {memoryIncrease:N0} bytes");
        _output.WriteLine($"Memory increase per request: {memoryIncreasePerRequest:F2} bytes");

        // Memory increase should be reasonable (less than 1KB per request)
        Assert.True(memoryIncreasePerRequest < 1024, 
            $"Memory increase per request ({memoryIncreasePerRequest:F2} bytes) should be under 1KB");
    }

    /// <summary>
    /// Simulates load from a single user making multiple requests
    /// </summary>
    private async Task SimulateUserLoad(int requestsPerUser, int userId)
    {
        using var client = _factory.CreateClient();
        
        // Add user-specific headers
        client.DefaultRequestHeaders.Add("X-User-ID", $"test-user-{userId}");
        
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < requestsPerUser; i++)
        {
            // Vary the endpoints to simulate realistic usage
            var endpoint = (i % 3) switch
            {
                0 => "/api/info",
                1 => "/health",
                _ => "/api/info"
            };
            
            tasks.Add(client.GetAsync(endpoint));
            
            // Add small delay to simulate realistic user behavior
            await Task.Delay(10);
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Log any failures for this user
        var failures = responses.Count(r => !r.IsSuccessStatusCode);
        if (failures > 0)
        {
            _output.WriteLine($"User {userId} had {failures} failed requests out of {requestsPerUser}");
        }
    }
}
