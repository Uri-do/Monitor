namespace MonitoringGrid.Core.Models;

/// <summary>
/// Filter criteria for security event queries
/// </summary>
public class SecurityEventFilter
{
    /// <summary>
    /// Start date for filtering events
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering events
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by event type (e.g., LOGIN_SUCCESS, LOGIN_FAILED, etc.)
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Filter by user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Filter by IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Filter by success status
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Filter by severity level
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Filter by resource accessed
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Filter by action performed
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int? MaxResults { get; set; } = 1000;

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Security health status information
/// </summary>
public class SecurityHealthStatus
{
    /// <summary>
    /// Overall security health status
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Time when the health check was performed
    /// </summary>
    public DateTime CheckTime { get; set; }

    /// <summary>
    /// Number of active security threats
    /// </summary>
    public int ActiveThreatsCount { get; set; }

    /// <summary>
    /// Number of failed login attempts in the last hour
    /// </summary>
    public int RecentFailedLogins { get; set; }

    /// <summary>
    /// Number of suspicious activities detected
    /// </summary>
    public int SuspiciousActivities { get; set; }

    /// <summary>
    /// List of security issues or concerns
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Security recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Security metrics summary
    /// </summary>
    public SecurityMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Overall security score (0-100)
    /// </summary>
    public int SecurityScore { get; set; }

    /// <summary>
    /// Security level classification
    /// </summary>
    public SecurityLevel Level { get; set; } = SecurityLevel.Medium;
}

/// <summary>
/// Security metrics for health monitoring
/// </summary>
public class SecurityMetrics
{
    /// <summary>
    /// Total security events in the last 24 hours
    /// </summary>
    public int TotalEvents24h { get; set; }

    /// <summary>
    /// Successful authentication rate (percentage)
    /// </summary>
    public decimal AuthenticationSuccessRate { get; set; }

    /// <summary>
    /// Number of unique users active in the last 24 hours
    /// </summary>
    public int ActiveUsers24h { get; set; }

    /// <summary>
    /// Number of unique IP addresses in the last 24 hours
    /// </summary>
    public int UniqueIpAddresses24h { get; set; }

    /// <summary>
    /// Average time between security events (in minutes)
    /// </summary>
    public double AverageEventInterval { get; set; }

    /// <summary>
    /// Number of blocked IP addresses
    /// </summary>
    public int BlockedIpAddresses { get; set; }

    /// <summary>
    /// Number of users with 2FA enabled
    /// </summary>
    public int TwoFactorEnabledUsers { get; set; }

    /// <summary>
    /// Percentage of users with 2FA enabled
    /// </summary>
    public decimal TwoFactorAdoptionRate { get; set; }
}

/// <summary>
/// Security level classification
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// Critical security issues detected
    /// </summary>
    Critical = 0,

    /// <summary>
    /// High security risk
    /// </summary>
    High = 1,

    /// <summary>
    /// Medium security risk (normal)
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Low security risk
    /// </summary>
    Low = 3,

    /// <summary>
    /// Optimal security status
    /// </summary>
    Optimal = 4
}

/// <summary>
/// Security threat summary for dashboard
/// </summary>
public class SecurityThreatSummary
{
    /// <summary>
    /// Total number of active threats
    /// </summary>
    public int TotalActiveThreats { get; set; }

    /// <summary>
    /// Number of critical threats
    /// </summary>
    public int CriticalThreats { get; set; }

    /// <summary>
    /// Number of high severity threats
    /// </summary>
    public int HighSeverityThreats { get; set; }

    /// <summary>
    /// Number of medium severity threats
    /// </summary>
    public int MediumSeverityThreats { get; set; }

    /// <summary>
    /// Number of low severity threats
    /// </summary>
    public int LowSeverityThreats { get; set; }

    /// <summary>
    /// Most recent threat detection time
    /// </summary>
    public DateTime? LastThreatDetected { get; set; }

    /// <summary>
    /// Most common threat type
    /// </summary>
    public string? MostCommonThreatType { get; set; }

