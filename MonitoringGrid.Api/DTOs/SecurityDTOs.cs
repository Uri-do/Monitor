using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// Security configuration DTO
/// </summary>
public class SecurityConfigDto
{
    public PasswordPolicyDto PasswordPolicy { get; set; } = new();
    public SessionSettingsDto SessionSettings { get; set; } = new();
    public TwoFactorSettingsDto TwoFactorSettings { get; set; } = new();
    public RateLimitSettingsDto RateLimitSettings { get; set; } = new();
}

/// <summary>
/// Password policy DTO
/// </summary>
public class PasswordPolicyDto
{
    [Range(6, 128)]
    public int MinimumLength { get; set; } = 8;
    
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireNumbers { get; set; } = true;
    public bool RequireSpecialChars { get; set; } = true;
    
    [Range(1, 365)]
    public int PasswordExpirationDays { get; set; } = 90;
    
    [Range(1, 10)]
    public int MaxFailedAttempts { get; set; } = 5;
    
    [Range(1, 1440)]
    public int LockoutDurationMinutes { get; set; } = 30;
}

/// <summary>
/// Session settings DTO
/// </summary>
public class SessionSettingsDto
{
    [Range(5, 1440)]
    public int SessionTimeoutMinutes { get; set; } = 480;
    
    [Range(5, 240)]
    public int IdleTimeoutMinutes { get; set; } = 60;
    
    public bool AllowConcurrentSessions { get; set; } = false;
}

/// <summary>
/// Two-factor authentication settings DTO
/// </summary>
public class TwoFactorSettingsDto
{
    public bool Enabled { get; set; } = false;
    public bool Required { get; set; } = false;
    public List<string> Methods { get; set; } = new() { "TOTP", "SMS", "Email" };
}

/// <summary>
/// Rate limiting settings DTO
/// </summary>
public class RateLimitSettingsDto
{
    public bool Enabled { get; set; } = true;
    
    [Range(1, 10000)]
    public int MaxRequestsPerMinute { get; set; } = 100;
    
    [Range(1, 100000)]
    public int MaxRequestsPerHour { get; set; } = 1000;
}

/// <summary>
/// Security event DTO
/// </summary>
public class SecurityEventDto
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// API key DTO
/// </summary>
public class ApiKeyDto
{
    public string KeyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
    public string? LastUsed { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Create API key request DTO
/// </summary>
public class CreateApiKeyRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// Create API key response DTO
/// </summary>
public class CreateApiKeyResponseDto
{
    public string KeyId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Update user roles request DTO
/// </summary>
public class UpdateUserRolesDto
{
    [Required]
    public List<string> Roles { get; set; } = new();
}