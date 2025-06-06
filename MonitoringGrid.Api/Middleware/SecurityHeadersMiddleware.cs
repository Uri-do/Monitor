using Microsoft.Extensions.Options;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Middleware to add security headers for production security
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next, 
        IOptions<SecurityHeadersOptions> options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        await _next(context);

        // Log security violations if any
        LogSecurityViolations(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        // Content Security Policy
        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            response.Headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // X-Content-Type-Options
        response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options
        response.Headers["X-Frame-Options"] = _options.XFrameOptions;

        // X-XSS-Protection
        response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer Policy
        response.Headers["Referrer-Policy"] = _options.ReferrerPolicy;

        // Permissions Policy
        if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
        {
            response.Headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Strict Transport Security (HTTPS only)
        if (context.Request.IsHttps && _options.EnableHSTS)
        {
            response.Headers["Strict-Transport-Security"] = $"max-age={_options.HSTSMaxAge}; includeSubDomains";
        }

        // Remove server information
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");

        // Add custom security headers
        foreach (var header in _options.CustomHeaders)
        {
            response.Headers[header.Key] = header.Value;
        }

        _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
    }

    private void LogSecurityViolations(HttpContext context)
    {
        // Check for potential security issues
        var request = context.Request;
        var response = context.Response;

        // Log if sensitive data might be exposed
        if (response.StatusCode == 500 && _options.LogSecurityViolations)
        {
            _logger.LogWarning("Internal server error occurred for {Path} from {IP} - potential information disclosure",
                request.Path, context.Connection.RemoteIpAddress);
        }

        // Log suspicious requests
        if (request.Headers.ContainsKey("X-Forwarded-For") && _options.LogSuspiciousRequests)
        {
            var forwardedFor = request.Headers["X-Forwarded-For"].ToString();
            if (forwardedFor.Contains("script") || forwardedFor.Contains("<"))
            {
                _logger.LogWarning("Suspicious X-Forwarded-For header detected: {Header} from {IP}",
                    forwardedFor, context.Connection.RemoteIpAddress);
            }
        }

        // Log potential XSS attempts
        if (request.QueryString.HasValue && _options.LogSuspiciousRequests)
        {
            var queryString = request.QueryString.Value;
            if (queryString.Contains("<script") || queryString.Contains("javascript:") || queryString.Contains("onload="))
            {
                _logger.LogWarning("Potential XSS attempt detected in query string: {QueryString} from {IP}",
                    queryString, context.Connection.RemoteIpAddress);
            }
        }
    }
}

/// <summary>
/// Configuration options for security headers
/// </summary>
public class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>
    /// Content Security Policy header value
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "connect-src 'self' wss: https:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";

    /// <summary>
    /// X-Frame-Options header value
    /// </summary>
    public string XFrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Referrer-Policy header value
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Permissions-Policy header value
    /// </summary>
    public string PermissionsPolicy { get; set; } = 
        "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

    /// <summary>
    /// Enable HTTP Strict Transport Security
    /// </summary>
    public bool EnableHSTS { get; set; } = true;

    /// <summary>
    /// HSTS max age in seconds (default: 1 year)
    /// </summary>
    public int HSTSMaxAge { get; set; } = 31536000;

    /// <summary>
    /// Log security violations
    /// </summary>
    public bool LogSecurityViolations { get; set; } = true;

    /// <summary>
    /// Log suspicious requests
    /// </summary>
    public bool LogSuspiciousRequests { get; set; } = true;

    /// <summary>
    /// Custom security headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Extension methods for adding security headers middleware
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Add security headers middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Add security headers middleware with custom options
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Action<SecurityHeadersOptions> configureOptions)
    {
        var options = new SecurityHeadersOptions();
        configureOptions(options);
        
        return app.UseMiddleware<SecurityHeadersMiddleware>(Options.Create(options));
    }

    /// <summary>
    /// Configure security headers options
    /// </summary>
    public static IServiceCollection ConfigureSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(configuration.GetSection(SecurityHeadersOptions.SectionName));
        return services;
    }
}

/// <summary>
/// Security headers configuration for different environments
/// </summary>
public static class SecurityHeadersProfiles
{
    /// <summary>
    /// Development environment security headers (more permissive)
    /// </summary>
    public static SecurityHeadersOptions Development => new()
    {
        ContentSecurityPolicy = 
            "default-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https:; " +
            "style-src 'self' 'unsafe-inline' https:; " +
            "img-src 'self' data: https: blob:; " +
            "font-src 'self' https: data:; " +
            "connect-src 'self' wss: ws: https: http:;",
        XFrameOptions = "SAMEORIGIN",
        EnableHSTS = false,
        LogSecurityViolations = false,
        LogSuspiciousRequests = false
    };

    /// <summary>
    /// Production environment security headers (strict)
    /// </summary>
    public static SecurityHeadersOptions Production => new()
    {
        ContentSecurityPolicy = 
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net; " +
            "style-src 'self' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "connect-src 'self' wss: https:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "upgrade-insecure-requests;",
        XFrameOptions = "DENY",
        EnableHSTS = true,
        HSTSMaxAge = 31536000, // 1 year
        LogSecurityViolations = true,
        LogSuspiciousRequests = true,
        CustomHeaders = new Dictionary<string, string>
        {
            ["X-Robots-Tag"] = "noindex, nofollow",
            ["Cache-Control"] = "no-store, no-cache, must-revalidate, private"
        }
    };

    /// <summary>
    /// API-specific security headers
    /// </summary>
    public static SecurityHeadersOptions ApiOnly => new()
    {
        ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none';",
        XFrameOptions = "DENY",
        EnableHSTS = true,
        LogSecurityViolations = true,
        LogSuspiciousRequests = true,
        CustomHeaders = new Dictionary<string, string>
        {
            ["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet",
            ["Cache-Control"] = "no-store, no-cache, must-revalidate, private",
            ["X-API-Version"] = "1.0"
        }
    };
}
