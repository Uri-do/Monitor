using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Infrastructure.Configuration;

/// <summary>
/// Standardized configuration options for the MonitoringGrid system
/// Consolidates all configuration settings with validation
/// </summary>
public class MonitoringGridOptions
{
    public const string SectionName = "MonitoringGrid";

    [Required]
    public DatabaseOptions Database { get; set; } = new();

    [Required]
    public MonitoringOptions Monitoring { get; set; } = new();

    public EmailOptions Email { get; set; } = new();

    public SecurityOptions Security { get; set; } = new();

    public WorkerOptions Worker { get; set; } = new();

    public LoggingOptions Logging { get; set; } = new();
}

/// <summary>
/// Database connection configuration
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Main monitoring database connection (PopAI)
    /// </summary>
    [Required]
    public string DefaultConnection { get; set; } = string.Empty;

    /// <summary>
    /// Source database for monitoring (ProgressPlayDB)
    /// </summary>
    [Required]
    public string SourceDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Database command timeout in seconds
    /// </summary>
    [Range(10, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable connection pooling
    /// </summary>
    public bool EnablePooling { get; set; } = true;

    /// <summary>
    /// Maximum pool size
    /// </summary>
    [Range(1, 100)]
    public int MaxPoolSize { get; set; } = 50;
}

/// <summary>
/// Core monitoring system configuration
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Maximum number of indicators to process in parallel
    /// </summary>
    [Range(1, 20)]
    public int MaxParallelExecutions { get; set; } = 5;

    /// <summary>
    /// Service interval in seconds
    /// </summary>
    [Range(10, 3600)]
    public int ServiceIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed alerts
    /// </summary>
    [Range(0, 10)]
    public int AlertRetryCount { get; set; } = 3;

    /// <summary>
    /// Enable SMS notifications
    /// </summary>
    public bool EnableSms { get; set; } = true;

    /// <summary>
    /// Enable email notifications
    /// </summary>
    public bool EnableEmail { get; set; } = true;

    /// <summary>
    /// Enable historical comparison
    /// </summary>
    public bool EnableHistoricalComparison { get; set; } = true;

    /// <summary>
    /// Enable absolute threshold checking
    /// </summary>
    public bool EnableAbsoluteThresholds { get; set; } = true;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    [Range(1, 100)]
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Maximum days to retain alert history
    /// </summary>
    [Range(1, 365)]
    public int MaxAlertHistoryDays { get; set; } = 90;

    /// <summary>
    /// Number of weeks to look back for historical comparison
    /// </summary>
    [Range(1, 52)]
    public int HistoricalWeeksBack { get; set; } = 4;

    /// <summary>
    /// Default last minutes for indicator execution
    /// </summary>
    [Range(1, 10080)]
    public int DefaultLastMinutes { get; set; } = 1440;

    /// <summary>
    /// Default frequency in minutes
    /// </summary>
    [Range(1, 1440)]
    public int DefaultFrequency { get; set; } = 60;

    /// <summary>
    /// Default cooldown period in minutes
    /// </summary>
    [Range(1, 1440)]
    public int DefaultCooldownMinutes { get; set; } = 30;

    /// <summary>
    /// Enable worker services
    /// </summary>
    public bool EnableWorkerServices { get; set; } = true;

    /// <summary>
    /// SMS gateway email address
    /// </summary>
    [EmailAddress]
    public string SmsGateway { get; set; } = string.Empty; // Must be configured via environment

    /// <summary>
    /// Administrator email address
    /// </summary>
    [EmailAddress]
    public string AdminEmail { get; set; } = "admin@example.com";
}

