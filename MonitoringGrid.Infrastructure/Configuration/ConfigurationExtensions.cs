using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuration setup and validation
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds and validates MonitoringGrid configuration options
    /// </summary>
    public static IServiceCollection AddMonitoringGridConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Bind and validate the main configuration
        services.Configure<MonitoringGridOptions>(configuration.GetSection(MonitoringGridOptions.SectionName));
        
        // Add validation
        services.AddSingleton<IValidateOptions<MonitoringGridOptions>, MonitoringGridOptionsValidator>();
        
        // Register individual option sections for easier injection
        services.Configure<DatabaseOptions>(configuration.GetSection($"{MonitoringGridOptions.SectionName}:Database"));
        services.Configure<MonitoringOptions>(configuration.GetSection($"{MonitoringGridOptions.SectionName}:Monitoring"));
        services.Configure<EmailOptions>(configuration.GetSection($"{MonitoringGridOptions.SectionName}:Email"));
        services.Configure<SecurityOptions>(configuration.GetSection($"{MonitoringGridOptions.SectionName}:Security"));
        services.Configure<WorkerOptions>(configuration.GetSection($"{MonitoringGridOptions.SectionName}:Worker"));
        
        return services;
    }

    /// <summary>
    /// Gets a validated configuration section
    /// </summary>
    public static T GetValidatedSection<T>(this IConfiguration configuration, string sectionName) 
        where T : class, new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(section);
        
        if (!Validator.TryValidateObject(section, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Configuration validation failed for section '{sectionName}': {errors}");
        }
        
        return section;
    }

    /// <summary>
    /// Validates configuration on startup
    /// </summary>
    public static void ValidateConfiguration(this IConfiguration configuration)
    {
        try
        {
            // Validate main configuration
            var options = configuration.GetValidatedSection<MonitoringGridOptions>(MonitoringGridOptions.SectionName);
            
            // Validate connection strings
            ValidateConnectionStrings(configuration);
            
            // Validate required sections
            ValidateRequiredSections(configuration);
            
            Console.WriteLine("✅ Configuration validation passed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Configuration validation failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates connection strings
    /// </summary>
    private static void ValidateConnectionStrings(IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        
        var requiredConnections = new[] { "DefaultConnection", "SourceDatabase" };
        
        foreach (var connectionName in requiredConnections)
        {
            var connectionString = connectionStrings[connectionName];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{connectionName}' is required but not configured");
            }
            
            // Basic validation - ensure it contains required keywords
            if (!connectionString.Contains("Data Source") && !connectionString.Contains("Server"))
            {
                throw new InvalidOperationException($"Connection string '{connectionName}' appears to be invalid");
            }
        }
    }

    /// <summary>
    /// Validates required configuration sections
    /// </summary>
    private static void ValidateRequiredSections(IConfiguration configuration)
    {
        var requiredSections = new[]
        {
            $"{MonitoringGridOptions.SectionName}:Database",
            $"{MonitoringGridOptions.SectionName}:Monitoring",
            $"{MonitoringGridOptions.SectionName}:Security:Jwt"
        };

        foreach (var sectionPath in requiredSections)
        {
            var section = configuration.GetSection(sectionPath);
            if (!section.Exists())
            {
                throw new InvalidOperationException($"Required configuration section '{sectionPath}' is missing");
            }
        }
    }

    /// <summary>
    /// Gets connection string with fallback and validation
    /// </summary>
    public static string GetConnectionStringOrThrow(this IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is required but not configured");
        }
        
        return connectionString;
    }

    /// <summary>
    /// Creates a configuration summary for logging
    /// </summary>
    public static string CreateConfigurationSummary(this IConfiguration configuration)
    {
        var summary = new List<string>
        {
            "=== MonitoringGrid Configuration Summary ===",
            ""
        };

        try
        {
            var options = configuration.GetSection(MonitoringGridOptions.SectionName).Get<MonitoringGridOptions>();
            if (options != null)
            {
                summary.AddRange(new[]
                {
                    $"Database Timeout: {options.Database.TimeoutSeconds}s",
                    $"Max Parallel Executions: {options.Monitoring.MaxParallelExecutions}",
                    $"Service Interval: {options.Monitoring.ServiceIntervalSeconds}s",
                    $"Email Enabled: {options.Monitoring.EnableEmail}",
                    $"SMS Enabled: {options.Monitoring.EnableSms}",
                    $"Worker Services Enabled: {options.Monitoring.EnableWorkerServices}",
                    $"Historical Comparison: {options.Monitoring.EnableHistoricalComparison}",
                    $"Alert Retention: {options.Monitoring.MaxAlertHistoryDays} days",
                    ""
                });

                if (options.Worker.IndicatorMonitoring != null)
                {
                    summary.AddRange(new[]
                    {
                        "Worker Configuration:",
                        $"  Indicator Monitoring Interval: {options.Worker.IndicatorMonitoring.IntervalSeconds}s",
                        $"  Max Parallel Indicators: {options.Worker.IndicatorMonitoring.MaxParallelIndicators}",
                        $"  Execution Timeout: {options.Worker.IndicatorMonitoring.ExecutionTimeoutSeconds}s",
                        $"  Scheduled Tasks Enabled: {options.Worker.ScheduledTasks.Enabled}",
                        ""
                    });
                }

                if (options.Security.Jwt != null)
                {
                    summary.AddRange(new[]
                    {
                        "Security Configuration:",
                        $"  JWT Issuer: {options.Security.Jwt.Issuer}",
                        $"  JWT Audience: {options.Security.Jwt.Audience}",
                        $"  Access Token Expiration: {options.Security.Jwt.AccessTokenExpirationMinutes} minutes",
                        $"  Rate Limiting Enabled: {options.Security.RateLimit.IsEnabled}",
                        $"  Max Requests/Minute: {options.Security.RateLimit.MaxRequestsPerMinute}",
                        ""
                    });
                }
            }

            // Connection strings (without sensitive data)
            var connectionStrings = configuration.GetSection("ConnectionStrings");
            summary.Add("Connection Strings:");
            foreach (var child in connectionStrings.GetChildren())
            {
                var connectionString = child.Value ?? "";
                var serverPart = ExtractServerFromConnectionString(connectionString);
                summary.Add($"  {child.Key}: {serverPart}");
            }
        }
        catch (Exception ex)
        {
            summary.Add($"❌ Error creating configuration summary: {ex.Message}");
        }

        summary.Add("=== End Configuration Summary ===");
        return string.Join(Environment.NewLine, summary);
    }

    /// <summary>
    /// Extracts server information from connection string for logging (without sensitive data)
    /// </summary>
    private static string ExtractServerFromConnectionString(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            var serverPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Data Source", StringComparison.OrdinalIgnoreCase) ||
                                                      p.Trim().StartsWith("Server", StringComparison.OrdinalIgnoreCase));
            var databasePart = parts.FirstOrDefault(p => p.Trim().StartsWith("Initial Catalog", StringComparison.OrdinalIgnoreCase) ||
                                                        p.Trim().StartsWith("Database", StringComparison.OrdinalIgnoreCase));

            var result = new List<string>();
            if (!string.IsNullOrEmpty(serverPart))
                result.Add(serverPart.Trim());
            if (!string.IsNullOrEmpty(databasePart))
                result.Add(databasePart.Trim());

            return result.Count > 0 ? string.Join("; ", result) : "Connection configured";
        }
        catch
        {
            return "Connection configured";
        }
    }
}

