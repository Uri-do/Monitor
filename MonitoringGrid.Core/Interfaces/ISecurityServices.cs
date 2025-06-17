using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Security;
using System.Security.Claims;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Enhanced interface for authentication service with Result pattern
/// </summary>
public interface IAuthenticationService
{
    Task<Result<LoginResponse>> AuthenticateAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default);
    Task<Result<JwtToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result<User>> GetUserFromTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string userId, string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Enhanced interface for authorization service with Result pattern
/// </summary>
public interface IAuthorizationService
{
    Task<Result<bool>> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
    Task<Result<bool>> CanAccessResourceAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);
    Task<Result<List<string>>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<List<string>>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    Task<Result> RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Enhanced interface for JWT token service with Result pattern
/// </summary>
public interface IJwtTokenService
{
    Result<string> GenerateAccessToken(User user, List<Claim>? additionalClaims = null);
    Result<string> GenerateRefreshToken();
    Result<ClaimsPrincipal> ValidateToken(string token);
    Result<DateTime> GetTokenExpiration(string token);
    Task<Result<bool>> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);
    Task<Result> BlacklistTokenAsync(string token, DateTime expiration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for API key service
/// </summary>
public interface IApiKeyService
{
    Task<ApiKey> CreateApiKeyAsync(string name, string createdBy, List<string> scopes, DateTime? expiresAt = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateApiKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetApiKeyInfoAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> RevokeApiKeyAsync(string keyId, CancellationToken cancellationToken = default);
    Task<List<ApiKey>> GetApiKeysAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAsync(string keyId, string ipAddress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for role management service
/// </summary>
public interface IRoleManagementService
{
    Task<MonitoringGrid.Core.Entities.Role> CreateRoleAsync(string name, string description, List<string> permissions, CancellationToken cancellationToken = default);
    Task<MonitoringGrid.Core.Entities.Role?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default);
    Task<List<MonitoringGrid.Core.Entities.Role>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(string roleId, string name, string description, List<string> permissions, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
    Task<List<MonitoringGrid.Core.Entities.Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for user management service
/// </summary>
public interface IUserManagementService
{
    Task<User> CreateUserAsync(string username, string email, string displayName, List<string> roles, CancellationToken cancellationToken = default);
    Task<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<User>> GetUsersAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(string userId, string displayName, string? department, string? title, CancellationToken cancellationToken = default);
    Task<bool> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ActivateUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for security audit service
/// </summary>
public interface ISecurityAuditService
{
    Task LogSecurityEventAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task LogLoginAttemptAsync(string username, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default);
    Task LogPasswordChangeAsync(string userId, bool success, CancellationToken cancellationToken = default);
    Task LogPermissionChangeAsync(string userId, string action, string resource, CancellationToken cancellationToken = default);
    Task LogSuspiciousActivityAsync(string? userId, string activityType, string description, string ipAddress, CancellationToken cancellationToken = default);
    Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for threat detection service
/// </summary>
public interface IThreatDetectionService
{
    Task<List<SecurityThreat>> DetectThreatsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> IsUserBehaviorSuspiciousAsync(string userId, string action, CancellationToken cancellationToken = default);
    Task ReportThreatAsync(SecurityThreat threat, CancellationToken cancellationToken = default);
    Task<List<SecurityThreat>> GetActiveThreatsAsync(CancellationToken cancellationToken = default);
    Task ResolveThreatAsync(string threatId, string resolution, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for encryption service
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string Hash(string input);
    bool VerifyHash(string input, string hash);
    string GenerateSalt();
    byte[] EncryptBytes(byte[] data);
    byte[] DecryptBytes(byte[] encryptedData);
}

/// <summary>
/// Interface for Azure Key Vault service
/// </summary>
public interface IKeyVaultService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
    Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<List<string>> GetSecretNamesAsync(CancellationToken cancellationToken = default);
    Task<string> GetConnectionStringAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for rate limiting service
/// </summary>
public interface IRateLimitingService
{
    Task<bool> IsRequestAllowedAsync(string identifier, string endpoint, CancellationToken cancellationToken = default);
    Task RecordRequestAsync(string identifier, string endpoint, CancellationToken cancellationToken = default);
    Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, CancellationToken cancellationToken = default);
    Task ResetRateLimitAsync(string identifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for two-factor authentication service
/// </summary>
public interface ITwoFactorService
{
    Task<string> GenerateSecretAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateCodeAsync(string userId, string code, CancellationToken cancellationToken = default);
    Task<string> GenerateQrCodeAsync(string userId, string secret, CancellationToken cancellationToken = default);
    Task<bool> EnableTwoFactorAsync(string userId, string verificationCode, CancellationToken cancellationToken = default);
    Task<bool> DisableTwoFactorAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<string>> GenerateRecoveryCodesAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateRecoveryCodeAsync(string userId, string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for session management service
/// </summary>
public interface ISessionManagementService
{
    Task<string> CreateSessionAsync(string userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task UpdateSessionActivityAsync(string sessionId, CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task InvalidateAllUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limit status model
/// </summary>
public class RateLimitStatus
{
    public bool IsAllowed { get; set; }
    public int RequestsRemaining { get; set; }
    public DateTime ResetTime { get; set; }
    public TimeSpan RetryAfter { get; set; }
}

/// <summary>
/// User session model
/// </summary>
public class UserSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
