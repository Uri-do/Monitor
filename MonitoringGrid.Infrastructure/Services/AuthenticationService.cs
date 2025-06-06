using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Authentication service for user login and token management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly MonitoringContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEncryptionService _encryptionService;
    private readonly ISecurityAuditService _auditService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly SecurityConfiguration _securityConfig;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        MonitoringContext context,
        IJwtTokenService jwtTokenService,
        IEncryptionService encryptionService,
        ISecurityAuditService auditService,
        ITwoFactorService twoFactorService,
        IOptions<SecurityConfiguration> securityConfig,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _twoFactorService = twoFactorService;
        _securityConfig = securityConfig.Value;
        _logger = logger;
    }

    public async Task<LoginResponse> AuthenticateAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Authentication attempt for user {Username} from {IpAddress}", request.Username, ipAddress);

            // Check for account lockout
            if (await IsAccountLockedAsync(request.Username, cancellationToken))
            {
                await _auditService.LogLoginAttemptAsync(request.Username, ipAddress, false, "Account locked", cancellationToken);
                return new LoginResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Account is temporarily locked due to multiple failed login attempts."
                };
            }

            // Get user from database
            var user = await GetUserByUsernameAsync(request.Username, cancellationToken);
            if (user == null)
            {
                await RecordFailedLoginAttemptAsync(request.Username, ipAddress, "User not found", cancellationToken);
                return new LoginResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid username or password."
                };
            }

            // Verify password
            if (!await VerifyPasswordAsync(user.UserId, request.Password, cancellationToken))
            {
                await RecordFailedLoginAttemptAsync(request.Username, ipAddress, "Invalid password", cancellationToken);
                return new LoginResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid username or password."
                };
            }

            // Check if user is active
            if (!user.IsActive)
            {
                await _auditService.LogLoginAttemptAsync(request.Username, ipAddress, false, "User inactive", cancellationToken);
                return new LoginResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Account is disabled. Please contact your administrator."
                };
            }

            // Check if password needs to be changed
            if (await IsPasswordExpiredAsync(user.UserId, cancellationToken))
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    RequiresPasswordChange = true,
                    ErrorMessage = "Password has expired. Please change your password."
                };
            }

            // Check two-factor authentication
            if (await IsTwoFactorEnabledAsync(user.UserId, cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                {
                    return new LoginResponse
                    {
                        IsSuccess = false,
                        RequiresTwoFactor = true,
                        ErrorMessage = "Two-factor authentication code required."
                    };
                }

                if (!await _twoFactorService.ValidateCodeAsync(user.UserId, request.TwoFactorCode, cancellationToken))
                {
                    await RecordFailedLoginAttemptAsync(request.Username, ipAddress, "Invalid 2FA code", cancellationToken);
                    return new LoginResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid two-factor authentication code."
                    };
                }
            }

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token
            await StoreRefreshTokenAsync(user.UserId, refreshToken, cancellationToken);

            // Update last login
            await UpdateLastLoginAsync(user.UserId, ipAddress, cancellationToken);

            // Clear failed login attempts
            await ClearFailedLoginAttemptsAsync(request.Username, cancellationToken);

            // Log successful login
            await _auditService.LogLoginAttemptAsync(request.Username, ipAddress, true, cancellationToken: cancellationToken);

            _logger.LogInformation("User {Username} authenticated successfully from {IpAddress}", request.Username, ipAddress);

            return new LoginResponse
            {
                IsSuccess = true,
                Token = new JwtToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_securityConfig.Jwt.AccessTokenExpirationMinutes),
                    RefreshExpiresAt = DateTime.UtcNow.AddDays(_securityConfig.Jwt.RefreshTokenExpirationDays)
                },
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {Username}: {Message}", request.Username, ex.Message);
            return new LoginResponse
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during authentication. Please try again."
            };
        }
    }

    public async Task<JwtToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Attempting to refresh token");

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                throw new SecurityException("Invalid or expired refresh token");
            }

            // Get user for token generation
            var user = await GetUserByIdAsync(storedToken.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token");
                throw new SecurityException("Invalid refresh token");
            }

            // Generate new tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Invalidate old refresh token
            storedToken.IsActive = false;
            storedToken.RevokedAt = DateTime.UtcNow;

            // Store new refresh token
            await StoreRefreshTokenAsync(storedToken.UserId, newRefreshToken, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", storedToken.UserId);

            return new JwtToken
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_securityConfig.Jwt.AccessTokenExpirationMinutes),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_securityConfig.Jwt.RefreshTokenExpirationDays)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = _jwtTokenService.ValidateToken(token);
            if (principal == null)
                return false;

            // Check if token is blacklisted
            return !await _jwtTokenService.IsTokenBlacklistedAsync(token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<User?> GetUserFromTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = _jwtTokenService.ValidateToken(token);
            if (principal == null)
                return null;

            var userId = principal.FindFirst("user_id")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await GetUserByIdAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user from token: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiration = _jwtTokenService.GetTokenExpiration(token);
            await _jwtTokenService.BlacklistTokenAsync(token, expiration, cancellationToken);
            
            _logger.LogInformation("Token revoked successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Password change attempt for user {UserId}", userId);

            // Verify current password
            if (!await VerifyPasswordAsync(userId, request.CurrentPassword, cancellationToken))
            {
                await _auditService.LogPasswordChangeAsync(userId, false, cancellationToken);
                return false;
            }

            // Validate new password policy
            if (!ValidatePasswordPolicy(request.NewPassword))
            {
                await _auditService.LogPasswordChangeAsync(userId, false, cancellationToken);
                return false;
            }

            // Check password history
            if (await IsPasswordInHistoryAsync(userId, request.NewPassword, cancellationToken))
            {
                await _auditService.LogPasswordChangeAsync(userId, false, cancellationToken);
                return false;
            }

            // Hash and store new password
            var hashedPassword = _encryptionService.Hash(request.NewPassword);
            await UpdatePasswordAsync(userId, hashedPassword, cancellationToken);

            // Add to password history
            await AddPasswordToHistoryAsync(userId, hashedPassword, cancellationToken);

            await _auditService.LogPasswordChangeAsync(userId, true, cancellationToken);
            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password for user {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Password reset requested for email {Email}", email);

            var user = await GetUserByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                // Don't reveal if email exists
                _logger.LogWarning("Password reset requested for non-existent email {Email}", email);
                return true; // Return true to not reveal if email exists
            }

            // Generate reset token
            var resetToken = GeneratePasswordResetToken();
            var expiresAt = DateTime.UtcNow.AddHours(1); // 1 hour expiration

            await StorePasswordResetTokenAsync(user.UserId, resetToken, expiresAt, cancellationToken);

            // In a real implementation, send email with reset link
            // await _emailService.SendPasswordResetEmailAsync(email, resetToken);

            _logger.LogInformation("Password reset token generated for user {UserId}", user.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process password reset for email {Email}: {Message}", email, ex.Message);
            return false;
        }
    }

    public async Task LogoutAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Blacklist the access token
            await RevokeTokenAsync(token, cancellationToken);

            // Invalidate all refresh tokens for the user
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.IsActive = false;
                refreshToken.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged out successfully", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout user {UserId}: {Message}", userId, ex.Message);
            throw;
        }
    }

    // Private helper methods
    private async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    private async Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    private async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    private async Task<bool> VerifyPasswordAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var userPassword = await _context.UserPasswords
            .FirstOrDefaultAsync(up => up.UserId == userId && up.IsActive, cancellationToken);

        if (userPassword == null)
            return false;

        return _encryptionService.VerifyHash(password, userPassword.PasswordHash);
    }

    private async Task<bool> IsAccountLockedAsync(string username, CancellationToken cancellationToken)
    {
        // Temporarily disabled - use User.LockoutEnd instead
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        return user?.LockoutEnd > DateTime.UtcNow;
    }

    private async Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken)
    {
        var userPassword = await _context.UserPasswords
            .FirstOrDefaultAsync(up => up.UserId == userId && up.IsActive, cancellationToken);

        if (userPassword == null)
            return true;

        var expirationDate = userPassword.CreatedAt.AddDays(_securityConfig.PasswordPolicy.PasswordExpirationDays);
        return DateTime.UtcNow > expirationDate;
    }

    private async Task<bool> IsTwoFactorEnabledAsync(string userId, CancellationToken cancellationToken)
    {
        // Use User.TwoFactorEnabled instead
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        return user?.TwoFactorEnabled == true;
    }

    private bool ValidatePasswordPolicy(string password)
    {
        var policy = _securityConfig.PasswordPolicy;

        if (password.Length < policy.MinimumLength || password.Length > policy.MaximumLength)
            return false;

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            return false;

        if (policy.RequireLowercase && !password.Any(char.IsLower))
            return false;

        if (policy.RequireDigit && !password.Any(char.IsDigit))
            return false;

        if (policy.RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c)))
            return false;

        return true;
    }

    private async Task<bool> IsPasswordInHistoryAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var passwordHistory = await _context.UserPasswords
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(_securityConfig.PasswordPolicy.PasswordHistoryCount)
            .ToListAsync(cancellationToken);

        return passwordHistory.Any(ph => _encryptionService.VerifyHash(password, ph.PasswordHash));
    }

    private async Task StoreRefreshTokenAsync(string userId, string refreshToken, CancellationToken cancellationToken)
    {
        var token = new MonitoringGrid.Core.Entities.RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_securityConfig.Jwt.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateLastLoginAsync(string userId, string ipAddress, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RecordFailedLoginAttemptAsync(string username, string ipAddress, string reason, CancellationToken cancellationToken)
    {
        await _auditService.LogLoginAttemptAsync(username, ipAddress, false, reason, cancellationToken);

        // Update failed login attempts on user record
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user != null)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= _securityConfig.PasswordPolicy.MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(_securityConfig.PasswordPolicy.LockoutDurationMinutes);
                _logger.LogWarning("Account locked for user {Username} due to multiple failed login attempts", username);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task LockAccountAsync(string username, CancellationToken cancellationToken)
    {
        // This method is now handled in RecordFailedLoginAttemptAsync
        // Keeping for compatibility but functionality moved to User entity
        _logger.LogWarning("Account locked for user {Username} due to multiple failed login attempts", username);
    }

    private async Task ClearFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken)
    {
        // Clear failed login attempts and lockout from user record
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpdatePasswordAsync(string userId, string hashedPassword, CancellationToken cancellationToken)
    {
        // Deactivate old password
        var oldPasswords = await _context.UserPasswords
            .Where(up => up.UserId == userId && up.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var oldPassword in oldPasswords)
        {
            oldPassword.IsActive = false;
        }

        // Add new password
        var newPassword = new MonitoringGrid.Core.Entities.UserPassword
        {
            UserId = userId,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.UserPasswords.Add(newPassword);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddPasswordToHistoryAsync(string userId, string hashedPassword, CancellationToken cancellationToken)
    {
        var historyEntry = new MonitoringGrid.Core.Entities.UserPassword
        {
            UserId = userId,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow,
            IsActive = false // History entries are not active
        };

        _context.UserPasswords.Add(historyEntry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GeneratePasswordResetToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task StorePasswordResetTokenAsync(string userId, string token, DateTime expiresAt, CancellationToken cancellationToken)
    {
        // Temporarily disabled - would need PasswordResetToken entity in DbContext
        // For now, password reset tokens are not persisted
        _logger.LogInformation("Password reset token generated for user {UserId} (not persisted)", userId);
    }
}

// Supporting entities
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? RevokedAt { get; set; }
    public AuthUser User { get; set; } = null!;
}

public class UserPassword
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class PasswordHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AccountLockout
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
    public DateTime LockedUntil { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PasswordResetToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }
}

public class UserTwoFactorSettings
{
    public string UserId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Secret { get; set; }
    public List<string> RecoveryCodes { get; set; } = new();
    public DateTime? EnabledAt { get; set; }
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}