    /// <summary>
    /// Threat detection trend (increasing/decreasing)
    /// </summary>
    public string ThreatTrend { get; set; } = "stable";
}

/// <summary>
/// Security audit summary for reporting
/// </summary>
public class SecurityAuditSummary
{
    /// <summary>
    /// Report period
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Total number of security events
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of successful events
    /// </summary>
    public int SuccessfulEvents { get; set; }

    /// <summary>
    /// Number of failed events
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Number of unique users
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Number of unique IP addresses
    /// </summary>
    public int UniqueIpAddresses { get; set; }

    /// <summary>
    /// Events grouped by type
    /// </summary>
    public Dictionary<string, int> EventsByType { get; set; } = new();

    /// <summary>
    /// Events grouped by hour of day
    /// </summary>
    public Dictionary<int, int> EventsByHour { get; set; } = new();

    /// <summary>
    /// Top IP addresses by event count
    /// </summary>
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();

    /// <summary>
    /// Top users by event count
    /// </summary>
    public Dictionary<string, int> TopUsers { get; set; } = new();
}

/// <summary>
/// Data validation result for integrity checks
/// </summary>
public class DataValidationResult
{
    /// <summary>
    /// Overall validation status
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Time when validation was performed
    /// </summary>
    public DateTime ValidationTime { get; set; }

    /// <summary>
    /// List of validation issues found
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Number of records validated
    /// </summary>
    public int RecordsValidated { get; set; }

    /// <summary>
    /// Validation execution time in milliseconds
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Validation summary by category
    /// </summary>
    public Dictionary<string, int> IssuesByCategory { get; set; } = new();
}

/// <summary>
/// Individual validation issue
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Issue category (e.g., "ForeignKey", "DataType", "Constraint")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Issue severity level
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Description of the issue
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Table name where issue was found
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Column name where issue was found (if applicable)
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Record identifier where issue was found (if applicable)
    /// </summary>
    public string? RecordId { get; set; }

    /// <summary>
    /// Suggested fix for the issue
    /// </summary>
    public string? SuggestedFix { get; set; }

    /// <summary>
    /// Whether this issue can be automatically fixed
    /// </summary>
    public bool CanAutoFix { get; set; }
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Export request for data export operations
/// </summary>
public class ExportRequest
{
    /// <summary>
    /// Export format (CSV, JSON, XML, Excel)
    /// </summary>
    public string Format { get; set; } = "CSV";

    /// <summary>
    /// Tables or entities to export
    /// </summary>
    public List<string> Tables { get; set; } = new();

    /// <summary>
    /// Date range for export (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Export filters (optional)
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();

    /// <summary>
    /// Output file path
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include related data
    /// </summary>
    public bool IncludeRelatedData { get; set; } = true;

    /// <summary>
    /// Compression settings
    /// </summary>
    public bool CompressOutput { get; set; } = false;
}

/// <summary>
/// Export operation result
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Whether export was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Export file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Number of records exported
    /// </summary>
    public int RecordsExported { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Export execution time in milliseconds
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Export format used
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Error message if export failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Export summary by table
    /// </summary>
    public Dictionary<string, int> RecordsByTable { get; set; } = new();
}

/// <summary>
/// Import request for data import operations
/// </summary>
public class ImportRequest
{
    /// <summary>
    /// Import file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Import format (CSV, JSON, XML, Excel)
    /// </summary>
    public string Format { get; set; } = "CSV";

    /// <summary>
    /// Target table or entity
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;

    /// <summary>
    /// Import mode (Insert, Update, Upsert, Replace)
    /// </summary>
    public ImportMode Mode { get; set; } = ImportMode.Insert;

    /// <summary>
    /// Column mappings (source -> target)
    /// </summary>
    public Dictionary<string, string> ColumnMappings { get; set; } = new();

    /// <summary>
    /// Whether to validate data before import
    /// </summary>
    public bool ValidateBeforeImport { get; set; } = true;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to skip invalid records
    /// </summary>
    public bool SkipInvalidRecords { get; set; } = false;
}

