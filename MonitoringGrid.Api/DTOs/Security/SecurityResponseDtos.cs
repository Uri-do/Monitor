using MonitoringGrid.Api.DTOs.Common;

namespace MonitoringGrid.Api.DTOs.Security;

/// <summary>
/// Enhanced security configuration response
/// </summary>
public class SecurityConfigResponse
{
    /// <summary>
    /// Password policy settings
    /// </summary>
    public PasswordPolicyResponse PasswordPolicy { get; set; } = new();

    /// <summary>
    /// Session policy settings
    /// </summary>
    public SessionPolicyResponse SessionPolicy { get; set; } = new();

    /// <summary>
    /// Lockout policy settings
    /// </summary>
    public LockoutPolicyResponse LockoutPolicy { get; set; } = new();

    /// <summary>
    /// Two-factor authentication settings
    /// </summary>
    public TwoFactorPolicyResponse TwoFactorPolicy { get; set; } = new();

    /// <summary>
    /// Audit policy settings
    /// </summary>
    public AuditPolicyResponse AuditPolicy { get; set; } = new();

    /// <summary>
    /// Configuration last modified date
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Who last modified the configuration
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Password policy response
/// </summary>
public class PasswordPolicyResponse
{
    /// <summary>
    /// Minimum password length
    /// </summary>
    public int MinimumLength { get; set; }

    /// <summary>
    /// Require uppercase letters
    /// </summary>
    public bool RequireUppercase { get; set; }

    /// <summary>
    /// Require lowercase letters
    /// </summary>
    public bool RequireLowercase { get; set; }

    /// <summary>
    /// Require numbers
    /// </summary>
    public bool RequireNumbers { get; set; }

    /// <summary>
    /// Require digits
    /// </summary>
    public bool RequireDigits { get; set; }

    /// <summary>
    /// Require special characters
    /// </summary>
    public bool RequireSpecialChars { get; set; }

    /// <summary>
    /// Require special characters (alias)
    /// </summary>
    public bool RequireSpecialCharacters { get; set; }

    /// <summary>
    /// Password expiration in days
    /// </summary>
    public int PasswordExpirationDays { get; set; }

    /// <summary>
    /// Maximum failed login attempts
    /// </summary>
    public int MaxFailedAttempts { get; set; }

    /// <summary>
    /// Account lockout duration in minutes
    /// </summary>
    public int LockoutDurationMinutes { get; set; }

    /// <summary>
    /// Password max age
    /// </summary>
    public TimeSpan MaxAge { get; set; }

    /// <summary>
    /// Prevent reuse count
    /// </summary>
    public int PreventReuse { get; set; }
}

/// <summary>
/// Session settings response
/// </summary>
public class SessionSettingsResponse
{
    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; }

    /// <summary>
    /// Idle timeout in minutes
    /// </summary>
    public int IdleTimeoutMinutes { get; set; }

    /// <summary>
    /// Allow concurrent sessions
    /// </summary>
    public bool AllowConcurrentSessions { get; set; }

    /// <summary>
    /// Current active sessions count
    /// </summary>
    public int ActiveSessionsCount { get; set; }
}

/// <summary>
/// Two-factor authentication settings response
/// </summary>
public class TwoFactorSettingsResponse
{
    /// <summary>
    /// Two-factor authentication enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Two-factor authentication required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Available two-factor methods
    /// </summary>
    public List<string> Methods { get; set; } = new();

    /// <summary>
    /// Users with two-factor enabled count
    /// </summary>
    public int UsersWithTwoFactorCount { get; set; }
}

/// <summary>
/// Rate limiting settings response
/// </summary>
public class RateLimitSettingsResponse
{
    /// <summary>
    /// Rate limiting enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum requests per minute
    /// </summary>
    public int MaxRequestsPerMinute { get; set; }

    /// <summary>
    /// Maximum requests per hour
    /// </summary>
    public int MaxRequestsPerHour { get; set; }

    /// <summary>
    /// Current rate limit violations count
    /// </summary>
    public int CurrentViolationsCount { get; set; }
}

