using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EnterpriseApp.Api.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly RequestResponseLoggingOptions _options;

    /// <summary>
    /// Initializes a new instance of the RequestResponseLoggingMiddleware
    /// </summary>
    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        RequestResponseLoggingOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldLog(context))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetOrCreateCorrelationId(context);

        // Log request
        await LogRequestAsync(context, correlationId);

        // Capture response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            context.Response.Body = originalResponseBodyStream;
        }
    }

    /// <summary>
    /// Determines if the request should be logged
    /// </summary>
    private bool ShouldLog(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // Skip health check endpoints
        if (path?.Contains("/health") == true)
            return false;

        // Skip swagger endpoints in production
        if (!_options.LogSwaggerRequests && path?.Contains("/swagger") == true)
            return false;

        // Skip static files
        if (_options.SkipStaticFiles && IsStaticFile(path))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the path is for a static file
    /// </summary>
    private static bool IsStaticFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
        return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets or creates a correlation ID for the request
    /// </summary>
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                           context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
                           Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        return correlationId;
    }

    /// <summary>
    /// Logs the HTTP request
    /// </summary>
    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        var requestLog = new
        {
            CorrelationId = correlationId,
            Method = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            Headers = GetHeaders(request.Headers),
            UserAgent = request.Headers["User-Agent"].FirstOrDefault(),
            IpAddress = GetClientIpAddress(context),
            UserId = GetUserId(context),
            Body = await GetRequestBodyAsync(request)
        };

        _logger.LogInformation("HTTP Request: {Method} {Path} - CorrelationId: {CorrelationId}",
            request.Method, request.Path, correlationId);

        if (_options.LogRequestDetails)
        {
            _logger.LogDebug("Request Details: {@RequestLog}", requestLog);
        }
    }

    /// <summary>
    /// Logs the HTTP response
    /// </summary>
    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var response = context.Response;
        var responseLog = new
        {
            CorrelationId = correlationId,
            StatusCode = response.StatusCode,
            Headers = GetHeaders(response.Headers),
            ElapsedMilliseconds = elapsedMilliseconds,
            Body = await GetResponseBodyAsync(response)
        };

        var logLevel = GetLogLevel(response.StatusCode);
        _logger.Log(logLevel, "HTTP Response: {StatusCode} - {ElapsedMs}ms - CorrelationId: {CorrelationId}",
            response.StatusCode, elapsedMilliseconds, correlationId);

        if (_options.LogResponseDetails)
        {
            _logger.LogDebug("Response Details: {@ResponseLog}", responseLog);
        }

        // Log slow requests
        if (elapsedMilliseconds > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow Request: {Method} {Path} took {ElapsedMs}ms - CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path, elapsedMilliseconds, correlationId);
        }
    }

    /// <summary>
    /// Gets the request body as a string
    /// </summary>
    private async Task<string?> GetRequestBodyAsync(HttpRequest request)
    {
        if (!_options.LogRequestBody)
            return null;

        if (!HasBody(request))
            return null;

        if (request.ContentLength > _options.MaxBodySizeToLog)
            return $"[Body too large: {request.ContentLength} bytes]";

        request.EnableBuffering();
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;

        return SanitizeBody(body, request.ContentType);
    }

    /// <summary>
    /// Gets the response body as a string
    /// </summary>
    private async Task<string?> GetResponseBodyAsync(HttpResponse response)
    {
        if (!_options.LogResponseBody)
            return null;

        if (response.Body.Length > _options.MaxBodySizeToLog)
            return $"[Body too large: {response.Body.Length} bytes]";

        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return SanitizeBody(body, response.ContentType);
    }

    /// <summary>
    /// Sanitizes the body content for logging
    /// </summary>
    private string? SanitizeBody(string? body, string? contentType)
    {
        if (string.IsNullOrEmpty(body))
            return null;

        // Don't log binary content
        if (IsBinaryContent(contentType))
            return "[Binary Content]";

        // Sanitize sensitive data
        if (_options.SanitizeSensitiveData)
        {
            body = SanitizeSensitiveData(body);
        }

        // Truncate if too long
        if (body.Length > _options.MaxBodySizeToLog)
        {
            body = body.Substring(0, _options.MaxBodySizeToLog) + "... [truncated]";
        }

        return body;
    }

    /// <summary>
    /// Sanitizes sensitive data from the body
    /// </summary>
    private static string SanitizeSensitiveData(string body)
    {
        // List of sensitive field names to sanitize
        var sensitiveFields = new[] { "password", "token", "secret", "key", "authorization", "credit", "ssn", "social" };

        try
        {
            // Try to parse as JSON and sanitize
            var jsonDoc = JsonDocument.Parse(body);
            var sanitized = SanitizeJsonElement(jsonDoc.RootElement, sensitiveFields);
            return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            // If not JSON, do simple string replacement
            foreach (var field in sensitiveFields)
            {
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
                body = System.Text.RegularExpressions.Regex.Replace(body, pattern, $"\"{field}\": \"***\"", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return body;
        }
    }

    /// <summary>
    /// Sanitizes a JSON element recursively
    /// </summary>
    private static object SanitizeJsonElement(JsonElement element, string[] sensitiveFields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name;
                    var value = sensitiveFields.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase))
                        ? "***"
                        : SanitizeJsonElement(property.Value, sensitiveFields);
                    obj[key] = value;
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(e => SanitizeJsonElement(e, sensitiveFields)).ToArray();

            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Number:
                return element.GetDecimal();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            case JsonValueKind.Null:
                return null!;

            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Checks if the content type is binary
    /// </summary>
    private static bool IsBinaryContent(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var binaryTypes = new[] { "image/", "video/", "audio/", "application/octet-stream", "application/pdf" };
        return binaryTypes.Any(type => contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the request has a body
    /// </summary>
    private static bool HasBody(HttpRequest request)
    {
        return request.ContentLength > 0 || 
               request.Headers.ContainsKey("Transfer-Encoding");
    }

    /// <summary>
    /// Gets filtered headers for logging
    /// </summary>
    private Dictionary<string, string> GetHeaders(IHeaderDictionary headers)
    {
        if (!_options.LogHeaders)
            return new Dictionary<string, string>();

        var filteredHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key", "x-auth-token" };

        foreach (var header in headers)
        {
            var key = header.Key;
            var value = sensitiveHeaders.Contains(key.ToLower()) ? "***" : header.Value.ToString();
            filteredHeaders[key] = value;
        }

        return filteredHeaders;
    }

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    private static string? GetClientIpAddress(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the user ID from the context
    /// </summary>
    private static string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value ??
               context.User?.FindFirst("userId")?.Value ??
               context.User?.Identity?.Name;
    }

    /// <summary>
    /// Gets the appropriate log level based on status code
    /// </summary>
    private static LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}

/// <summary>
/// Configuration options for request/response logging
/// </summary>
public class RequestResponseLoggingOptions
{
    /// <summary>
    /// Whether to log request details
    /// </summary>
    public bool LogRequestDetails { get; set; } = true;

    /// <summary>
    /// Whether to log response details
    /// </summary>
    public bool LogResponseDetails { get; set; } = true;

    /// <summary>
    /// Whether to log request headers
    /// </summary>
    public bool LogHeaders { get; set; } = true;

    /// <summary>
    /// Whether to log request body
    /// </summary>
    public bool LogRequestBody { get; set; } = true;

    /// <summary>
    /// Whether to log response body
    /// </summary>
    public bool LogResponseBody { get; set; } = false;

    /// <summary>
    /// Whether to log Swagger requests
    /// </summary>
    public bool LogSwaggerRequests { get; set; } = false;

    /// <summary>
    /// Whether to skip static files
    /// </summary>
    public bool SkipStaticFiles { get; set; } = true;

    /// <summary>
    /// Whether to sanitize sensitive data
    /// </summary>
    public bool SanitizeSensitiveData { get; set; } = true;

    /// <summary>
    /// Maximum body size to log (in bytes)
    /// </summary>
    public int MaxBodySizeToLog { get; set; } = 4096;

    /// <summary>
    /// Threshold for slow request logging (in milliseconds)
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 5000;
}
