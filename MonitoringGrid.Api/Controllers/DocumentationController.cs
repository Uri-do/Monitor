using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.Documentation;
using MonitoringGrid.Api.Documentation.Models;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for API documentation and testing endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentationController : ControllerBase
{
    private readonly IApiDocumentationService _documentationService;
    private readonly ILogger<DocumentationController> _logger;

    public DocumentationController(
        IApiDocumentationService documentationService,
        ILogger<DocumentationController> logger)
    {
        _documentationService = documentationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets comprehensive API documentation
    /// </summary>
    /// <returns>Complete API documentation including endpoints, examples, and architecture</returns>
    /// <response code="200">Returns the complete API documentation</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiDocumentation>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ApiDocumentation>>> GetDocumentation()
    {
        try
        {
            var documentation = await _documentationService.GenerateDocumentationAsync();
            return Ok(ApiResponse.Success(documentation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate API documentation");
            return StatusCode(500, ApiResponse.Failure("Failed to generate API documentation"));
        }
    }

    /// <summary>
    /// Gets endpoint-specific documentation
    /// </summary>
    /// <returns>List of all API endpoints with detailed documentation</returns>
    /// <response code="200">Returns endpoint documentation</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(ApiResponse<List<EndpointDocumentation>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<EndpointDocumentation>>>> GetEndpoints()
    {
        try
        {
            var endpoints = await _documentationService.GetEndpointDocumentationAsync();
            return Ok(ApiResponse.Success(endpoints));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get endpoint documentation");
            return StatusCode(500, ApiResponse.Failure("Failed to get endpoint documentation"));
        }
    }

    /// <summary>
    /// Gets API usage examples
    /// </summary>
    /// <returns>Collection of API usage examples with requests and responses</returns>
    /// <response code="200">Returns API examples</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("examples")]
    [ProducesResponseType(typeof(ApiResponse<List<ApiExample>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<ApiExample>>>> GetExamples()
    {
        try
        {
            var examples = await _documentationService.GetApiExamplesAsync();
            return Ok(ApiResponse.Success(examples));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API examples");
            return StatusCode(500, ApiResponse.Failure("Failed to get API examples"));
        }
    }

    /// <summary>
    /// Gets performance documentation
    /// </summary>
    /// <returns>Performance optimization documentation including caching, rate limiting, and compression</returns>
    /// <response code="200">Returns performance documentation</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceDocumentation>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PerformanceDocumentation>>> GetPerformanceDocumentation()
    {
        try
        {
            var performance = await _documentationService.GetPerformanceDocumentationAsync();
            return Ok(ApiResponse.Success(performance));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance documentation");
            return StatusCode(500, ApiResponse.Failure("Failed to get performance documentation"));
        }
    }

    /// <summary>
    /// Gets OpenAPI specification
    /// </summary>
    /// <returns>OpenAPI 3.0 specification document</returns>
    /// <response code="200">Returns OpenAPI specification</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("openapi")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<object>>> GetOpenApiSpecification()
    {
        try
        {
            var openApiDoc = await _documentationService.GenerateOpenApiDocumentAsync();
            return Ok(ApiResponse.Success(openApiDoc, "OpenAPI specification generated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OpenAPI specification");
            return StatusCode(500, ApiResponse.Failure("Failed to generate OpenAPI specification"));
        }
    }

    /// <summary>
    /// Gets API testing guide
    /// </summary>
    /// <returns>Comprehensive guide for testing the API</returns>
    /// <response code="200">Returns testing guide</response>
    [HttpGet("testing-guide")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public ActionResult<ApiResponse<object>> GetTestingGuide()
    {
        var testingGuide = new
        {
            title = "MonitoringGrid API Testing Guide",
            overview = "Comprehensive guide for testing the MonitoringGrid API",
            sections = new object[]
            {
                new
                {
                    title = "Authentication",
                    description = "How to authenticate with the API",
                    steps = new[]
                    {
                        "Obtain a JWT token from the authentication endpoint",
                        "Include the token in the Authorization header: 'Bearer {token}'",
                        "Ensure the token is valid and not expired"
                    },
                    example = new
                    {
                        method = "POST",
                        url = "/api/auth/login",
                        headers = new { ContentType = "application/json" },
                        body = new { username = "testuser", password = "testpass" },
                        response = new { token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
                    }
                },
                new
                {
                    title = "Rate Limiting",
                    description = "Understanding API rate limits",
                    limits = new
                    {
                        ip = "60 requests per minute",
                        user = "120 requests per minute",
                        endpoint = "1000 requests per minute"
                    },
                    headers = new[]
                    {
                        "X-RateLimit-Remaining: Number of requests remaining",
                        "X-RateLimit-Reset: Unix timestamp when limit resets",
                        "Retry-After: Seconds to wait before retrying (when rate limited)"
                    }
                },
                new
                {
                    title = "Caching",
                    description = "How to leverage API caching",
                    features = new[]
                    {
                        "ETag support for conditional requests",
                        "Multi-layer caching (Memory + Distributed)",
                        "Cache warming for frequently accessed data",
                        "Tag-based cache invalidation"
                    },
                    usage = new
                    {
                        etag = "Include If-None-Match header with ETag value",
                        compression = "Include Accept-Encoding: gzip header",
                        fields = "Use ?fields=field1,field2 for partial responses"
                    }
                },
                new
                {
                    title = "Error Handling",
                    description = "Understanding API error responses",
                    structure = new
                    {
                        isSuccess = false,
                        message = "Error description",
                        errors = new[] { "Detailed error messages" },
                        correlationId = "12345678-1234-1234-1234-123456789012",
                        timestamp = "2024-12-01T10:30:00Z"
                    },
                    commonErrors = new object[]
                    {
                        new { code = 400, description = "Bad Request - Invalid parameters" },
                        new { code = 401, description = "Unauthorized - Invalid or missing token" },
                        new { code = 403, description = "Forbidden - Insufficient permissions" },
                        new { code = 404, description = "Not Found - Resource not found" },
                        new { code = 429, description = "Too Many Requests - Rate limit exceeded" },
                        new { code = 500, description = "Internal Server Error - Server error" }
                    }
                },
                new
                {
                    title = "Performance Testing",
                    description = "Guidelines for performance testing",
                    recommendations = new[]
                    {
                        "Use correlation IDs for request tracking",
                        "Test with realistic data volumes",
                        "Include authentication in performance tests",
                        "Test rate limiting behavior",
                        "Verify caching effectiveness",
                        "Monitor response times and throughput"
                    },
                    tools = new[]
                    {
                        "Integration tests with WebApplicationFactory",
                        "Load testing with concurrent requests",
                        "Memory usage monitoring",
                        "Response time analysis"
                    }
                },
                new
                {
                    title = "Security Testing",
                    description = "Security testing considerations",
                    areas = new[]
                    {
                        "JWT token validation and replay protection",
                        "Rate limiting and automatic blocking",
                        "Input validation and sanitization",
                        "Security event logging and monitoring",
                        "Suspicious activity detection"
                    },
                    tests = new[]
                    {
                        "Test with invalid/expired tokens",
                        "Attempt token replay attacks",
                        "Test rate limiting thresholds",
                        "Verify security headers",
                        "Test input sanitization"
                    }
                }
            },
            bestPractices = new[]
            {
                "Always include correlation IDs for request tracking",
                "Use appropriate HTTP methods (GET, POST, PUT, DELETE)",
                "Include proper Content-Type headers",
                "Handle rate limiting gracefully with exponential backoff",
                "Leverage caching with ETags and compression",
                "Monitor API performance and error rates",
                "Test edge cases and error conditions",
                "Validate all input parameters",
                "Use HTTPS in production environments"
            }
        };

        return Ok(ApiResponse.Success(testingGuide, "Testing guide generated successfully"));
    }

    /// <summary>
    /// Gets API health and status information
    /// </summary>
    /// <returns>Current API health status and metrics</returns>
    /// <response code="200">Returns API health status</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public ActionResult<ApiResponse<object>> GetApiHealth()
    {
        var healthStatus = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            features = new
            {
                caching = new
                {
                    enabled = true,
                    layers = new[] { "Memory", "Distributed" },
                    warmupEnabled = true
                },
                rateLimiting = new
                {
                    enabled = true,
                    multiDimensional = true,
                    autoBlocking = true
                },
                security = new
                {
                    jwtAuthentication = true,
                    replayProtection = true,
                    threatDetection = true,
                    securityHeaders = true
                },
                performance = new
                {
                    compression = true,
                    etagSupport = true,
                    streamingResponses = true,
                    databaseOptimization = true
                }
            },
            uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
            memoryUsage = new
            {
                workingSet = Environment.WorkingSet,
                gcMemory = GC.GetTotalMemory(false)
            }
        };

        return Ok(ApiResponse.Success(healthStatus, "API health status retrieved successfully"));
    }
}
