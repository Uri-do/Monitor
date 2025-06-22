using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.DTOs.Security;

/// <summary>
/// Request DTO for getting security configuration
/// </summary>
public class GetSecurityConfigRequest
{
    /// <summary>
    /// Include sensitive configuration details
    /// </summary>
    [BooleanFlag]
    public bool IncludeSensitive { get; set; } = false;

    /// <summary>
    /// Include detailed policy information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Request DTO for updating security configuration
/// </summary>
public class UpdateSecurityConfigRequest
{
    /// <summary>
    /// Password policy settings
    /// </summary>
    public PasswordPolicyRequest? PasswordPolicy { get; set; }

    /// <summary>
    /// Session policy settings
    /// </summary>
    public SessionPolicyRequest? SessionPolicy { get; set; }

    /// <summary>
    /// Lockout policy settings
    /// </summary>
    public LockoutPolicyRequest? LockoutPolicy { get; set; }

    /// <summary>
    /// Two-factor authentication settings
    /// </summary>
    public TwoFactorPolicyRequest? TwoFactorPolicy { get; set; }

    /// <summary>
    /// Audit policy settings
    /// </summary>
    public AuditPolicyRequest? AuditPolicy { get; set; }

    /// <summary>
    /// Reason for configuration change
    /// </summary>
    [SearchTerm(0, 500)]
    public string? ChangeReason { get; set; }
}

/// <summary>
/// Password policy request settings
/// </summary>
public class PasswordPolicyRequest
{
    /// <summary>
    /// Minimum password length
    /// </summary>
    [Range(6, 128)]
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Require uppercase letters
    /// </summary>
    [BooleanFlag]
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Require lowercase letters
    /// </summary>
    [BooleanFlag]
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Require numbers (alias for RequireDigit)
    /// </summary>
    [BooleanFlag]
    public bool RequireNumbers { get; set; } = true;

    /// <summary>
    /// Require digits
    /// </summary>
    [BooleanFlag]
    public bool RequireDigits { get; set; } = true;

    /// <summary>
    /// Require special characters
    /// </summary>
    [BooleanFlag]
    public bool RequireSpecialChars { get; set; } = true;

    /// <summary>
    /// Require special characters (alias)
    /// </summary>
    [BooleanFlag]
    public bool RequireSpecialCharacters { get; set; } = true;

    /// <summary>
    /// Password expiration in days
    /// </summary>
    [Range(0, 365)]
    public int PasswordExpirationDays { get; set; } = 90;

    /// <summary>
    /// Maximum failed login attempts
    /// </summary>
    [Range(3, 20)]
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Account lockout duration in minutes
    /// </summary>
    [Range(5, 1440)]
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Password maximum age
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Number of previous passwords to prevent reuse
    /// </summary>
    [Range(0, 24)]
    public int PreventReuse { get; set; } = 5;
}

/// <summary>
/// Session settings request
/// </summary>
public class SessionSettingsRequest
{
    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    [Range(5, 1440)]
    public int SessionTimeoutMinutes { get; set; } = 480;

    /// <summary>
    /// Idle timeout in minutes
    /// </summary>
    [Range(5, 240)]
    public int IdleTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Allow concurrent sessions
    /// </summary>
    [BooleanFlag]
    public bool AllowConcurrentSessions { get; set; } = true;
}

/// <summary>
/// Two-factor authentication settings request
/// </summary>
public class TwoFactorSettingsRequest
{
    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    [BooleanFlag]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Require two-factor authentication for all users
    /// </summary>
    [BooleanFlag]
    public bool Required { get; set; } = false;

    /// <summary>
    /// Enabled two-factor methods
    /// </summary>
    public List<string> Methods { get; set; } = new();
}

/// <summary>
/// Rate limiting settings request
/// </summary>
public class RateLimitSettingsRequest
{
    /// <summary>
    /// Enable rate limiting
    /// </summary>
    [BooleanFlag]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute
    /// </summary>
    [Range(10, 10000)]
    public int MaxRequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Maximum requests per hour
    /// </summary>
    [Range(100, 100000)]
    public int MaxRequestsPerHour { get; set; } = 1000;
}

/// <summary>
/// Request DTO for getting security events
/// </summary>
public class GetSecurityEventsRequest
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
    /// Filter by user ID
    /// </summary>
    [SearchTerm(0, 100)]
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by event type
    /// </summary>
    [SearchTerm(0, 100)]
    public string? EventType { get; set; }

