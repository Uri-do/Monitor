using System.Diagnostics;
using System.Net;
using System.Text.Json;
using MonitoringGrid.Core.Exceptions;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Request: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = new ApiErrorResponse();

        switch (exception)
        {
            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = notFoundEx.Message;
                response.Details = notFoundEx.Details;
                break;

            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Validation failed";
                response.Details = validationEx.Message;
                response.Errors = validationEx.Errors?.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.ToArray()
                );
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                response.Details = unauthorizedEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Message = "Access forbidden";
                response.Details = forbiddenEx.Message;
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = conflictEx.Message;
                response.Details = conflictEx.Details;
                break;

            case BusinessRuleException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Business rule violation";
                response.Details = businessEx.Message;
                break;

            case ExternalServiceException externalEx:
                response.StatusCode = (int)HttpStatusCode.BadGateway;
                response.Message = "External service error";
                response.Details = "An external service is currently unavailable";
                break;

            case TimeoutException timeoutEx:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Message = "Request timeout";
                response.Details = "The request took too long to process";
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid argument";
                response.Details = argEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid operation";
                response.Details = invalidOpEx.Message;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An error occurred while processing your request";
                response.Details = "Please try again later or contact support if the problem persists";
                break;
        }

        context.Response.StatusCode = response.StatusCode;
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Standardized API error response model
/// </summary>
public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public string TraceId { get; set; } = Activity.Current?.Id ?? Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