/// <summary>
/// Validator for MonitoringGrid configuration options
/// </summary>
public class MonitoringGridOptionsValidator : IValidateOptions<MonitoringGridOptions>
{
    public ValidateOptionsResult Validate(string? name, MonitoringGridOptions options)
    {
        var failures = new List<string>();

        // Validate database options
        if (string.IsNullOrWhiteSpace(options.Database.DefaultConnection))
            failures.Add("Database.DefaultConnection is required");

        if (string.IsNullOrWhiteSpace(options.Database.SourceDatabase))
            failures.Add("Database.SourceDatabase is required");

        // Validate monitoring options
        if (options.Monitoring.MaxParallelExecutions < 1 || options.Monitoring.MaxParallelExecutions > 20)
            failures.Add("Monitoring.MaxParallelExecutions must be between 1 and 20");

        if (options.Monitoring.ServiceIntervalSeconds < 10 || options.Monitoring.ServiceIntervalSeconds > 3600)
            failures.Add("Monitoring.ServiceIntervalSeconds must be between 10 and 3600");

        // Validate email options if email is enabled
        if (options.Monitoring.EnableEmail)
        {
            if (string.IsNullOrWhiteSpace(options.Email.SmtpServer))
                failures.Add("Email.SmtpServer is required when email is enabled");

            if (string.IsNullOrWhiteSpace(options.Email.FromAddress))
                failures.Add("Email.FromAddress is required when email is enabled");
        }

        // Validate JWT options
        if (string.IsNullOrWhiteSpace(options.Security.Jwt.SecretKey))
            failures.Add("Security.Jwt.SecretKey is required");

        if (options.Security.Jwt.SecretKey.Length < 32)
            failures.Add("Security.Jwt.SecretKey must be at least 32 characters long");

        // Validate worker options
        if (options.Worker.IndicatorMonitoring.MaxParallelIndicators < 1 || 
            options.Worker.IndicatorMonitoring.MaxParallelIndicators > 20)
            failures.Add("Worker.IndicatorMonitoring.MaxParallelIndicators must be between 1 and 20");

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