/// <summary>
/// Import modes
/// </summary>
public enum ImportMode
{
    Insert,
    Update,
    Upsert,
    Replace
}

/// <summary>
/// Import operation result
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Whether import was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of records processed
    /// </summary>
    public int RecordsProcessed { get; set; }

    /// <summary>
    /// Number of records successfully imported
    /// </summary>
    public int RecordsImported { get; set; }

    /// <summary>
    /// Number of records skipped
    /// </summary>
    public int RecordsSkipped { get; set; }

    /// <summary>
    /// Number of records with errors
    /// </summary>
    public int RecordsWithErrors { get; set; }

    /// <summary>
    /// Import execution time in milliseconds
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Import errors
    /// </summary>
    public List<ImportError> Errors { get; set; } = new();

    /// <summary>
    /// Error message if import failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Import validation result
/// </summary>
public class ImportValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Number of records validated
    /// </summary>
    public int RecordsValidated { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<ImportError> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<ImportError> Warnings { get; set; } = new();

    /// <summary>
    /// Validation execution time in milliseconds
    /// </summary>
    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// Import error information
/// </summary>
public class ImportError
{
    /// <summary>
    /// Row number where error occurred
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Column name where error occurred
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error severity
    /// </summary>
    public ImportErrorSeverity Severity { get; set; }

    /// <summary>
    /// Raw value that caused the error
    /// </summary>
    public string? RawValue { get; set; }
}

/// <summary>
/// Import error severity levels
/// </summary>
public enum ImportErrorSeverity
{
    Warning,
    Error,
    Critical
}

// CreateIndicatorRequest moved to IndicatorModels.cs - duplicate DELETED

/// <summary>
/// Request model for updating Indicators
/// </summary>
public class UpdateIndicatorSecurityRequest
{
    /// <summary>
    /// Indicator ID to update
    /// </summary>
    public long IndicatorId { get; set; }

    /// <summary>
    /// Indicator name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Indicator description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Stored procedure name for Indicator execution (optional)
    /// </summary>
    public string? StoredProcedureName { get; set; }

    /// <summary>
    /// Indicator owner (optional)
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Whether the Indicator is active (optional)
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Last minutes parameter for KPI execution (optional)
    /// </summary>
    public int? LastMinutes { get; set; }
}

/// <summary>
/// Request model for creating contacts
/// </summary>
public class CreateContactRequest
{
    /// <summary>
    /// Contact name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Slack user ID
    /// </summary>
    public string? SlackUserId { get; set; }

    /// <summary>
    /// Teams user ID
    /// </summary>
    public string? TeamsUserId { get; set; }

    /// <summary>
    /// Whether the contact is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating contacts
/// </summary>
public class UpdateContactRequest
{
    /// <summary>
    /// Contact ID to update
    /// </summary>
    public int ContactId { get; set; }

    /// <summary>
    /// Contact name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Contact email (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone number (optional)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Slack user ID (optional)
    /// </summary>
    public string? SlackUserId { get; set; }

    /// <summary>
    /// Teams user ID (optional)
    /// </summary>
    public string? TeamsUserId { get; set; }

    /// <summary>
    /// Whether the contact is active (optional)
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Result of bulk operations
/// </summary>
public class BulkOperationResult
{
    /// <summary>
    /// Whether the bulk operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Total number of items requested for processing
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of items successfully processed
    /// </summary>
    public int SuccessfulItems { get; set; }

    /// <summary>
    /// Number of items that failed processing
    /// </summary>
    public int FailedItems { get; set; }

    /// <summary>
    /// Individual results for each item
    /// </summary>
    public List<BulkOperationItem> Results { get; set; } = new();

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalItems > 0 ? (double)SuccessfulItems / TotalItems * 100 : 0;

    /// <summary>
    /// List of error messages for failed operations
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Additional metadata about the operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Operation start time
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation end time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Operation type or description
    /// </summary>
    public string? OperationType { get; set; }
}
