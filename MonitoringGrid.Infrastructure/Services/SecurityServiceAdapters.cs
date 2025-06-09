using System.Security.Claims;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Adapter for IAuthenticationService to maintain backward compatibility
/// </summary>
public class AuthenticationServiceAdapter : IAuthenticationService
{
    private readonly ISecurityService _securityService;

    public AuthenticationServiceAdapter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public Task<LoginResponse> AuthenticateAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default)
        => _securityService.AuthenticateAsync(request, ipAddress, cancellationToken);

    public Task<JwtToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => _securityService.RefreshTokenAsync(refreshToken, cancellationToken);

    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        => _securityService.ValidateTokenAsync(token, cancellationToken);

    public Task<User?> GetUserFromTokenAsync(string token, CancellationToken cancellationToken = default)
        => _securityService.GetUserFromTokenAsync(token, cancellationToken);

    public Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
        => _securityService.RevokeTokenAsync(token, cancellationToken);

    public Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        => _securityService.ChangePasswordAsync(userId, request, cancellationToken);

    public Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
        => _securityService.HasPermissionAsync(userId, permission, cancellationToken);

    public Task<bool> HasRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        => _securityService.HasRoleAsync(userId, role, cancellationToken);

    public Task<List<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
        => _securityService.GetUserPermissionsAsync(userId, cancellationToken);

    public Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
        => _securityService.GetUserRolesAsync(userId, cancellationToken);

    public Task<bool> AddRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
        => _securityService.AddRoleAsync(userId, roleId, cancellationToken);

    public Task<bool> RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
        => _securityService.RemoveRoleAsync(userId, roleId, cancellationToken);

    public Task<bool> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
        => _securityService.ResetPasswordAsync(email, cancellationToken);

    public Task LogoutAsync(string userId, string token, CancellationToken cancellationToken = default)
        => _securityService.LogoutAsync(userId, token, cancellationToken);
}

/// <summary>
/// Adapter for IJwtTokenService to maintain backward compatibility
/// </summary>
public class JwtTokenServiceAdapter : IJwtTokenService
{
    private readonly ISecurityService _securityService;

    public JwtTokenServiceAdapter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public string GenerateAccessToken(User user, List<Claim>? additionalClaims = null)
        => _securityService.GenerateAccessToken(user, additionalClaims);

    public string GenerateRefreshToken()
        => _securityService.GenerateRefreshToken();

    public ClaimsPrincipal? ValidateToken(string token)
        => _securityService.ValidateTokenStructure(token);

    public DateTime GetTokenExpiration(string token)
        => _securityService.GetTokenExpiration(token);

    public Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
        => _securityService.IsTokenBlacklistedAsync(token, cancellationToken);

    public Task BlacklistTokenAsync(string token, DateTime expiration, CancellationToken cancellationToken = default)
        => _securityService.BlacklistTokenAsync(token, expiration, cancellationToken);
}

/// <summary>
/// Adapter for IEncryptionService to maintain backward compatibility
/// </summary>
public class EncryptionServiceAdapter : IEncryptionService
{
    private readonly ISecurityService _securityService;

    public EncryptionServiceAdapter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public string Encrypt(string plainText)
        => _securityService.Encrypt(plainText);

    public string Decrypt(string cipherText)
        => _securityService.Decrypt(cipherText);

    public string Hash(string input)
        => _securityService.Hash(input);

    public bool VerifyHash(string input, string hash)
        => _securityService.VerifyHash(input, hash);

    public string GenerateSalt()
        => _securityService.GenerateSalt();

    public byte[] EncryptBytes(byte[] data)
        => _securityService.EncryptBytes(data);

    public byte[] DecryptBytes(byte[] encryptedData)
        => _securityService.DecryptBytes(encryptedData);

    public string HashPassword(string password, string? salt = null)
        => _securityService.HashPassword(password, salt);

    public bool VerifyPassword(string password, string hash)
        => _securityService.VerifyPassword(password, hash);
}

/// <summary>
/// Adapter for ISecurityAuditService to maintain backward compatibility
/// </summary>
public class SecurityAuditServiceAdapter : ISecurityAuditService
{
    private readonly ISecurityService _securityService;

