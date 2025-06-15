using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using System.Diagnostics;

namespace MonitoringGrid.Api.Versioning;

/// <summary>
/// API versioning configuration extensions
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Adds comprehensive API versioning support
    /// </summary>
    public static IServiceCollection AddAdvancedApiVersioning(this IServiceCollection services)
    {
        // Add API versioning
        services.AddApiVersioning(options =>
        {
            // Default version
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Version reading strategies
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),           // /api/v1/indicators
                new HeaderApiVersionReader("X-Version"),    // X-Version: 1.0
                new QueryStringApiVersionReader("version"), // ?version=1.0
                new MediaTypeApiVersionReader("version")    // Accept: application/json;version=1.0
            );

            // Version selection strategy
            options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);

            // Error handling
            options.ErrorResponses = new ApiVersioningErrorResponseProvider();
        });

        // Add versioned API explorer
        services.AddVersionedApiExplorer(options =>
        {
            // Group name format for Swagger
            options.GroupNameFormat = "'v'VVV";
            
            // Substitute version in URL
            options.SubstituteApiVersionInUrl = true;
            
            // Assume default version when unspecified
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        return services;
    }

    /// <summary>
    /// Configures API versioning middleware
    /// </summary>
    public static IApplicationBuilder UseAdvancedApiVersioning(this IApplicationBuilder app)
    {
        // Add version-aware routing
        app.UseMiddleware<ApiVersionMiddleware>();
        
        return app;
    }
}

/// <summary>
/// Custom error response provider for API versioning
/// </summary>
public class ApiVersioningErrorResponseProvider : IErrorResponseProvider
{
    public IActionResult CreateResponse(ErrorResponseContext context)
    {
        var error = new
        {
            error = "api_version_error",
            message = context.Message,
            code = context.ErrorCode,
            supportedVersions = new[] { "1.0", "2.0" },
            timestamp = DateTime.UtcNow,
            traceId = Activity.Current?.Id ?? context.Request.HttpContext.TraceIdentifier
        };

        return new ObjectResult(error)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }
}

/// <summary>
/// Middleware for API version processing and validation
/// </summary>
public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiVersionMiddleware> _logger;
    private readonly IApiVersionReader _versionReader;

    public ApiVersionMiddleware(
        RequestDelegate next,
        ILogger<ApiVersionMiddleware> logger,
        IApiVersionReader versionReader)
    {
        _next = next;
        _logger = logger;
        _versionReader = versionReader;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract and validate API version
        var versionFeature = context.Features.Get<IApiVersioningFeature>();
        if (versionFeature != null)
        {
            var requestedVersion = versionFeature.RequestedApiVersion;

            // Log version information
            _logger.LogDebug("API request for version {Version} on path {Path}",
                requestedVersion?.ToString() ?? "default",
                context.Request.Path);

            // Add version headers to response
            if (requestedVersion != null)
            {
                context.Response.Headers["X-API-Version"] = requestedVersion.ToString();
            }

            // Add supported versions header
            context.Response.Headers["X-Supported-Versions"] = "1.0, 2.0";
            
            // Add deprecation warnings for old versions
            if (requestedVersion != null && requestedVersion.MajorVersion < 2)
            {
                context.Response.Headers["X-API-Deprecated"] = "true";
                context.Response.Headers["X-API-Sunset"] = "2025-12-31";
                context.Response.Headers["X-API-Deprecation-Info"] = "This version will be deprecated. Please upgrade to v2.0";
            }
        }

        await _next(context);
    }
}

/// <summary>
/// API version constants
/// </summary>
public static class ApiVersions
{
    public static readonly ApiVersion V1_0 = new(1, 0);
    public static readonly ApiVersion V2_0 = new(2, 0);
    
    public static class Headers
    {
        public const string Version = "X-Version";
        public const string SupportedVersions = "X-Supported-Versions";
        public const string Deprecated = "X-API-Deprecated";
        public const string Sunset = "X-API-Sunset";
        public const string DeprecationInfo = "X-API-Deprecation-Info";
    }
}

/// <summary>
/// Version-aware base controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class VersionedApiController : ControllerBase
{
    /// <summary>
    /// Gets the current API version
    /// </summary>
    protected ApiVersion CurrentVersion => HttpContext.GetRequestedApiVersion() ?? ApiVersions.V1_0;

    /// <summary>
    /// Checks if the current version is deprecated
    /// </summary>
    protected bool IsDeprecatedVersion => CurrentVersion.MajorVersion < 2;

    /// <summary>
    /// Creates a version-aware response
    /// </summary>
    protected IActionResult VersionedOk<T>(T data)
    {
        var response = new
        {
            data,
            version = CurrentVersion.ToString(),
            deprecated = IsDeprecatedVersion,
            timestamp = DateTime.UtcNow
        };

        if (IsDeprecatedVersion)
        {
            Response.Headers["X-API-Deprecated"] = "true";
            Response.Headers["X-API-Sunset"] = "2025-12-31";
        }

        return Ok(response);
    }

    /// <summary>
    /// Creates a version-aware error response
    /// </summary>
    protected IActionResult VersionedError(string message, int statusCode = 400)
    {
        var response = new
        {
            error = message,
            version = CurrentVersion.ToString(),
            timestamp = DateTime.UtcNow,
            traceId = HttpContext.TraceIdentifier
        };

        return StatusCode(statusCode, response);
    }
}

/// <summary>
/// Attribute to mark API endpoints as deprecated
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiDeprecatedAttribute : Attribute
{
    public string? SunsetDate { get; set; }
    public string? Message { get; set; }
    public string? AlternativeEndpoint { get; set; }

    public ApiDeprecatedAttribute(string? sunsetDate = null, string? message = null)
    {
        SunsetDate = sunsetDate;
        Message = message;
    }
}

/// <summary>
/// Filter to handle deprecated API endpoints
/// </summary>
public class ApiDeprecationFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var deprecatedAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<ApiDeprecatedAttribute>()
            .FirstOrDefault();

        if (deprecatedAttribute != null)
        {
            context.HttpContext.Response.Headers["X-API-Deprecated"] = "true";
            
            if (!string.IsNullOrEmpty(deprecatedAttribute.SunsetDate))
            {
                context.HttpContext.Response.Headers["X-API-Sunset"] = deprecatedAttribute.SunsetDate;
            }

            if (!string.IsNullOrEmpty(deprecatedAttribute.Message))
            {
                context.HttpContext.Response.Headers["X-API-Deprecation-Info"] = deprecatedAttribute.Message;
            }

            if (!string.IsNullOrEmpty(deprecatedAttribute.AlternativeEndpoint))
            {
                context.HttpContext.Response.Headers["X-API-Alternative"] = deprecatedAttribute.AlternativeEndpoint;
            }
        }

        base.OnActionExecuting(context);
    }
}
