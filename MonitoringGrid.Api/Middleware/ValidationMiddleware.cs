using FluentValidation;
using MonitoringGrid.Api.Common;
using System.Text.Json;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Middleware for handling FluentValidation validation errors and model state validation
/// </summary>
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(
        RequestDelegate next,
        ILogger<ValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Process the HTTP request and handle validation errors
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            await HandleValidationExceptionAsync(context, validationException);
        }
        catch (ArgumentException argumentException)
        {
            await HandleArgumentExceptionAsync(context, argumentException);
        }
    }

    /// <summary>
    /// Handle FluentValidation exceptions
    /// </summary>
    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException validationException)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var corrId)
            ? corrId?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        _logger.LogWarning("Validation failed for request {CorrelationId}: {Errors}",
            correlationId,
            string.Join(", ", validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

        var validationErrors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var response = ApiResponse.ValidationFailure(validationErrors, new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ValidationErrorCount"] = validationException.Errors.Count()
        });

        await WriteResponseAsync(context, (ApiResponse)response, 400);
    }

    /// <summary>
    /// Handle argument exceptions
    /// </summary>
    private async Task HandleArgumentExceptionAsync(HttpContext context, ArgumentException argumentException)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var corrId)
            ? corrId?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        _logger.LogWarning("Argument validation failed for request {CorrelationId}: {Message}",
            correlationId,
            argumentException.Message);

        var response = ApiResponse.Failure(argumentException.Message, new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ErrorType"] = "ArgumentValidation"
        });

        await WriteResponseAsync(context, response, 400);
    }

    /// <summary>
    /// Write the API response to the HTTP context
    /// </summary>
    private static async Task WriteResponseAsync(HttpContext context, ApiResponse response, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Extension methods for validation middleware registration
/// </summary>
public static class ValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds validation middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidationMiddleware>();
    }
}

/// <summary>
/// Validates that a string is a valid SQL query (basic validation)
/// </summary>
public class ValidSqlQueryAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        private static readonly string[] DangerousKeywords = 
        {
            "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE",
            "EXEC", "EXECUTE", "SP_", "XP_", "OPENROWSET", "OPENDATASOURCE"
        };

        public override bool IsValid(object? value)
        {
            if (value is not string sqlQuery || string.IsNullOrWhiteSpace(sqlQuery))
                return false;

            // Basic validation - check for dangerous keywords
            var upperQuery = sqlQuery.ToUpperInvariant();
            
            foreach (var keyword in DangerousKeywords)
            {
                if (upperQuery.Contains(keyword))
                {
                    ErrorMessage = $"SQL query contains dangerous keyword: {keyword}";
                    return false;
                }
            }

            // Must start with SELECT
            if (!upperQuery.TrimStart().StartsWith("SELECT"))
            {
                ErrorMessage = "SQL query must start with SELECT";
                return false;
            }

            return true;
        }
    }

/// <summary>
/// Validates that a frequency value is within acceptable range
/// </summary>
public class ValidFrequencyAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        private readonly int _minMinutes;
        private readonly int _maxMinutes;

        public ValidFrequencyAttribute(int minMinutes = 1, int maxMinutes = 1440)
        {
            _minMinutes = minMinutes;
            _maxMinutes = maxMinutes;
        }

        public override bool IsValid(object? value)
        {
            if (value is not int frequency)
                return false;

            if (frequency < _minMinutes || frequency > _maxMinutes)
            {
                ErrorMessage = $"Frequency must be between {_minMinutes} and {_maxMinutes} minutes";
                return false;
            }

            return true;
        }
    }

/// <summary>
/// Validates that an email address is properly formatted and from allowed domains
/// </summary>
public class ValidEmailDomainAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        private readonly string[] _allowedDomains;

        public ValidEmailDomainAttribute(params string[] allowedDomains)
        {
            _allowedDomains = allowedDomains ?? Array.Empty<string>();
        }

        public override bool IsValid(object? value)
        {
            if (value is not string email || string.IsNullOrWhiteSpace(email))
                return false;

            // Basic email format validation
            if (!email.Contains('@') || !email.Contains('.'))
            {
                ErrorMessage = "Invalid email format";
                return false;
            }

            // Domain validation if domains are specified
            if (_allowedDomains.Length > 0)
            {
                var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
                if (domain == null || !_allowedDomains.Contains(domain))
                {
                    ErrorMessage = $"Email domain must be one of: {string.Join(", ", _allowedDomains)}";
                    return false;
                }
            }

            return true;
        }
    }

/// <summary>
/// Validates that a priority value is within the valid range
/// </summary>
public class ValidPriorityAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not byte priority)
                return false;

            if (priority < 1 || priority > 2)
            {
                ErrorMessage = "Priority must be 1 (SMS+Email) or 2 (Email only)";
                return false;
            }

            return true;
        }
    }

/// <summary>
/// Input sanitization service for preventing XSS and injection attacks
/// </summary>
public interface IInputSanitizationService
{
    /// <summary>
    /// Sanitizes HTML input to prevent XSS attacks
    /// </summary>
    string SanitizeHtml(string input);

    /// <summary>
    /// Sanitizes SQL input to prevent injection attacks
    /// </summary>
    string SanitizeSql(string input);

    /// <summary>
    /// Validates and sanitizes general text input
    /// </summary>
    string SanitizeText(string input);
}

/// <summary>
/// Implementation of input sanitization service
/// </summary>
public class InputSanitizationService : IInputSanitizationService
{
    private readonly ILogger<InputSanitizationService> _logger;

    public InputSanitizationService(ILogger<InputSanitizationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes HTML input by removing dangerous tags and attributes
    /// </summary>
    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Basic HTML sanitization - remove script tags and dangerous attributes
        var sanitized = input
            .Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase)
            .Replace("</script>", "&lt;/script&gt;", StringComparison.OrdinalIgnoreCase)
            .Replace("javascript:", "javascript&#58;", StringComparison.OrdinalIgnoreCase)
            .Replace("vbscript:", "vbscript&#58;", StringComparison.OrdinalIgnoreCase)
            .Replace("onload=", "onload&#61;", StringComparison.OrdinalIgnoreCase)
            .Replace("onerror=", "onerror&#61;", StringComparison.OrdinalIgnoreCase);

        return sanitized;
    }

    /// <summary>
    /// Sanitizes SQL input by escaping dangerous characters
    /// </summary>
    public string SanitizeSql(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Basic SQL sanitization - escape single quotes
        return input.Replace("'", "''");
    }

    /// <summary>
    /// Sanitizes general text input
    /// </summary>
    public string SanitizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove control characters and normalize whitespace
        var sanitized = new string(input.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());
        return sanitized.Trim();
    }
}
