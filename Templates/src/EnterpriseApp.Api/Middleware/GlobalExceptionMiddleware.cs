using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionMiddleware
    /// </summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions and returns appropriate responses
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var problemDetails = CreateProblemDetails(context, exception);
        response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await response.WriteAsync(json);
    }

    /// <summary>
    /// Creates problem details based on the exception type
    /// </summary>
    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Status = GetStatusCode(exception),
            Title = GetTitle(exception),
            Detail = GetDetail(exception),
            Type = GetType(exception)
        };

        // Add common extensions
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = GetCorrelationId(context);

        // Add exception type for debugging
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        // Add specific error information based on exception type
        switch (exception)
        {
            case ArgumentException argEx:
                problemDetails.Extensions["parameterName"] = argEx.ParamName;
                break;

            case UnauthorizedAccessException:
                problemDetails.Extensions["authenticationRequired"] = true;
                break;

            case TimeoutException:
                problemDetails.Extensions["timeout"] = true;
                break;

            case TaskCanceledException:
                problemDetails.Extensions["cancelled"] = true;
                break;

            case InvalidOperationException invalidOpEx:
                problemDetails.Extensions["operationContext"] = invalidOpEx.Source;
                break;
        }

        return problemDetails;
    }

    /// <summary>
    /// Gets the HTTP status code for the exception
    /// </summary>
    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            SecurityException => (int)HttpStatusCode.Forbidden,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            FileNotFoundException => (int)HttpStatusCode.NotFound,
            DirectoryNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException => (int)HttpStatusCode.Conflict,
            NotSupportedException => (int)HttpStatusCode.BadRequest,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            TaskCanceledException => (int)HttpStatusCode.RequestTimeout,
            OperationCanceledException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    /// <summary>
    /// Gets the title for the exception
    /// </summary>
    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Invalid Argument",
            ArgumentNullException => "Missing Required Parameter",
            UnauthorizedAccessException => "Unauthorized Access",
            SecurityException => "Access Forbidden",
            KeyNotFoundException => "Resource Not Found",
            FileNotFoundException => "File Not Found",
            DirectoryNotFoundException => "Directory Not Found",
            InvalidOperationException => "Invalid Operation",
            NotSupportedException => "Operation Not Supported",
            TimeoutException => "Request Timeout",
            TaskCanceledException => "Request Cancelled",
            OperationCanceledException => "Operation Cancelled",
            _ => "Internal Server Error"
        };
    }

    /// <summary>
    /// Gets the detail message for the exception
    /// </summary>
    private string GetDetail(Exception exception)
    {
        // In production, don't expose internal exception details
        if (!_environment.IsDevelopment())
        {
            return exception switch
            {
                ArgumentException => "One or more arguments are invalid",
                ArgumentNullException => "A required parameter was not provided",
                UnauthorizedAccessException => "Authentication is required to access this resource",
                SecurityException => "You do not have permission to perform this action",
                KeyNotFoundException => "The requested resource was not found",
                FileNotFoundException => "The requested file was not found",
                DirectoryNotFoundException => "The requested directory was not found",
                InvalidOperationException => "The requested operation cannot be performed at this time",
                NotSupportedException => "The requested operation is not supported",
                TimeoutException => "The request timed out",
                TaskCanceledException => "The request was cancelled",
                OperationCanceledException => "The operation was cancelled",
                _ => "An error occurred while processing your request"
            };
        }

        return exception.Message;
    }

    /// <summary>
    /// Gets the type URI for the exception
    /// </summary>
    private static string GetType(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            UnauthorizedAccessException => "https://tools.ietf.org/html/rfc7235#section-3.1",
            SecurityException => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            KeyNotFoundException => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            FileNotFoundException => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            DirectoryNotFoundException => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            InvalidOperationException => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            TimeoutException => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    /// <summary>
    /// Gets the correlation ID from the request
    /// </summary>
    private static string GetCorrelationId(HttpContext context)
    {
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
               context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
               context.TraceIdentifier;
    }
}

/// <summary>
/// Security exception for authorization failures
/// </summary>
public class SecurityException : Exception
{
    /// <summary>
    /// Initializes a new instance of the SecurityException
    /// </summary>
    public SecurityException() : base("Access denied")
    {
    }

    /// <summary>
    /// Initializes a new instance of the SecurityException with a message
    /// </summary>
    public SecurityException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the SecurityException with a message and inner exception
    /// </summary>
    public SecurityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