/// <summary>
/// Session policy response
/// </summary>
public class SessionPolicyResponse
{
    /// <summary>
    /// Session timeout
    /// </summary>
    public TimeSpan SessionTimeout { get; set; }

    /// <summary>
    /// Maximum concurrent sessions
    /// </summary>
    public int MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Require reauthentication
    /// </summary>
    public bool RequireReauthentication { get; set; }

    /// <summary>
    /// Idle timeout
    /// </summary>
    public TimeSpan IdleTimeout { get; set; }
}

/// <summary>
/// Lockout policy response
/// </summary>
public class LockoutPolicyResponse
{
    /// <summary>
    /// Maximum failed attempts
    /// </summary>
    public int MaxFailedAttempts { get; set; }

    /// <summary>
    /// Lockout duration
    /// </summary>
    public TimeSpan LockoutDuration { get; set; }

    /// <summary>
    /// Reset counter after
    /// </summary>
    public TimeSpan ResetCounterAfter { get; set; }
}

/// <summary>
/// Two-factor policy response
/// </summary>
public class TwoFactorPolicyResponse
{
    /// <summary>
    /// Is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Allowed providers
    /// </summary>
    public List<string> AllowedProviders { get; set; } = new();

    /// <summary>
    /// Token lifetime
    /// </summary>
    public TimeSpan TokenLifetime { get; set; }
}

/// <summary>
/// Audit policy response
/// </summary>
public class AuditPolicyResponse
{
    /// <summary>
    /// Log all events
    /// </summary>
    public bool LogAllEvents { get; set; }

    /// <summary>
    /// Retention period
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; }

    /// <summary>
    /// Log sensitive data
    /// </summary>
    public bool LogSensitiveData { get; set; }

    /// <summary>
    /// Enable real-time alerts
    /// </summary>
    public bool EnableRealTimeAlerts { get; set; }
}

/// <summary>
/// Enhanced security event response
/// </summary>
public class SecurityEventResponse
{
    /// <summary>
    /// Event ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Action performed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Resource affected
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Additional event data
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// Risk score (computed)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Time since event
    /// </summary>
    public TimeSpan TimeSinceEvent { get; set; }
}

/// <summary>
/// Paginated security events response
/// </summary>
public class PaginatedSecurityEventsResponse
{
    /// <summary>
    /// List of security events
    /// </summary>
    public List<SecurityEventResponse> Events { get; set; } = new();

    /// <summary>
    /// Total count of events (before pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Security events summary
    /// </summary>
    public SecurityEventsSummary Summary { get; set; } = new();

    /// <summary>
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Security events summary
/// </summary>
public class SecurityEventsSummary
{
    /// <summary>
    /// Total events in current filter
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Successful events count
    /// </summary>
    public int SuccessfulEvents { get; set; }

    /// <summary>
    /// Failed events count
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// High risk events count
    /// </summary>
    public int HighRiskEvents { get; set; }

    /// <summary>
    /// Unique users count
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Unique IP addresses count
    /// </summary>
    public int UniqueIpAddresses { get; set; }
}

/// <summary>
/// Enhanced user response
/// </summary>
public class UserResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the user is locked out
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Failed login attempts count
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Whether two-factor authentication is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Additional user details (if requested)
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Paginated users response
/// </summary>
public class PaginatedUsersResponse
{
    /// <summary>
    /// List of users
    /// </summary>
    public List<UserResponse> Users { get; set; } = new();

    /// <summary>
    /// Total count of users (before pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Users summary statistics
    /// </summary>
    public UsersSummary Summary { get; set; } = new();

    /// <summary>
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Users summary statistics
/// </summary>
public class UsersSummary
{
    /// <summary>
    /// Total users
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Active users count
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Locked out users count
    /// </summary>
    public int LockedOutUsers { get; set; }

    /// <summary>
    /// Users with two-factor enabled count
    /// </summary>
    public int TwoFactorEnabledUsers { get; set; }

