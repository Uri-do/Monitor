using Microsoft.OpenApi.Models;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Documentation.Models;
using System.Reflection;
using System.Text.Json;

namespace MonitoringGrid.Api.Documentation;

/// <summary>
/// Service for generating comprehensive API documentation
/// </summary>
public interface IApiDocumentationService
{
    /// <summary>
    /// Generates comprehensive API documentation
    /// </summary>
    Task<ApiDocumentation> GenerateDocumentationAsync();

    /// <summary>
    /// Gets API endpoint documentation
    /// </summary>
    Task<List<EndpointDocumentation>> GetEndpointDocumentationAsync();

    /// <summary>
    /// Gets API examples and samples
    /// </summary>
    Task<List<ApiExample>> GetApiExamplesAsync();

    /// <summary>
    /// Generates OpenAPI specification
    /// </summary>
    Task<OpenApiDocument> GenerateOpenApiDocumentAsync();

    /// <summary>
    /// Gets API performance metrics documentation
    /// </summary>
    Task<PerformanceDocumentation> GetPerformanceDocumentationAsync();
}

/// <summary>
/// Implementation of API documentation service
/// </summary>
public class ApiDocumentationService : IApiDocumentationService
{
    private readonly ILogger<ApiDocumentationService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public ApiDocumentationService(
        ILogger<ApiDocumentationService> logger,
        ICorrelationIdService correlationIdService,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Generates comprehensive API documentation
    /// </summary>
    public async Task<ApiDocumentation> GenerateDocumentationAsync()
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Generating comprehensive API documentation [{CorrelationId}]", correlationId);

            var documentation = new ApiDocumentation
            {
                GeneratedAt = DateTime.UtcNow,
                Version = GetApiVersion(),
                Title = "MonitoringGrid API",
                Description = "Comprehensive Indicator monitoring and alerting system with advanced performance optimization and security features",
                BaseUrl = GetBaseUrl(),
                Endpoints = await GetEndpointDocumentationAsync(),
                Examples = await GetApiExamplesAsync(),
                Performance = await GetPerformanceDocumentationAsync(),
                Security = GetSecurityDocumentation(),
                Architecture = GetArchitectureDocumentation()
            };

            _logger.LogInformation("API documentation generated successfully with {EndpointCount} endpoints [{CorrelationId}]",
                documentation.Endpoints.Count, correlationId);

            return documentation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate API documentation [{CorrelationId}]", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive endpoint documentation
    /// </summary>
    public Task<List<EndpointDocumentation>> GetEndpointDocumentationAsync()
    {
        var endpoints = new List<EndpointDocumentation>
        {
            // Indicator Management Endpoints
            new EndpointDocumentation
            {
                Method = "GET",
                Path = "/api/indicator",
                Summary = "Get all Indicators with advanced filtering and pagination",
                Description = "Retrieves Indicators with support for filtering by status, priority, category, and advanced search capabilities. Includes performance optimization with caching and response compression.",
                Parameters = new List<ParameterDocumentation>
                {
                    new() { Name = "isActive", Type = "boolean", Description = "Filter by active status", Required = false },
                    new() { Name = "search", Type = "string", Description = "Search term for Indicator name or description", Required = false },
                    new() { Name = "priority", Type = "byte", Description = "Filter by priority level (1-5)", Required = false },
                    new() { Name = "category", Type = "string", Description = "Filter by Indicator category", Required = false },
                    new() { Name = "page", Type = "integer", Description = "Page number for pagination (default: 1)", Required = false },
                    new() { Name = "pageSize", Type = "integer", Description = "Items per page (default: 10, max: 100)", Required = false },
                    new() { Name = "sortBy", Type = "string", Description = "Sort field (name, priority, lastExecuted)", Required = false },
                    new() { Name = "includeInactive", Type = "boolean", Description = "Include inactive Indicators in results", Required = false }
                },
                Responses = new List<ResponseDocumentation>
                {
                    new() { StatusCode = 200, Description = "Success - Returns paginated Indicator list with metadata", Example = GetIndicatorListExample() },
                    new() { StatusCode = 400, Description = "Bad Request - Invalid parameters", Example = GetErrorExample("Invalid page size") },
                    new() { StatusCode = 429, Description = "Too Many Requests - Rate limit exceeded", Example = GetRateLimitExample() },
                    new() { StatusCode = 500, Description = "Internal Server Error", Example = GetErrorExample("Internal server error") }
                },
                RateLimits = new RateLimitDocumentation
                {
                    RequestsPerMinute = 50,
                    BurstSize = 10,
                    Scope = "Per user/IP"
                },
                CachingInfo = new CachingDocumentation
                {
                    CacheEnabled = true,
                    CacheDuration = TimeSpan.FromMinutes(5),
                    ETagSupport = true,
                    VaryHeaders = new[] { "Accept", "Accept-Encoding" }
                }
            },

            new EndpointDocumentation
            {
                Method = "POST",
                Path = "/api/indicator/{id}/execute",
                Summary = "Execute a specific Indicator",
                Description = "Executes an Indicator and returns the results. Supports both synchronous and asynchronous execution modes with comprehensive error handling and performance monitoring.",
                Parameters = new List<ParameterDocumentation>
                {
                    new() { Name = "id", Type = "integer", Description = "Indicator identifier", Required = true, Location = "path" },
                    new() { Name = "async", Type = "boolean", Description = "Execute asynchronously (default: false)", Required = false },
                    new() { Name = "timeout", Type = "integer", Description = "Execution timeout in seconds (default: 30)", Required = false }
                },
                RequestBody = new RequestBodyDocumentation
                {
                    ContentType = "application/json",
                    Description = "Optional execution parameters",
                    Example = GetIndicatorExecuteRequestExample(),
                    Schema = "IndicatorExecutionRequest"
                },
                Responses = new List<ResponseDocumentation>
                {
                    new() { StatusCode = 200, Description = "Success - Indicator executed successfully", Example = GetIndicatorExecuteResponseExample() },
                    new() { StatusCode = 202, Description = "Accepted - Asynchronous execution started", Example = GetAsyncExecuteExample() },
                    new() { StatusCode = 400, Description = "Bad Request - Invalid Indicator or parameters", Example = GetErrorExample("Invalid Indicator configuration") },
                    new() { StatusCode = 404, Description = "Not Found - Indicator not found", Example = GetErrorExample("Indicator not found") },
                    new() { StatusCode = 408, Description = "Request Timeout - Execution timeout", Example = GetErrorExample("Execution timeout") },
                    new() { StatusCode = 429, Description = "Too Many Requests - Rate limit exceeded", Example = GetRateLimitExample() }
                },
                RateLimits = new RateLimitDocumentation
                {
                    RequestsPerMinute = 20,
                    BurstSize = 5,
                    Scope = "Per user/IP"
                }
            },

            // Alert Management Endpoints
            new EndpointDocumentation
            {
                Method = "GET",
                Path = "/api/alerts",
                Summary = "Get alerts with advanced filtering",
                Description = "Retrieves alerts with comprehensive filtering, sorting, and pagination. Includes real-time updates via SignalR integration.",
                Parameters = new List<ParameterDocumentation>
                {
                    new() { Name = "severity", Type = "string", Description = "Filter by severity (Low, Medium, High, Critical)", Required = false },
                    new() { Name = "status", Type = "string", Description = "Filter by status (Active, Acknowledged, Resolved)", Required = false },
                    new() { Name = "indicatorId", Type = "integer", Description = "Filter by Indicator identifier", Required = false },
                    new() { Name = "fromDate", Type = "datetime", Description = "Filter alerts from date (ISO 8601)", Required = false },
                    new() { Name = "toDate", Type = "datetime", Description = "Filter alerts to date (ISO 8601)", Required = false },
                    new() { Name = "page", Type = "integer", Description = "Page number for pagination", Required = false },
                    new() { Name = "pageSize", Type = "integer", Description = "Items per page", Required = false }
                },
                Responses = new List<ResponseDocumentation>
                {
                    new() { StatusCode = 200, Description = "Success - Returns paginated alert list", Example = GetAlertListExample() },
                    new() { StatusCode = 400, Description = "Bad Request - Invalid date format", Example = GetErrorExample("Invalid date format") }
                },
                RateLimits = new RateLimitDocumentation
                {
                    RequestsPerMinute = 30,
                    BurstSize = 10,
                    Scope = "Per user/IP"
                }
            },

            // System Information Endpoints
            new EndpointDocumentation
            {
                Method = "GET",
                Path = "/api/info",
                Summary = "Get API information and health status",
                Description = "Returns comprehensive API information including version, environment, performance metrics, and system health status.",
                Parameters = new List<ParameterDocumentation>(),
                Responses = new List<ResponseDocumentation>
                {
                    new() { StatusCode = 200, Description = "Success - API information", Example = GetApiInfoExample() }
                },
                RateLimits = new RateLimitDocumentation
                {
                    RequestsPerMinute = 100,
                    BurstSize = 20,
                    Scope = "Per IP"
                },
                CachingInfo = new CachingDocumentation
                {
                    CacheEnabled = true,
                    CacheDuration = TimeSpan.FromMinutes(1),
                    ETagSupport = false
                }
            }
        };

        return Task.FromResult(endpoints);
    }

    /// <summary>
    /// Gets comprehensive API examples
    /// </summary>
    public Task<List<ApiExample>> GetApiExamplesAsync()
    {
        var examples = new List<ApiExample>
        {
            new ApiExample
            {
                Title = "Get Indicators with Filtering",
                Description = "Example of retrieving Indicators with advanced filtering and pagination",
                Method = "GET",
                Url = "/api/indicator?isActive=true&priority=3&page=1&pageSize=10&sortBy=priority",
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "application/json",
                    ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                    ["X-Correlation-ID"] = "12345678-1234-1234-1234-123456789012"
                },
                Response = GetIndicatorListExample()
            },

            new ApiExample
            {
                Title = "Execute Indicator Asynchronously",
                Description = "Example of executing an Indicator in asynchronous mode",
                Method = "POST",
                Url = "/api/indicator/123/execute?async=true&timeout=60",
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                    ["X-Correlation-ID"] = "12345678-1234-1234-1234-123456789012"
                },
                RequestBody = GetIndicatorExecuteRequestExample(),
                Response = GetAsyncExecuteExample()
            },

            new ApiExample
            {
                Title = "Rate Limit Exceeded Response",
                Description = "Example response when rate limit is exceeded",
                Method = "GET",
                Url = "/api/indicator",
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "application/json"
                },
                Response = GetRateLimitExample()
            }
        };

        return Task.FromResult(examples);
    }

    /// <summary>
    /// Generates OpenAPI specification document
    /// </summary>
    public Task<OpenApiDocument> GenerateOpenApiDocumentAsync()
    {
        var openApiDoc = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "MonitoringGrid API",
                Version = GetApiVersion(),
                Description = "Comprehensive Indicator monitoring and alerting system with advanced performance optimization and security features",
                Contact = new OpenApiContact
                {
                    Name = "MonitoringGrid Support",
                    Email = "support@monitoringgrid.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License"
                }
            },
            Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = GetBaseUrl(), Description = "Production API" },
                new OpenApiServer { Url = "https://localhost:57652", Description = "Development API (HTTPS)" },
                new OpenApiServer { Url = "http://localhost:57653", Description = "Development API (HTTP)" }
            }
        };

        return Task.FromResult(openApiDoc);
    }

    /// <summary>
    /// Gets performance documentation
    /// </summary>
    public Task<PerformanceDocumentation> GetPerformanceDocumentationAsync()
    {
        var performanceDoc = new PerformanceDocumentation
        {
            CachingStrategy = new CachingStrategyDocumentation
            {
                Description = "Multi-layer caching with Memory (L1) and Distributed (L2) cache",
                MemoryCacheSize = "200MB",
                DistributedCacheType = "In-Memory (Development) / Redis (Production)",
                DefaultCacheDuration = TimeSpan.FromMinutes(15),
                CacheWarmupEnabled = true,
                TagBasedInvalidation = true
            },
            RateLimiting = new RateLimitingDocumentation
            {
                Description = "Multi-dimensional rate limiting with automatic threat detection",
                IpLimits = "60 requests/minute, 10 burst",
                UserLimits = "120 requests/minute, 20 burst",
                EndpointLimits = "1000 requests/minute, 100 burst",
                AutoBlockingEnabled = true,
                ThreatDetectionEnabled = true
            },
            Compression = new CompressionDocumentation
            {
                Description = "Intelligent response compression with size thresholds",
                Algorithms = new[] { "Gzip", "Brotli" },
                MinimumSize = "1KB",
                CompressionLevel = "Optimal",
                ETagSupport = true
            },
            DatabaseOptimization = new DatabaseOptimizationDocumentation
            {
                Description = "Automated database performance monitoring and optimization",
                SlowQueryThreshold = "2 seconds",
                CriticalQueryThreshold = "5 seconds",
                AutoMaintenanceEnabled = true,
                ConnectionPoolOptimization = true,
                QueryOptimizationSuggestions = true
            }
        };

        return Task.FromResult(performanceDoc);
    }

    /// <summary>
    /// Gets security documentation
    /// </summary>
    private SecurityDocumentation GetSecurityDocumentation()
    {
        return new SecurityDocumentation
        {
            Authentication = new AuthenticationDocumentation
            {
                Type = "JWT Bearer Token",
                Description = "Enhanced JWT authentication with replay protection and suspicious activity detection",
                TokenExpiry = "1 hour",
                RefreshTokenSupport = true,
                ReplayProtection = true,
                SuspiciousActivityDetection = true
            },
            RateLimiting = new SecurityRateLimitingDocumentation
            {
                Description = "Advanced rate limiting with automatic IP blocking",
                MultiDimensionalLimits = true,
                AutomaticBlocking = true,
                ThreatDetection = true,
                SecurityEventLogging = true
            },
            SecurityHeaders = new SecurityHeadersDocumentation
            {
                Description = "Comprehensive security headers for production deployment",
                HSTS = true,
                ContentSecurityPolicy = true,
                XFrameOptions = true,
                XContentTypeOptions = true,
                ReferrerPolicy = true
            }
        };
    }

    /// <summary>
    /// Gets architecture documentation
    /// </summary>
    private ArchitectureDocumentation GetArchitectureDocumentation()
    {
        return new ArchitectureDocumentation
        {
            Overview = "Clean Architecture with Domain-Driven Design principles",
            Layers = new List<LayerDocumentation>
            {
                new() { Name = "API Layer", Description = "Controllers, middleware, and API-specific services" },
                new() { Name = "Core Layer", Description = "Domain entities, interfaces, and business logic" },
                new() { Name = "Infrastructure Layer", Description = "Data access, external services, and infrastructure concerns" },
                new() { Name = "Worker Layer", Description = "Background services for Indicator monitoring and alerting" }
            },
            Patterns = new List<PatternDocumentation>
            {
                new() { Name = "CQRS", Description = "Command Query Responsibility Segregation for read/write operations" },
                new() { Name = "Repository Pattern", Description = "Data access abstraction with Entity Framework Core" },
                new() { Name = "Mediator Pattern", Description = "MediatR for decoupled request/response handling" },
                new() { Name = "Result Pattern", Description = "Result<T> for error handling without exceptions" }
            },
            Technologies = new List<TechnologyDocumentation>
            {
                new() { Name = ".NET 8", Description = "Latest .NET framework with performance improvements" },
                new() { Name = "Entity Framework Core", Description = "ORM for database access with SQL Server" },
                new() { Name = "SignalR", Description = "Real-time communication for live updates" },
                new() { Name = "Swagger/OpenAPI", Description = "API documentation and testing interface" }
            }
        };
    }

    // Helper methods for getting examples
    private string GetApiVersion() => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    private string GetBaseUrl() => _configuration["ApiSettings:BaseUrl"] ?? "https://api.monitoringgrid.com";

    private string GetIndicatorListExample() => JsonSerializer.Serialize(new
    {
        isSuccess = true,
        data = new
        {
            items = new[]
            {
                new
                {
                    id = 1,
                    name = "Database Response Time",
                    description = "Average database response time in milliseconds",
                    isActive = true,
                    priority = 3,
                    category = "Performance",
                    lastExecuted = "2024-12-01T10:30:00Z",
                    nextExecution = "2024-12-01T11:00:00Z"
                }
            },
            totalCount = 25,
            page = 1,
            pageSize = 10,
            totalPages = 3
        },
        correlationId = "12345678-1234-1234-1234-123456789012",
        timestamp = DateTime.UtcNow
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetIndicatorExecuteRequestExample() => JsonSerializer.Serialize(new
    {
        parameters = new
        {
            timeRange = "1h",
            threshold = 100
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetIndicatorExecuteResponseExample() => JsonSerializer.Serialize(new
    {
        isSuccess = true,
        data = new
        {
            executionId = "exec_12345",
            result = new
            {
                value = 85.5,
                status = "Normal",
                threshold = 100,
                executedAt = DateTime.UtcNow
            }
        },
        correlationId = "12345678-1234-1234-1234-123456789012"
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetAsyncExecuteExample() => JsonSerializer.Serialize(new
    {
        isSuccess = true,
        data = new
        {
            executionId = "exec_12345",
            status = "Running",
            estimatedCompletion = DateTime.UtcNow.AddMinutes(2)
        },
        correlationId = "12345678-1234-1234-1234-123456789012"
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetAlertListExample() => JsonSerializer.Serialize(new
    {
        isSuccess = true,
        data = new
        {
            items = new[]
            {
                new
                {
                    id = 1,
                    indicatorID = 1,
                    severity = "High",
                    status = "Active",
                    message = "Database response time exceeded threshold",
                    createdAt = "2024-12-01T10:35:00Z"
                }
            },
            totalCount = 5,
            page = 1,
            pageSize = 10
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetApiInfoExample() => JsonSerializer.Serialize(new
    {
        name = "MonitoringGrid API",
        version = GetApiVersion(),
        environment = "Production",
        timestamp = DateTime.UtcNow,
        features = new[]
        {
            "Advanced Caching",
            "Rate Limiting",
            "Security Hardening",
            "Performance Optimization"
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetErrorExample(string message) => JsonSerializer.Serialize(new
    {
        isSuccess = false,
        message,
        errors = new[] { message },
        correlationId = "12345678-1234-1234-1234-123456789012",
        timestamp = DateTime.UtcNow
    }, new JsonSerializerOptions { WriteIndented = true });

    private string GetRateLimitExample() => JsonSerializer.Serialize(new
    {
        isSuccess = false,
        message = "Rate limit exceeded",
        errors = new[] { "Too many requests. Please try again later." },
        correlationId = "12345678-1234-1234-1234-123456789012",
        retryAfter = 60,
        resetTime = DateTime.UtcNow.AddMinutes(1)
    }, new JsonSerializerOptions { WriteIndented = true });
}
