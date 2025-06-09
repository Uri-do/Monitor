using MonitoringGrid.Api.Common;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text.Json;
using System.Diagnostics;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Enhanced global exception handling middleware with structured logging and correlation tracking
/// </summary>
public class EnhancedExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnhancedExceptionHandlingMiddleware> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly IWebHostEnvironment _environment;

    public EnhancedExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<EnhancedExceptionHandlingMiddleware> logger,
        ICorrelationIdService correlationIdService,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _correlationIdService = correlationIdService;
        _environment = environment;
    }

    /// <summary>
    /// Process the HTTP request and handle any unhandled exceptions
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handle exceptions and return appropriate API responses
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var traceId = _correlationIdService.GetTraceId();

        // Log the exception with structured data
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            ["ExceptionType"] = exception.GetType().Name
        });

        _logger.LogError(exception, "Unhandled exception occurred during request processing");

        // Determine response based on exception type
        var (statusCode, response) = CreateErrorResponse(exception, correlationId, traceId);

        // Set response properties
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Serialize and write response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Create appropriate error response based on exception type
    /// </summary>
    private (HttpStatusCode statusCode, ApiResponse response) CreateErrorResponse(
        Exception exception, 
        string correlationId, 
        string traceId)
    {
        var metadata = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["ExceptionType"] = exception.GetType().Name,
            ["Timestamp"] = DateTime.UtcNow
        };

        // Add stack trace in development environment
        if (_environment.IsDevelopment())
        {
            metadata["StackTrace"] = exception.StackTrace ?? "No stack trace available";
            metadata["InnerException"] = exception.InnerException?.Message;
        }

        return exception switch
        {
            ArgumentNullException nullEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Failure($"Required parameter is missing: {nullEx.ParamName}", metadata)
            ),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Failure(argEx.Message, metadata)
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ApiResponse.Failure("Access denied. Please check your authentication credentials.", metadata)
            ),

            SecurityException => (
                HttpStatusCode.Forbidden,
                ApiResponse.Failure("Access forbidden. You don't have permission to perform this action.", metadata)
            ),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse.Failure("The requested resource was not found.", metadata)
            ),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.Conflict,
                ApiResponse.Failure(invalidOpEx.Message, metadata)
            ),

            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                ApiResponse.Failure("The request timed out. Please try again later.", metadata)
            ),

            NotSupportedException => (
                HttpStatusCode.NotImplemented,
                ApiResponse.Failure("The requested operation is not supported.", metadata)
            ),

            HttpRequestException httpEx => (
                HttpStatusCode.BadGateway,
                ApiResponse.Failure("External service error. Please try again later.", metadata)
            ),

            TaskCanceledException => (
                HttpStatusCode.RequestTimeout,
                ApiResponse.Failure("The request was cancelled or timed out.", metadata)
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse.Failure(
                    _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An internal server error occurred. Please try again later.",
                    metadata)
            )
        };
    }
}

/// <summary>
/// Extension methods for enhanced exception handling middleware registration
/// </summary>
public static class EnhancedExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds enhanced exception handling middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseEnhancedExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EnhancedExceptionHandlingMiddleware>();
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : Exception
    {
        public string RuleName { get; }
        public Dictionary<string, object> Context { get; }

        public BusinessRuleException(string ruleName, string message, Dictionary<string, object>? context = null)
            : base(message)
        {
            RuleName = ruleName;
            Context = context ?? new Dictionary<string, object>();
        }
    }

/// <summary>
/// Exception thrown when a resource is not found
/// </summary>
public class ResourceNotFoundException : Exception
    {
        public string ResourceType { get; }
        public string ResourceId { get; }

        public ResourceNotFoundException(string resourceType, string resourceId)
            : base($"{resourceType} with ID '{resourceId}' was not found")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
    }

/// <summary>
/// Exception thrown when a resource already exists
/// </summary>
public class ResourceAlreadyExistsException : Exception
    {
        public string ResourceType { get; }
        public string ResourceId { get; }

        public ResourceAlreadyExistsException(string resourceType, string resourceId)
            : base($"{resourceType} with ID '{resourceId}' already exists")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
    }

/// <summary>
/// Exception thrown when an external service is unavailable
/// </summary>
public class ExternalServiceException : Exception
    {
        public string ServiceName { get; }
        public string? ServiceEndpoint { get; }

        public ExternalServiceException(string serviceName, string message, string? serviceEndpoint = null, Exception? innerException = null)
            : base(message, innerException)
        {
            ServiceName = serviceName;
            ServiceEndpoint = serviceEndpoint;
        }
    }

/// <summary>
/// Exception thrown when rate limiting is exceeded
/// </summary>
public class RateLimitExceededException : Exception
    {
        public string LimitType { get; }
        public int Limit { get; }
        public TimeSpan RetryAfter { get; }

        public RateLimitExceededException(string limitType, int limit, TimeSpan retryAfter)
            : base($"Rate limit exceeded for {limitType}. Limit: {limit}. Retry after: {retryAfter}")
        {
            LimitType = limitType;
            Limit = limit;
            RetryAfter = retryAfter;
        }
    }

/// <summary>
/// Exception handling service for consistent exception processing
/// </summary>
public interface IExceptionHandlingService
{
    /// <summary>
    /// Logs an exception with structured data
    /// </summary>
    void LogException(Exception exception, string? additionalContext = null);

    /// <summary>
    /// Creates a standardized error response from an exception
    /// </summary>
    ApiResponse CreateErrorResponse(Exception exception);

    /// <summary>
    /// Determines if an exception should be retried
    /// </summary>
    bool ShouldRetry(Exception exception);
}

/// <summary>
/// Implementation of exception handling service
/// </summary>
public class ExceptionHandlingService : IExceptionHandlingService
{
    private readonly ILogger<ExceptionHandlingService> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public ExceptionHandlingService(
        ILogger<ExceptionHandlingService> logger,
        ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    /// <summary>
    /// Logs an exception with correlation context
    /// </summary>
    public void LogException(Exception exception, string? additionalContext = null)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var traceId = _correlationIdService.GetTraceId();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["ExceptionType"] = exception.GetType().Name,
            ["AdditionalContext"] = additionalContext ?? "None"
        });

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    public ApiResponse CreateErrorResponse(Exception exception)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        
        return ApiResponse.Failure(exception.Message, new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ExceptionType"] = exception.GetType().Name
        });
    }

    /// <summary>
    /// Determines if an exception indicates a retryable error
    /// </summary>
    public bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            TaskCanceledException => true,
            HttpRequestException => true,
            SocketException => true,
            _ => false
        };
    }
}
