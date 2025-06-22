using MonitoringGrid.Api.Common;
using System.Net;
using System.Text.Json;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Basic global exception handling middleware
/// </summary>
public class BasicExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BasicExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public BasicExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<BasicExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
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
        // Get correlation ID if available
        var correlationId = context.Items.TryGetValue("CorrelationId", out var corrId)
            ? corrId?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        // Log the exception
        _logger.LogError(exception, "Unhandled exception occurred during request processing. Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}",
            context.Request.Path, context.Request.Method, correlationId);

        // Determine response based on exception type
        var (statusCode, response) = CreateErrorResponse(exception, correlationId);

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
        string correlationId)
    {
        var metadata = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Timestamp"] = DateTime.UtcNow
        };

        // Add exception details in development environment
        if (_environment.IsDevelopment())
        {
            metadata["ExceptionType"] = exception.GetType().Name;
            metadata["StackTrace"] = exception.StackTrace ?? "No stack trace available";
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
/// Extension methods for basic exception handling middleware registration
/// </summary>
public static class BasicExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds basic exception handling middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseBasicExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BasicExceptionHandlingMiddleware>();
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
