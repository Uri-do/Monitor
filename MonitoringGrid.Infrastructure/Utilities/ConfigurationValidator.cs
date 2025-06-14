using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Utilities;

/// <summary>
/// Configuration validation utility for MonitoringGrid
/// Validates configuration files and provides detailed reporting
/// </summary>
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates configuration from file paths
    /// </summary>
    public async Task<ValidationResult> ValidateConfigurationFilesAsync(params string[] configFilePaths)
    {
        var result = new ValidationResult();
        
        foreach (var filePath in configFilePaths)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    result.AddError($"Configuration file not found: {filePath}");
                    continue;
                }

                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(filePath, optional: false)
                    .Build();

                var fileResult = await ValidateConfigurationAsync(configuration, filePath);
                result.Merge(fileResult);
            }
            catch (Exception ex)
            {
                result.AddError($"Error reading configuration file {filePath}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates a configuration instance
    /// </summary>
    public async Task<ValidationResult> ValidateConfigurationAsync(IConfiguration configuration, string source = "Configuration")
    {
        var result = new ValidationResult { Source = source };

        try
        {
            // Validate connection strings
            ValidateConnectionStrings(configuration, result);

            // Validate MonitoringGrid section
            ValidateMonitoringGridSection(configuration, result);

            // Validate structure and required sections
            ValidateConfigurationStructure(configuration, result);

            // Validate environment-specific settings
            ValidateEnvironmentSettings(configuration, result);

            _logger.LogInformation("Configuration validation completed for {Source}. Errors: {ErrorCount}, Warnings: {WarningCount}", 
                source, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            result.AddError($"Unexpected error during validation: {ex.Message}");
            _logger.LogError(ex, "Error validating configuration for {Source}", source);
        }

        return result;
    }

    /// <summary>
    /// Validates connection strings
    /// </summary>
    private void ValidateConnectionStrings(IConfiguration configuration, ValidationResult result)
    {
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        
        var requiredConnections = new[] { "DefaultConnection", "SourceDatabase" };
        
        foreach (var connectionName in requiredConnections)
        {
            var connectionString = connectionStrings[connectionName];
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.AddError($"Connection string '{connectionName}' is required but missing");
                continue;
            }

            // Validate connection string format
            if (!IsValidConnectionString(connectionString))
            {
                result.AddError($"Connection string '{connectionName}' appears to be invalid");
                continue;
            }

            // Check for common issues
            if (connectionString.Contains("password", StringComparison.OrdinalIgnoreCase) && 
                connectionString.Contains("XXXXXXXX"))
            {
                result.AddWarning($"Connection string '{connectionName}' contains placeholder password");
            }

            if (!connectionString.Contains("TrustServerCertificate=true", StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning($"Connection string '{connectionName}' should include TrustServerCertificate=true for development");
            }
        }

        // Check for legacy connection string names
        var legacyNames = new[] { "MonitoringGrid", "ProgressPlayDB" };
        foreach (var legacyName in legacyNames)
        {
            if (!string.IsNullOrWhiteSpace(connectionStrings[legacyName]))
            {
                result.AddWarning($"Legacy connection string '{legacyName}' found. Consider using standardized names.");
            }
        }
    }

    /// <summary>
    /// Validates the MonitoringGrid configuration section
    /// </summary>
    private void ValidateMonitoringGridSection(IConfiguration configuration, ValidationResult result)
    {
        var monitoringGridSection = configuration.GetSection(MonitoringGridOptions.SectionName);
        
        if (!monitoringGridSection.Exists())
        {
            result.AddError($"Required section '{MonitoringGridOptions.SectionName}' is missing");
            return;
        }

        try
        {
            var options = monitoringGridSection.Get<MonitoringGridOptions>();
            if (options == null)
            {
                result.AddError($"Failed to bind '{MonitoringGridOptions.SectionName}' section");
                return;
            }

            // Validate using data annotations
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(options);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            
            if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    result.AddError($"MonitoringGrid validation: {validationResult.ErrorMessage}");
                }
            }

            // Additional business logic validation
            ValidateBusinessRules(options, result);
        }
        catch (Exception ex)
        {
            result.AddError($"Error validating MonitoringGrid section: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates business rules for configuration
    /// </summary>
    private void ValidateBusinessRules(MonitoringGridOptions options, ValidationResult result)
    {
        // Validate monitoring settings
        if (options.Monitoring.EnableEmail && string.IsNullOrWhiteSpace(options.Email.SmtpServer))
        {
            result.AddError("Email is enabled but SMTP server is not configured");
        }

        if (options.Monitoring.EnableSms && string.IsNullOrWhiteSpace(options.Monitoring.SmsGateway))
        {
            result.AddError("SMS is enabled but SMS gateway is not configured");
        }

        // Validate worker settings
        if (options.Worker.IndicatorMonitoring.MaxParallelIndicators > options.Monitoring.MaxParallelExecutions)
        {
            result.AddWarning("Worker MaxParallelIndicators exceeds Monitoring MaxParallelExecutions");
        }

        // Validate security settings
        if (options.Security.Jwt.SecretKey.Length < 32)
        {
            result.AddError("JWT SecretKey must be at least 32 characters long");
        }

        if (options.Security.Jwt.AccessTokenExpirationMinutes > 1440)
        {
            result.AddWarning("JWT access token expiration is longer than 24 hours");
        }

        // Validate database settings
        if (options.Database.TimeoutSeconds < 30)
        {
            result.AddWarning("Database timeout is less than 30 seconds, which may cause issues");
        }
    }

    /// <summary>
    /// Validates overall configuration structure
    /// </summary>
    private void ValidateConfigurationStructure(IConfiguration configuration, ValidationResult result)
    {
        var requiredSections = new[]
        {
            "ConnectionStrings",
            $"{MonitoringGridOptions.SectionName}:Database",
            $"{MonitoringGridOptions.SectionName}:Monitoring",
            $"{MonitoringGridOptions.SectionName}:Security:Jwt"
        };

        foreach (var sectionPath in requiredSections)
        {
            var section = configuration.GetSection(sectionPath);
            if (!section.Exists())
            {
                result.AddError($"Required configuration section '{sectionPath}' is missing");
            }
        }

        // Check for legacy sections
        var legacySections = new[] { "Monitoring", "Email", "Security", "Worker" };
        foreach (var legacySection in legacySections)
        {
            var section = configuration.GetSection(legacySection);
            if (section.Exists() && !configuration.GetSection($"{MonitoringGridOptions.SectionName}:{legacySection}").Exists())
            {
                result.AddWarning($"Legacy section '{legacySection}' found at root level. Consider moving to '{MonitoringGridOptions.SectionName}:{legacySection}'");
            }
        }
    }

    /// <summary>
    /// Validates environment-specific settings
    /// </summary>
    private void ValidateEnvironmentSettings(IConfiguration configuration, ValidationResult result)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            // Development-specific validations
            var monitoringSection = configuration.GetSection($"{MonitoringGridOptions.SectionName}:Monitoring");
            if (monitoringSection["EnableWorkerServices"] == "true")
            {
                result.AddWarning("Worker services are enabled in Development environment");
            }
        }
        else if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            // Production-specific validations
            var jwtSection = configuration.GetSection($"{MonitoringGridOptions.SectionName}:Security:Jwt");
            if (jwtSection["SecretKey"]?.Contains("MonitoringGrid-Super-Secret-Key") == true)
            {
                result.AddError("Default JWT secret key detected in Production environment");
            }

            var encryptionSection = configuration.GetSection($"{MonitoringGridOptions.SectionName}:Security:Encryption");
            if (encryptionSection["EncryptionKey"]?.StartsWith("TW9uaXRvcmluZ0dyaWQ") == true)
            {
                result.AddError("Default encryption key detected in Production environment");
            }
        }
    }

    /// <summary>
    /// Validates connection string format
    /// </summary>
    private bool IsValidConnectionString(string connectionString)
    {
        try
        {
            return connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Server", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Console application entry point for configuration validation
    /// </summary>
    public static async Task RunConfigurationValidationTool(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ConfigurationValidator>();
        var validator = new ConfigurationValidator(logger);

        Console.WriteLine("🔍 MonitoringGrid Configuration Validator");
        Console.WriteLine("==========================================");

        var configFiles = args.Length > 0 ? args : new[]
        {
            "MonitoringGrid.Api/appsettings.json",
            "MonitoringGrid.Api/appsettings.Development.json",
            "MonitoringGrid.Worker/appsettings.json",
            "MonitoringGrid.Worker/appsettings.Development.json"
        };

        var result = await validator.ValidateConfigurationFilesAsync(configFiles);

        Console.WriteLine($"\n📊 Validation Results:");
        Console.WriteLine($"Files validated: {configFiles.Length}");
        Console.WriteLine($"Errors: {result.Errors.Count}");
        Console.WriteLine($"Warnings: {result.Warnings.Count}");

        if (result.Errors.Any())
        {
            Console.WriteLine("\n❌ Errors:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  • {error}");
            }
        }

        if (result.Warnings.Any())
        {
            Console.WriteLine("\n⚠️ Warnings:");
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"  • {warning}");
            }
        }

        if (!result.Errors.Any() && !result.Warnings.Any())
        {
            Console.WriteLine("\n✅ All configuration files are valid!");
        }

        Environment.Exit(result.Errors.Any() ? 1 : 0);
    }
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ValidationResult
{
    public string Source { get; set; } = string.Empty;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);

    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }

    public bool IsValid => !Errors.Any();
}
