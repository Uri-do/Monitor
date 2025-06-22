using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Core.Entities;
using System.Security.Claims;

namespace MonitoringGrid.Core.Security;

/// <summary>
/// User authentication model
/// </summary>
public class AuthUser
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public Dictionary<string, object> Claims { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// JWT token model
/// </summary>
public class JwtToken
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; }
    
    public string? TwoFactorCode { get; set; }
}

/// <summary>
/// Login response model
/// </summary>
public class LoginResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public JwtToken? Token { get; set; }
    public User? User { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresPasswordChange { get; set; }
}

/// <summary>
/// Token refresh request
/// </summary>
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Password change request
/// </summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Role definition
/// </summary>
public class Role
{
    public string RoleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsSystemRole { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Permission definition
/// </summary>
public class Permission
{
    public string PermissionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsSystemPermission { get; set; }
}

/// <summary>
/// API key model
/// </summary>
public class ApiKey
{
    public string KeyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HashedKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUsed { get; set; }
    public string? LastUsedFrom { get; set; }
}

/// <summary>
/// Security configuration
/// </summary>
public class SecurityConfiguration
{
    public JwtSettings Jwt { get; set; } = new();
    public PasswordPolicy PasswordPolicy { get; set; } = new();
    public SessionSettings Session { get; set; } = new();
    public TwoFactorSettings TwoFactor { get; set; } = new();
    public RateLimitSettings RateLimit { get; set; } = new();
    public AzureAdSettings AzureAd { get; set; } = new();
    public EncryptionSettings Encryption { get; set; } = new();
}

/// <summary>
/// JWT settings
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
    public string Algorithm { get; set; } = "HS256";
}

/// <summary>
/// Password policy settings
/// </summary>
public class PasswordPolicy
{
    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int PasswordHistoryCount { get; set; } = 5;
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 30;
    public int PasswordExpirationDays { get; set; } = 90;
}

/// <summary>
/// Session settings
/// </summary>
public class SessionSettings
{
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
    public int IdleTimeoutMinutes { get; set; } = 60;
    public bool RequireHttps { get; set; } = true;
    public bool SecureCookies { get; set; } = true;
    public string CookieDomain { get; set; } = string.Empty;
}

/// <summary>
/// Two-factor authentication settings
/// </summary>
public class TwoFactorSettings
{
    public bool IsEnabled { get; set; } = false;
    public bool IsRequired { get; set; } = false;
    public List<string> EnabledProviders { get; set; } = new() { "TOTP", "SMS", "Email" };
    public int CodeExpirationMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;
}

/// <summary>
/// Rate limiting settings
/// </summary>
public class RateLimitSettings
{
    public bool IsEnabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
    public List<string> ExemptedIpAddresses { get; set; } = new();
    public List<string> ExemptedUserAgents { get; set; } = new();
}

/// <summary>
/// Azure AD settings
/// </summary>
public class AzureAdSettings
{
    public bool IsEnabled { get; set; } = false;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public List<string> ValidAudiences { get; set; } = new();
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
}

/// <summary>
/// Encryption settings
/// </summary>
public class EncryptionSettings
{
    public string DataProtectionKeyPath { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public string HashingSalt { get; set; } = string.Empty;
    public bool UseHardwareSecurityModule { get; set; } = false;
    public AzureKeyVaultSettings KeyVault { get; set; } = new();
}

/// <summary>
/// Azure Key Vault settings
/// </summary>
public class AzureKeyVaultSettings
{
    public bool IsEnabled { get; set; } = false;
    public string VaultUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; } = true;
}

/// <summary>
/// Security audit event
/// </summary>
public class SecurityAuditEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = "Information";

    // Alias for compatibility with services that expect Description
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Security threat detection
/// </summary>
public class SecurityThreat
{
    public string ThreatId { get; set; } = string.Empty;
    public string ThreatType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public Dictionary<string, object> ThreatData { get; set; } = new();
}

/// <summary>
/// User two-factor authentication settings
/// </summary>
public class UserTwoFactorSettings
{
    public string UserId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Secret { get; set; }
    public List<string> RecoveryCodes { get; set; } = new();
    public DateTime? EnabledAt { get; set; }
}