    /// <summary>
    /// Users logged in today count
    /// </summary>
    public int LoggedInTodayUsers { get; set; }
}

/// <summary>
/// Enhanced login response
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Whether login was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if login failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether two-factor authentication is required
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Whether password change is required
    /// </summary>
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// JWT token information
    /// </summary>
    public JwtTokenResponse? Token { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserResponse? User { get; set; }

    /// <summary>
    /// Login attempt timestamp
    /// </summary>
    public DateTime LoginAttemptTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Login duration in milliseconds
    /// </summary>
    public long LoginDurationMs { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Additional login details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// JWT token response
/// </summary>
public class JwtTokenResponse
{
    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token scope
    /// </summary>
    public string? Scope { get; set; }
}

/// <summary>
/// Registration response
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Whether registration was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Created user information
    /// </summary>
    public UserResponse? User { get; set; }

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Registration duration in milliseconds
    /// </summary>
    public long RegistrationDurationMs { get; set; }

    /// <summary>
    /// Validation errors if any
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Additional registration details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Role response
/// </summary>
public class RoleResponse
{
    /// <summary>
    /// Role ID
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Role name
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Role permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Number of users with this role
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Whether the role is system-defined
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Role creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Permission response
/// </summary>
public class PermissionResponse
{
    /// <summary>
    /// Permission ID
    /// </summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// Permission name
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// Permission description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Permission category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether the permission is system-defined
    /// </summary>
    public bool IsSystemPermission { get; set; }
}

/// <summary>
/// Security operation response
/// </summary>
public class SecurityOperationResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Operation result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Operation timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Additional operation details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// User who performed the operation
    /// </summary>
    public string? PerformedBy { get; set; }
}

/// <summary>
/// Generic paged response for API endpoints
/// </summary>
public class PagedResponse<T>
{
    /// <summary>
    /// Data items for current page
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total count of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Security analysis request
/// </summary>
public class SecurityAnalysisRequest
{
    /// <summary>
    /// Analysis start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Analysis end time
    /// </summary>
    public DateTime EndTime { get; set; }
}

/// <summary>
/// Security analysis response
/// </summary>
public class SecurityAnalysisResponse
{
    /// <summary>
    /// Analysis ID
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Analysis start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Analysis end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Analysis date
    /// </summary>
    public DateTime AnalysisDate { get; set; }

    /// <summary>
    /// Whether suspicious activity was detected
    /// </summary>
    public bool HasSuspiciousActivity { get; set; }

    /// <summary>
    /// Analysis duration in milliseconds
    /// </summary>
    public long AnalysisDurationMs { get; set; }

    /// <summary>
    /// Analysis summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Additional analysis details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Security statistics response
/// </summary>
public class SecurityStatisticsResponse
{
    /// <summary>
    /// Statistics period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Statistics period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total events count
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Successful events count
    /// </summary>
    public int SuccessfulEvents { get; set; }

    /// <summary>
    /// Failed events count
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Unique users count
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Unique IP addresses count
    /// </summary>
    public int UniqueIpAddresses { get; set; }

    /// <summary>
    /// Events grouped by type
    /// </summary>
    public Dictionary<string, int> EventsByType { get; set; } = new();

    /// <summary>
    /// Events grouped by day
    /// </summary>
    public Dictionary<DateTime, int> EventsByDay { get; set; } = new();

    /// <summary>
    /// Statistics generation time
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Generation duration in milliseconds
    /// </summary>
    public long GenerationDurationMs { get; set; }
}

/// <summary>
/// Roles and permissions response
/// </summary>
public class RolesPermissionsResponse
{
    /// <summary>
    /// List of roles
    /// </summary>
    public List<RoleResponse> Roles { get; set; } = new();

    /// <summary>
    /// List of permissions
    /// </summary>
    public List<PermissionResponse> Permissions { get; set; } = new();

    /// <summary>
    /// Response generation time
    /// </summary>
    public DateTime RetrievedAt { get; set; }

    /// <summary>
    /// Retrieval duration in milliseconds
    /// </summary>
    public long RetrievalDurationMs { get; set; }
}


