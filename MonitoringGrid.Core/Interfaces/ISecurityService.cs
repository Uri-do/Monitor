using System.Security.Claims;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Unified security service interface consolidating all security-related operations
/// Replaces: IAuthenticationService, IJwtTokenService, IEncryptionService, 
/// ISecurityAuditService, IThreatDetectionService, ITwoFactorService
/// </summary>
public interface ISecurityService
{
    #region Authentication Domain
    
    /// <summary>
    /// Authenticates a user with login credentials
    /// </summary>
    Task<Result<LoginResponse>> AuthenticateAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    Task<Result<JwtToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    Task<Result<bool>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information from a JWT token
    /// </summary>
    Task<Result<User>> GetUserFromTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a JWT token
    /// </summary>
    Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password
    /// </summary>
    Task<Result> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user
    /// </summary>
    Task<Result> LogoutAsync(string userId, string token, CancellationToken cancellationToken = default);

    #endregion

    #region Authorization Domain
    
    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user has a specific role
    /// </summary>
    Task<bool> HasRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all permissions for a user
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all roles for a user
    /// </summary>
    Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a role to a user
    /// </summary>
    Task<bool> AddRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a role from a user
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);

    #endregion

    #region Token Management Domain
    
    /// <summary>
    /// Generates an access token for a user
    /// </summary>
    string GenerateAccessToken(User user, List<Claim>? additionalClaims = null);
    
    /// <summary>
    /// Generates a refresh token
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validates token structure without database lookup
    /// </summary>
    ClaimsPrincipal? ValidateTokenStructure(string token);
    
    /// <summary>
    /// Gets token expiration date
    /// </summary>
    DateTime GetTokenExpiration(string token);
    
    /// <summary>
    /// Checks if a token is blacklisted
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Blacklists a token
    /// </summary>
    Task BlacklistTokenAsync(string token, DateTime expiration, CancellationToken cancellationToken = default);

    #endregion

    #region Encryption Domain
    
    /// <summary>
    /// Encrypts a string
    /// </summary>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypts a string
    /// </summary>
    string Decrypt(string cipherText);
    
    /// <summary>
    /// Hashes a string
    /// </summary>
    string Hash(string input);
    
    /// <summary>
    /// Verifies a hash
    /// </summary>
    bool VerifyHash(string input, string hash);
    
    /// <summary>
    /// Generates a salt
    /// </summary>
    string GenerateSalt();
    
    /// <summary>
    /// Encrypts byte array
    /// </summary>
    byte[] EncryptBytes(byte[] data);
    
    /// <summary>
    /// Decrypts byte array
    /// </summary>
    byte[] DecryptBytes(byte[] encryptedData);
    
    /// <summary>
    /// Hashes a password with salt
    /// </summary>
    string HashPassword(string password, string? salt = null);
    
    /// <summary>
    /// Verifies a password against hash
    /// </summary>
    bool VerifyPassword(string password, string hash);

    #endregion

    #region Security Audit Domain
    
    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logs a login attempt
    /// </summary>
    Task LogLoginAttemptAsync(string username, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logs a password change
    /// </summary>
    Task LogPasswordChangeAsync(string userId, bool success, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logs suspicious activity
    /// </summary>
    Task LogSuspiciousActivityAsync(string? userId, string activityType, string description, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs permission changes
    /// </summary>
    Task LogPermissionChangeAsync(string userId, string action, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security events with filtering
    /// </summary>
    Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security events with simple filtering (backward compatibility)
    /// </summary>
    Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Threat Detection Domain
    
    /// <summary>
    /// Detects security threats
    /// </summary>
    Task<List<SecurityThreat>> DetectThreatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an IP address is suspicious
    /// </summary>
    Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if user behavior is suspicious
    /// </summary>
    Task<bool> IsUserBehaviorSuspiciousAsync(string userId, string action, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reports a security threat
    /// </summary>
    Task ReportThreatAsync(SecurityThreat threat, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active security threats
    /// </summary>
    Task<List<SecurityThreat>> GetActiveThreatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resolves a security threat
    /// </summary>
    Task ResolveThreatAsync(string threatId, string resolution, CancellationToken cancellationToken = default);

    #endregion

    #region Two-Factor Authentication Domain
    
    /// <summary>
    /// Generates a 2FA secret for a user
    /// </summary>
    Task<string> GenerateSecretAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a 2FA code
    /// </summary>
    Task<bool> ValidateTwoFactorCodeAsync(string userId, string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates QR code URI for 2FA setup
    /// </summary>
    Task<string> GenerateQrCodeAsync(string userId, string secret, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables 2FA for a user
    /// </summary>
    Task<bool> EnableTwoFactorAsync(string userId, string verificationCode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disables 2FA for a user
    /// </summary>
    Task<bool> DisableTwoFactorAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates recovery codes for 2FA
    /// </summary>
    Task<List<string>> GenerateRecoveryCodesAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a recovery code
    /// </summary>
    Task<bool> ValidateRecoveryCodeAsync(string userId, string recoveryCode, CancellationToken cancellationToken = default);

    #endregion

    #region Configuration and Validation
    
    /// <summary>
    /// Validates security configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets security health status
    /// </summary>
    Task<SecurityHealthStatus> GetSecurityHealthAsync(CancellationToken cancellationToken = default);

    #endregion
}