/// <summary>
/// Email configuration options
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// SMTP server hostname
    /// </summary>
    [Required]
    public string SmtpServer { get; set; } = "smtp.example.com";

    /// <summary>
    /// SMTP server port
    /// </summary>
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// From email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "monitoring@example.com";

    /// <summary>
    /// From display name
    /// </summary>
    [Required]
    public string FromName { get; set; } = "Monitoring Grid System";

    /// <summary>
    /// Email timeout in seconds
    /// </summary>
    [Range(10, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Security configuration options
/// </summary>
public class SecurityOptions
{
    public JwtOptions Jwt { get; set; } = new();
    public PasswordPolicyOptions PasswordPolicy { get; set; } = new();
    public SessionOptions Session { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
    public EncryptionOptions Encryption { get; set; } = new();
}

/// <summary>
/// JWT token configuration
/// </summary>
public class JwtOptions
{
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "MonitoringGrid.Api";

    [Required]
    public string Audience { get; set; } = "MonitoringGrid.Frontend";

    [Range(5, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    [Range(1, 90)]
    public int RefreshTokenExpirationDays { get; set; } = 30;

    public string Algorithm { get; set; } = "HS256";
}

/// <summary>
/// Password policy configuration
/// </summary>
public class PasswordPolicyOptions
{
    [Range(6, 128)]
    public int MinimumLength { get; set; } = 8;

    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;

    [Range(1, 20)]
    public int MaxFailedAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int LockoutDurationMinutes { get; set; } = 30;

    [Range(1, 365)]
    public int PasswordExpirationDays { get; set; } = 90;

    [Range(1, 20)]
    public int PasswordHistoryCount { get; set; } = 5;
}

/// <summary>
/// Session configuration
/// </summary>
public class SessionOptions
{
    [Range(5, 1440)]
    public int TimeoutMinutes { get; set; } = 60;

    public bool ExtendOnActivity { get; set; } = true;

    [Range(1, 10)]
    public int MaxConcurrentSessions { get; set; } = 3;
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitOptions
{
    public bool IsEnabled { get; set; } = true;

    [Range(1, 10000)]
    public int MaxRequestsPerMinute { get; set; } = 100;

    [Range(1, 100)]
    public int MaxLoginAttemptsPerMinute { get; set; } = 5;

    [Range(1, 1440)]
    public int BanDurationMinutes { get; set; } = 15;
}

/// <summary>
/// Encryption configuration
/// </summary>
public class EncryptionOptions
{
    [Required]
    public string EncryptionKey { get; set; } = string.Empty;

    [Required]
    public string HashingSalt { get; set; } = string.Empty;
}

/// <summary>
/// Worker service configuration
/// </summary>
public class WorkerOptions
{
    public IndicatorMonitoringOptions IndicatorMonitoring { get; set; } = new();
    public ScheduledTasksOptions ScheduledTasks { get; set; } = new();
    public HealthChecksOptions HealthChecks { get; set; } = new();
    public AlertProcessingOptions AlertProcessing { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
}

/// <summary>
/// Indicator monitoring worker configuration
/// </summary>
public class IndicatorMonitoringOptions
{
    [Range(10, 3600)]
    public int IntervalSeconds { get; set; } = 60;

    [Range(1, 20)]
    public int MaxParallelIndicators { get; set; } = 5;

    [Range(30, 1800)]
    public int ExecutionTimeoutSeconds { get; set; } = 300;

    public bool ProcessOnlyActiveIndicators { get; set; } = true;
    public bool SkipRunningIndicators { get; set; } = true;
}

/// <summary>
/// Scheduled tasks configuration
/// </summary>
public class ScheduledTasksOptions
{
    public bool Enabled { get; set; } = true;

    [Required]
    public string CleanupCronExpression { get; set; } = "0 0 2 * * ?";

    [Required]
    public string MaintenanceCronExpression { get; set; } = "0 0 3 ? * SUN";

    [Range(1, 365)]
    public int HistoricalDataRetentionDays { get; set; } = 90;

    [Range(1, 90)]
    public int LogRetentionDays { get; set; } = 30;
}

/// <summary>
/// Health checks configuration
/// </summary>
public class HealthChecksOptions
{
    [Range(30, 3600)]
    public int IntervalSeconds { get; set; } = 300;

    public bool CheckDatabase { get; set; } = true;
    public bool CheckIndicatorExecution { get; set; } = true;
    public bool CheckAlertProcessing { get; set; } = true;

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Alert processing configuration
/// </summary>
public class AlertProcessingOptions
{
    [Range(10, 300)]
    public int IntervalSeconds { get; set; } = 30;

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    public bool EnableEscalation { get; set; } = true;

    [Range(1, 1440)]
    public int EscalationTimeoutMinutes { get; set; } = 60;

    public bool EnableAutoResolution { get; set; } = true;

    [Range(1, 1440)]
    public int AutoResolutionTimeoutMinutes { get; set; } = 120;
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingOptions
{
    public string MinimumLevel { get; set; } = "Information";
    public bool EnableStructuredLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = false;
    public bool LogToConsole { get; set; } = true;
    public bool LogToEventLog { get; set; } = false;
}