    public SecurityAuditServiceAdapter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public Task LogSecurityEventAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        => _securityService.LogSecurityEventAsync(auditEvent, cancellationToken);

    public Task LogLoginAttemptAsync(string username, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default)
        => _securityService.LogLoginAttemptAsync(username, ipAddress, success, reason, cancellationToken);

    public Task LogPasswordChangeAsync(string userId, bool success, CancellationToken cancellationToken = default)
        => _securityService.LogPasswordChangeAsync(userId, success, cancellationToken);

    public Task LogSuspiciousActivityAsync(string? userId, string activityType, string description, string ipAddress, CancellationToken cancellationToken = default)
        => _securityService.LogSuspiciousActivityAsync(userId, activityType, description, ipAddress, cancellationToken);

    public Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default)
        => _securityService.GetSecurityEventsAsync(filter, cancellationToken);

    public Task LogPermissionChangeAsync(string userId, string action, string resource, CancellationToken cancellationToken = default)
        => _securityService.LogPermissionChangeAsync(userId, action, resource, cancellationToken);

    public Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, CancellationToken cancellationToken = default)
        => _securityService.GetSecurityEventsAsync(startDate, endDate, userId, cancellationToken);
}

/// <summary>
/// Adapter for IThreatDetectionService to maintain backward compatibility
/// </summary>
public class ThreatDetectionServiceAdapter : IThreatDetectionService
{
    private readonly ISecurityService _securityService;

    public ThreatDetectionServiceAdapter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public Task<List<SecurityThreat>> DetectThreatsAsync(CancellationToken cancellationToken = default)
        => _securityService.DetectThreatsAsync(cancellationToken);

    public Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default)
        => _securityService.IsIpAddressSuspiciousAsync(ipAddress, cancellationToken);

    public Task<bool> IsUserBehaviorSuspiciousAsync(string userId, string action, CancellationToken cancellationToken = default)
        => _securityService.IsUserBehaviorSuspiciousAsync(userId, action, cancellationToken);

    public Task ReportThreatAsync(SecurityThreat threat, CancellationToken cancellationToken = default)
        => _securityService.ReportThreatAsync(threat, cancellationToken);

    public Task<List<SecurityThreat>> GetActiveThreatsAsync(CancellationToken cancellationToken = default)
        => _securityService.GetActiveThreatsAsync(cancellationToken);

    public Task ResolveThreatAsync(string threatId, string resolution, CancellationToken cancellationToken = default)
        => _securityService.ResolveThreatAsync(threatId, resolution, cancellationToken);
}

/// <summary>
/// Adapter for ITwoFactorService to maintain backward compatibility
/// </summary>
public class TwoFactorServiceAdapter : ITwoFactorService
{
    private readonly ISecurityService _securityService;

    public TwoFactorServiceAdapter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public Task<string> GenerateSecretAsync(string userId, CancellationToken cancellationToken = default)
        => _securityService.GenerateSecretAsync(userId, cancellationToken);

    public Task<bool> ValidateCodeAsync(string userId, string code, CancellationToken cancellationToken = default)
        => _securityService.ValidateTwoFactorCodeAsync(userId, code, cancellationToken);

    public Task<string> GenerateQrCodeAsync(string userId, string secret, CancellationToken cancellationToken = default)
        => _securityService.GenerateQrCodeAsync(userId, secret, cancellationToken);

    public Task<bool> EnableTwoFactorAsync(string userId, string verificationCode, CancellationToken cancellationToken = default)
        => _securityService.EnableTwoFactorAsync(userId, verificationCode, cancellationToken);

    public Task<bool> DisableTwoFactorAsync(string userId, CancellationToken cancellationToken = default)
        => _securityService.DisableTwoFactorAsync(userId, cancellationToken);

    public Task<List<string>> GenerateRecoveryCodesAsync(string userId, CancellationToken cancellationToken = default)
        => _securityService.GenerateRecoveryCodesAsync(userId, cancellationToken);

    public Task<bool> ValidateRecoveryCodeAsync(string userId, string recoveryCode, CancellationToken cancellationToken = default)
        => _securityService.ValidateRecoveryCodeAsync(userId, recoveryCode, cancellationToken);
}
