namespace MonitoringGrid.Api.Models;

/// <summary>
/// Security validation result
/// </summary>
public class SecurityValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ApiKeyInfo? ApiKeyInfo { get; set; }
    public Dictionary<string, object> Claims { get; set; } = new();

    public static SecurityValidationResult Success(ApiKeyInfo apiKeyInfo)
    {
        return new SecurityValidationResult
        {
            IsValid = true,
            ApiKeyInfo = apiKeyInfo,
            Claims = new Dictionary<string, object>
            {
                ["user_id"] = apiKeyInfo.UserId,
                ["key_id"] = apiKeyInfo.KeyId,
                ["scope"] = apiKeyInfo.Scope ?? "api:read"
            }
        };
    }

    public static SecurityValidationResult Failed(string errorMessage)
    {
        return new SecurityValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// API key information
/// </summary>
public class ApiKeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string HashedKey { get; set; } = string.Empty;
    public string? PlainKey { get; set; } // Only available during generation
    public string UserId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked { get; set; }
    public long UsageCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Security event for audit logging
/// </summary>
public class SecurityEvent
{
    public string EventId { get; set; } = string.Empty;
    public SecurityEventType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    ApiKeyGenerated,
    ApiKeyRevoked,
    InvalidApiKey,
    ExpiredApiKey,
    RevokedApiKey,
    SuspiciousActivity,
    BruteForceAttempt,
    UnauthorizedAccess,
    DataBreach,
    SecurityViolation
}

/// <summary>
/// Trusted source information
/// </summary>
public class TrustedSource
{
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsTrusted { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Security metrics
/// </summary>
public class SecurityMetrics
{
    public TimeSpan Period { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public int UniqueIpAddresses { get; set; }
    public int UniqueUsers { get; set; }
    public List<ThreatInfo> TopThreats { get; set; } = new();
    public double SecurityScore { get; set; }
}

/// <summary>
/// Threat information
/// </summary>
public class ThreatInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public DateTime LastEventTime { get; set; }
    public ThreatLevel ThreatLevel { get; set; }
    public List<string> EventTypes { get; set; } = new();
}

/// <summary>
/// Threat levels
/// </summary>
public enum ThreatLevel
{
    Minimal,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Security configuration
/// </summary>
public class SecurityConfiguration
{
    public ApiKeySettings ApiKeys { get; set; } = new();
    public EncryptionSettings Encryption { get; set; } = new();
    public ThreatDetectionSettings ThreatDetection { get; set; } = new();
    public AuditSettings Audit { get; set; } = new();
}

/// <summary>
/// API key settings
/// </summary>
public class ApiKeySettings
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromDays(365);
    public int MaxKeysPerUser { get; set; } = 10;
    public bool RequireExpiration { get; set; } = false;
    public List<string> AllowedScopes { get; set; } = new() { "api:read", "api:write", "api:admin" };
}

/// <summary>
/// Encryption settings
/// </summary>
public class EncryptionSettings
{
    public string Algorithm { get; set; } = "AES-256-GCM";
    public int KeySize { get; set; } = 256;
    public bool RotateKeys { get; set; } = true;
    public TimeSpan KeyRotationInterval { get; set; } = TimeSpan.FromDays(90);
}

/// <summary>
/// Threat detection settings
/// </summary>
public class ThreatDetectionSettings
{
    public bool EnableBruteForceDetection { get; set; } = true;
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    public bool EnableAnomalyDetection { get; set; } = true;
    public double AnomalyThreshold { get; set; } = 0.8;
}

/// <summary>
/// Audit settings
/// </summary>
public class AuditSettings
{
    public bool EnableAuditLogging { get; set; } = true;
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(90);
    public bool LogSuccessfulOperations { get; set; } = false;
    public bool LogFailedOperations { get; set; } = true;
    public List<string> SensitiveFields { get; set; } = new() { "password", "token", "key", "secret" };
}

/// <summary>
/// Security scan result
/// </summary>
public class SecurityScanResult
{
    public DateTime ScanTime { get; set; }
    public TimeSpan Duration { get; set; }
    public SecurityScanStatus Status { get; set; }
    public List<SecurityVulnerability> Vulnerabilities { get; set; } = new();
    public List<SecurityRecommendation> Recommendations { get; set; } = new();
    public double SecurityScore { get; set; }
}

/// <summary>
/// Security scan status
/// </summary>
public enum SecurityScanStatus
{
    Passed,
    Warning,
    Failed,
    Error
}

/// <summary>
/// Security vulnerability
/// </summary>
public class SecurityVulnerability
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VulnerabilitySeverity Severity { get; set; }
    public string Component { get; set; } = string.Empty;
    public string? CveId { get; set; }
    public List<string> References { get; set; } = new();
    public string? Remediation { get; set; }
}

/// <summary>
/// Vulnerability severity levels
/// </summary>
public enum VulnerabilitySeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Security recommendation
/// </summary>
public class SecurityRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationCategory Category { get; set; }
    public RecommendationPriority Priority { get; set; }
    public string Component { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = new();
    public string? Documentation { get; set; }
}

/// <summary>
/// Security recommendation categories
/// </summary>
public enum RecommendationCategory
{
    Authentication,
    Authorization,
    Encryption,
    Configuration,
    Monitoring,
    Compliance
}

/// <summary>
/// Security compliance report
/// </summary>
public class SecurityComplianceReport
{
    public DateTime GeneratedAt { get; set; }
    public string Framework { get; set; } = string.Empty; // e.g., "OWASP", "SOC2", "ISO27001"
    public double ComplianceScore { get; set; }
    public List<ComplianceCheck> Checks { get; set; } = new();
    public List<ComplianceGap> Gaps { get; set; } = new();
}

/// <summary>
/// Compliance check result
/// </summary>
public class ComplianceCheck
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; }
    public string? Evidence { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Compliance status
/// </summary>
public enum ComplianceStatus
{
    Compliant,
    PartiallyCompliant,
    NonCompliant,
    NotApplicable
}

/// <summary>
/// Compliance gap
/// </summary>
public class ComplianceGap
{
    public string Requirement { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Remediation { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; }
}
