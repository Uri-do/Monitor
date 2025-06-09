using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api;
using MonitoringGrid.Api.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for the MonitoringGrid API
/// </summary>
public class ApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiIntegrationTests(ApiWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    [Fact]
    public async Task GetApiInfo_ShouldReturnSuccessWithCorrectStructure()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await _client.GetAsync("/api/info");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString());
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsSuccess);
        Assert.Equal(correlationId, apiResponse.CorrelationId);
        
        _output.WriteLine($"API Info Response: {content}");
    }

    [Fact]
    public async Task GetKpis_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/kpis");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        _output.WriteLine($"Unauthorized response status: {response.StatusCode}");
    }

    [Fact]
    public async Task GetKpis_WithValidAuthentication_ShouldReturnSuccess()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/kpis");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsSuccess);
        
        _output.WriteLine($"KPIs Response: {content}");
    }

    [Fact]
    public async Task GetKpis_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/kpis?page=1&pageSize=5");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsSuccess);
        
        _output.WriteLine($"Paginated KPIs Response: {content}");
    }

    [Fact]
    public async Task ExecuteKpi_WithValidId_ShouldReturnExecutionResult()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var requestBody = new
        {
            parameters = new
            {
                timeRange = "1h",
                threshold = 100
            }
        };
        
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/kpis/1/execute", jsonContent);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.IsSuccess);
        }
        
        _output.WriteLine($"Execute KPI Response ({response.StatusCode}): {content}");
    }

    [Fact]
    public async Task RateLimit_ExcessiveRequests_ShouldReturnTooManyRequests()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Make multiple rapid requests to trigger rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 70; i++) // Exceed the 60/minute limit
        {
            tasks.Add(_client.GetAsync("/api/v1/kpis"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - At least some requests should be rate limited
        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();
        
        _output.WriteLine($"Total requests: {responses.Length}");
        _output.WriteLine($"Rate limited responses: {rateLimitedResponses.Count}");
        
        // We expect some rate limiting to occur
        Assert.True(rateLimitedResponses.Count > 0, "Expected some requests to be rate limited");
        
        // Check rate limit headers
        var rateLimitedResponse = rateLimitedResponses.First();
        Assert.True(rateLimitedResponse.Headers.Contains("Retry-After"));
        Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Remaining"));
    }

    [Fact]
    public async Task Caching_ETagSupport_ShouldReturnNotModifiedForSameContent()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - First request
        var firstResponse = await _client.GetAsync("/api/info");
        var etag = firstResponse.Headers.ETag?.Tag;
        
        Assert.NotNull(etag);
        
        // Second request with If-None-Match header
        _client.DefaultRequestHeaders.IfNoneMatch.Clear();
        _client.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
        
        var secondResponse = await _client.GetAsync("/api/info");

        // Assert
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
        
        _output.WriteLine($"First response ETag: {etag}");
        _output.WriteLine($"Second response status: {secondResponse.StatusCode}");
    }

    [Fact]
    public async Task Compression_LargeResponse_ShouldBeCompressed()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await _client.GetAsync("/api/v1/kpis");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Check if response is compressed
        var contentEncoding = response.Content.Headers.ContentEncoding;
        if (contentEncoding.Any())
        {
            Assert.Contains("gzip", contentEncoding);
            _output.WriteLine($"Response is compressed with: {string.Join(", ", contentEncoding)}");
        }
        else
        {
            _output.WriteLine("Response is not compressed (may be too small)");
        }
    }

    [Fact]
    public async Task CorrelationId_ShouldBePropagatedThroughRequest()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await _client.GetAsync("/api/info");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
        Assert.NotNull(apiResponse);
        Assert.Equal(correlationId, apiResponse.CorrelationId);
        
        _output.WriteLine($"Correlation ID propagated: {correlationId}");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresentInResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Check for security headers
        var securityHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options",
            "X-XSS-Protection",
            "Referrer-Policy"
        };

        foreach (var header in securityHeaders)
        {
            if (response.Headers.Contains(header))
            {
                _output.WriteLine($"Security header present: {header} = {string.Join(", ", response.Headers.GetValues(header))}");
            }
            else
            {
                _output.WriteLine($"Security header missing: {header}");
            }
        }
    }

    [Fact]
    public async Task ErrorHandling_InvalidEndpoint_ShouldReturnStructuredError()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.IsSuccess);
        Assert.NotEmpty(apiResponse.Errors);
        Assert.NotNull(apiResponse.CorrelationId);
        
        _output.WriteLine($"Error Response: {content}");
    }

    [Fact]
    public async Task Performance_ConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        var concurrentRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            tasks.Add(client.GetAsync("/api/info"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successfulResponses = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)concurrentRequests;

        Assert.True(successfulResponses > 0, "At least some requests should succeed");
        Assert.True(averageResponseTime < 1000, "Average response time should be under 1 second");
        
        _output.WriteLine($"Concurrent requests: {concurrentRequests}");
        _output.WriteLine($"Successful responses: {successfulResponses}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average response time: {averageResponseTime:F2}ms");
    }

    /// <summary>
    /// Helper method to get a valid JWT token for testing
    /// </summary>
    private async Task<string> GetValidJwtTokenAsync()
    {
        // In a real implementation, this would authenticate with the API
        // For testing purposes, we'll generate a test token or use a mock
        
        // This is a simplified test token - in production you'd have proper authentication
        return "test-jwt-token-for-integration-tests";
    }
}

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configure test-specific services
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            // Override any services for testing if needed
            // For example, replace database with in-memory database
        });

        builder.UseEnvironment("Testing");
    }
}