    /// <summary>
    /// Filter by action
    /// </summary>
    [SearchTerm(0, 50)]
    public string? Action { get; set; }

    /// <summary>
    /// Filter by success status
    /// </summary>
    [BooleanFlag]
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    [PositiveInteger]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    [PageSize(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    [SearchTerm(0, 50)]
    public string? SortBy { get; set; } = "timestamp";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    [SearchTerm(0, 10)]
    public string? SortDirection { get; set; } = "desc";

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;
}

/// <summary>
/// Request DTO for getting users
/// </summary>
public class GetUsersRequest
{
    /// <summary>
    /// Search text for filtering users
    /// </summary>
    [SearchTerm(0, 200)]
    public string? SearchText { get; set; }

    /// <summary>
    /// Filter by role
    /// </summary>
    [SearchTerm(0, 50)]
    public string? Role { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [BooleanFlag]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    [PositiveInteger]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    [PageSize(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Include user roles
    /// </summary>
    [BooleanFlag]
    public bool IncludeRoles { get; set; } = true;
}

/// <summary>
/// Request DTO for updating user roles
/// </summary>
public class UpdateUserRolesRequest
{
    /// <summary>
    /// User ID to update
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// List of role names to assign
    /// </summary>
    [Required]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Reason for role change
    /// </summary>
    [SearchTerm(0, 500)]
    public string? ChangeReason { get; set; }
}

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username or email
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [SearchTerm(1, 200)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remember login
    /// </summary>
    [BooleanFlag]
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    [SearchTerm(0, 10)]
    public string? TwoFactorCode { get; set; }
}

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Username
    /// </summary>
    [Required]
    [SearchTerm(3, 50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    [SearchTerm(5, 100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [SearchTerm(6, 200)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirm password
    /// </summary>
    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [Required]
    [SearchTerm(1, 50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required]
    [SearchTerm(1, 50)]
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for token refresh
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token
    /// </summary>
    [Required]
    [SearchTerm(1, 500)]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating user information
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    [EmailAddress]
    [SearchTerm(5, 100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [SearchTerm(1, 50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [SearchTerm(1, 50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is active
    /// </summary>
    [BooleanFlag]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for changing password
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    [Required]
    [SearchTerm(1, 200)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required]
    [SearchTerm(6, 200)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Session policy request settings
/// </summary>
public class SessionPolicyRequest
{
    /// <summary>
    /// Session timeout
    /// </summary>
    public TimeSpan? SessionTimeout { get; set; }

    /// <summary>
    /// Maximum concurrent sessions
    /// </summary>
    [Range(1, 100)]
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Require reauthentication
    /// </summary>
    [BooleanFlag]
    public bool? RequireReauthentication { get; set; }

    /// <summary>
    /// Idle timeout
    /// </summary>
    public TimeSpan? IdleTimeout { get; set; }
}

/// <summary>
/// Lockout policy request settings
/// </summary>
public class LockoutPolicyRequest
{
    /// <summary>
    /// Maximum failed attempts
    /// </summary>
    [Range(1, 100)]
    public int? MaxFailedAttempts { get; set; }

    /// <summary>
    /// Lockout duration
    /// </summary>
    public TimeSpan? LockoutDuration { get; set; }

    /// <summary>
    /// Reset counter after
    /// </summary>
    public TimeSpan? ResetCounterAfter { get; set; }
}

/// <summary>
/// Two-factor policy request settings
/// </summary>
public class TwoFactorPolicyRequest
{
    /// <summary>
    /// Is enabled
    /// </summary>
    [BooleanFlag]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Is required
    /// </summary>
    [BooleanFlag]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// Allowed providers
    /// </summary>
    public List<string>? AllowedProviders { get; set; }

    /// <summary>
    /// Token lifetime
    /// </summary>
    public TimeSpan? TokenLifetime { get; set; }
}

/// <summary>
/// Audit policy request settings
/// </summary>
public class AuditPolicyRequest
{
    /// <summary>
    /// Log all events
    /// </summary>
    [BooleanFlag]
    public bool? LogAllEvents { get; set; }

    /// <summary>
    /// Retention period
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; }

    /// <summary>
    /// Log sensitive data
    /// </summary>
    [BooleanFlag]
    public bool? LogSensitiveData { get; set; }

    /// <summary>
    /// Enable real-time alerts
    /// </summary>
    [BooleanFlag]
    public bool? EnableRealTimeAlerts { get; set; }
}
