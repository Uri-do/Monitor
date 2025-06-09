using System.Diagnostics;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Middleware to handle correlation IDs for request tracking and distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string TraceIdHeaderName = "X-Trace-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Process the HTTP request and ensure correlation ID is present
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        context.Response.Headers.TryAdd(TraceIdHeaderName, traceId);

        // Add to HttpContext items for access throughout the request pipeline
        context.Items["CorrelationId"] = correlationId;
        context.Items["TraceId"] = traceId;

        // Add to logging scope
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        });

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Request started: {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation("Request completed: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
                context.Request.Method, 
                context.Request.Path, 
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Request failed: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Gets correlation ID from request headers or creates a new one
    /// </summary>
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Create new correlation ID
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension methods for correlation ID middleware registration
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

/// <summary>
/// Service for accessing correlation ID throughout the application
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string GetCorrelationId();

    /// <summary>
    /// Gets the current trace ID
    /// </summary>
    string GetTraceId();
}

/// <summary>
/// Implementation of correlation ID service
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current correlation ID from HTTP context
    /// </summary>
    public string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
        {
            return correlationId?.ToString() ?? Guid.NewGuid().ToString();
        }

        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the current trace ID from HTTP context
    /// </summary>
    public string GetTraceId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue("TraceId", out var traceId) == true)
        {
            return traceId?.ToString() ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class CorrelationIdServiceExtensions
{
    /// <summary>
    /// Adds correlation ID services to the DI container
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdService, CorrelationIdService>();
        return services;
    }
}
