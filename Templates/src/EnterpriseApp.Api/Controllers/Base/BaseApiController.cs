using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Api.Controllers.Base;

/// <summary>
/// Base controller for all API controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// MediatR instance for CQRS operations
    /// </summary>
    protected readonly IMediator Mediator;

    /// <summary>
    /// Logger instance
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the BaseApiController
    /// </summary>
    protected BaseApiController(IMediator mediator, ILogger logger)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles Result<T> and converts to appropriate ActionResult
    /// </summary>
    /// <typeparam name="T">Type of the result value</typeparam>
    /// <param name="result">The result to handle</param>
    /// <returns>ActionResult based on the result</returns>
    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return HandleError(result.Error);
    }

    /// <summary>
    /// Handles Result and converts to appropriate ActionResult
    /// </summary>
    /// <param name="result">The result to handle</param>
    /// <returns>ActionResult based on the result</returns>
    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return HandleError(result.Error);
    }

    /// <summary>
    /// Handles errors and converts to appropriate ActionResult
    /// </summary>
    /// <param name="error">The error to handle</param>
    /// <returns>ActionResult based on the error type</returns>
    protected ActionResult HandleError(Error error)
    {
        var statusCode = error.ToHttpStatusCode();
        
        var problemDetails = new ProblemDetails
        {
            Title = GetErrorTitle(error.Type),
            Detail = error.Message,
            Status = statusCode,
            Type = GetErrorType(error.Type),
            Instance = HttpContext.Request.Path
        };

        // Add error code to extensions
        problemDetails.Extensions["errorCode"] = error.Code;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        Logger.LogWarning("API Error: {ErrorType} - {ErrorCode}: {ErrorMessage}", 
            error.Type, error.Code, error.Message);

        return StatusCode(statusCode, problemDetails);
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    protected string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               User.FindFirst("sub")?.Value ??
               User.FindFirst("userId")?.Value;
    }

    /// <summary>
    /// Gets the current username from claims
    /// </summary>
    /// <returns>Username or null if not available</returns>
    protected string? GetCurrentUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ??
               User.FindFirst("username")?.Value ??
               User.FindFirst("preferred_username")?.Value;
    }

    /// <summary>
    /// Gets the current user's email from claims
    /// </summary>
    /// <returns>Email or null if not available</returns>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ??
               User.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the current user's roles from claims
    /// </summary>
    /// <returns>List of roles</returns>
    protected List<string> GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role)
                  .Select(c => c.Value)
                  .ToList();
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role</returns>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role);
    }

    /// <summary>
    /// Checks if the current user has any of the specified roles
    /// </summary>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has any of the roles</returns>
    protected bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => User.IsInRole(role));
    }

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    /// <returns>Client IP address</returns>
    protected string? GetClientIpAddress()
    {
        // Check for forwarded IP first (when behind proxy/load balancer)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the user agent string
    /// </summary>
    /// <returns>User agent string</returns>
    protected string? GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Gets the correlation ID from headers or generates a new one
    /// </summary>
    /// <returns>Correlation ID</returns>
    protected string GetCorrelationId()
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                           HttpContext.Request.Headers["X-Request-ID"].FirstOrDefault() ??
                           HttpContext.TraceIdentifier;

        return correlationId;
    }

    /// <summary>
    /// Checks if the request is from a mobile device
    /// </summary>
    /// <returns>True if mobile device</returns>
    protected bool IsMobileDevice()
    {
        var userAgent = GetUserAgent()?.ToLower();
        if (string.IsNullOrEmpty(userAgent))
            return false;

        var mobileKeywords = new[] { "mobile", "android", "iphone", "ipad", "tablet" };
        return mobileKeywords.Any(keyword => userAgent.Contains(keyword));
    }

    /// <summary>
    /// Gets the request origin
    /// </summary>
    /// <returns>Origin URL</returns>
    protected string? GetOrigin()
    {
        return HttpContext.Request.Headers["Origin"].FirstOrDefault() ??
               HttpContext.Request.Headers["Referer"].FirstOrDefault();
    }

    /// <summary>
    /// Creates a validation problem details response
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <returns>ValidationProblemDetails</returns>
    protected ValidationProblemDetails CreateValidationProblem(Dictionary<string, string[]> errors)
    {
        var problemDetails = new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred",
            Status = StatusCodes.Status422UnprocessableEntity,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return problemDetails;
    }

    /// <summary>
    /// Gets error title based on error type
    /// </summary>
    private static string GetErrorTitle(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.BusinessRule => "Business Rule Violation",
            ErrorType.External => "External Service Error",
            ErrorType.Critical => "Critical Error",
            _ => "An Error Occurred"
        };
    }

    /// <summary>
    /// Gets error type URI based on error type
    /// </summary>
    private static string GetErrorType(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            ErrorType.BusinessRule => "https://tools.ietf.org/html/rfc4918#section-11.2",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    /// <summary>
    /// Logs API action with context information
    /// </summary>
    /// <param name="action">Action being performed</param>
    /// <param name="additionalData">Additional data to log</param>
    protected void LogApiAction(string action, object? additionalData = null)
    {
        var logData = new
        {
            Action = action,
            UserId = GetCurrentUserId(),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            CorrelationId = GetCorrelationId(),
            AdditionalData = additionalData
        };

        Logger.LogInformation("API Action: {Action} by User: {UserId}", action, GetCurrentUserId());
        Logger.LogDebug("API Action Details: {@LogData}", logData);
    }
}
