using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Unified security service consolidating all security-related operations
/// Replaces: AuthenticationService, JwtTokenService, EncryptionService, 
/// SecurityAuditService, ThreatDetectionService, TwoFactorService
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly MonitoringContext _context;
    private readonly SecurityConfiguration _securityConfig;
    private readonly JwtSettings _jwtSettings;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly ILogger<SecurityService> _logger;
    
    // Encryption fields
    private readonly byte[] _encryptionKey;
    private readonly TokenValidationParameters _tokenValidationParameters;
    
    // 2FA constants
    private const int CodeLength = 6;
    private const int TimeStepSeconds = 30;

    public SecurityService(
        MonitoringContext context,
        IOptions<SecurityConfiguration> securityConfig,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EncryptionSettings> encryptionSettings,
        ILogger<SecurityService> logger)
    {
        _context = context;
        _securityConfig = securityConfig.Value;
        _jwtSettings = jwtSettings.Value;
        _encryptionSettings = encryptionSettings.Value;
        _logger = logger;

        // Initialize encryption key (using a default key if not configured)
        _encryptionKey = !string.IsNullOrEmpty(_encryptionSettings.EncryptionKey)
            ? Convert.FromBase64String(_encryptionSettings.EncryptionKey)
            : Encoding.UTF8.GetBytes("DefaultEncryptionKey123456789012"); // 32 bytes

        // Initialize token validation parameters
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    #region Authentication Domain

    public async Task<LoginResponse> AuthenticateAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Authenticating user {Username} from IP {IpAddress}", request.Username, ipAddress);

            // Check for suspicious IP
            if (await IsIpAddressSuspiciousAsync(ipAddress, cancellationToken))
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, false, "Suspicious IP address", cancellationToken);
                return new LoginResponse { IsSuccess = false, ErrorMessage = "Authentication failed" };
            }

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

            if (user == null)
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, false, "User not found", cancellationToken);
                return new LoginResponse { IsSuccess = false, ErrorMessage = "Invalid credentials" };
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, false, "Invalid password", cancellationToken);
                return new LoginResponse { IsSuccess = false, ErrorMessage = "Invalid credentials" };
            }

            // Check if 2FA is enabled
            var twoFactorSettings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == user.UserId && tfs.IsEnabled, cancellationToken);

            if (twoFactorSettings != null && string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    RequiresTwoFactor = true,
                    ErrorMessage = "Two-factor authentication required"
                };
            }

            // Validate 2FA if provided
            if (twoFactorSettings != null && !string.IsNullOrEmpty(request.TwoFactorCode))
            {
                if (!await ValidateTwoFactorCodeAsync(user.UserId, request.TwoFactorCode, cancellationToken))
                {
                    await LogLoginAttemptAsync(request.Username, ipAddress, false, "Invalid 2FA code", cancellationToken);
                    return new LoginResponse { IsSuccess = false, ErrorMessage = "Invalid two-factor code" };
                }
            }

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token
            var tokenEntity = new Core.Entities.RefreshToken
            {
                Token = refreshToken,
                UserId = user.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            _context.RefreshTokens.Add(tokenEntity);
            await _context.SaveChangesAsync(cancellationToken);

            await LogLoginAttemptAsync(request.Username, ipAddress, true, "Successful login", cancellationToken);

            return new LoginResponse
            {
                IsSuccess = true,
                Token = new JwtToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    RefreshExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
                },
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", request.Username);
            return new LoginResponse { IsSuccess = false, ErrorMessage = "Authentication failed" };
        }
    }

    public async Task<JwtToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (tokenEntity?.User == null)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            // Generate new access token
            var accessToken = GenerateAccessToken(tokenEntity.User);

            return new JwtToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if token is blacklisted
            if (await IsTokenBlacklistedAsync(token, cancellationToken))
                return false;

            // Validate token structure
            var principal = ValidateTokenStructure(token);
            return principal != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> GetUserFromTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = ValidateTokenStructure(token);
            if (principal == null) return null;

            var userId = principal.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from token");
            return null;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiration = GetTokenExpiration(token);
            await BlacklistTokenAsync(token, expiration, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                await LogPasswordChangeAsync(userId, false, cancellationToken);
                return false;
            }

            // Verify current password
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                await LogPasswordChangeAsync(userId, false, cancellationToken);
                return false;
            }

            // Hash new password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await LogPasswordChangeAsync(userId, true, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            await LogPasswordChangeAsync(userId, false, cancellationToken);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

            if (user == null)
            {
                // Don't reveal if email exists for security
                return true;
            }

            // Generate temporary password
            var tempPassword = GenerateTemporaryPassword();
            user.PasswordHash = HashPassword(tempPassword);
            user.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // TODO: Send email with temporary password
            _logger.LogInformation("Password reset for user {Email}", email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", email);
            return false;
        }
    }

    public async Task LogoutAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Blacklist the token
            var expiration = GetTokenExpiration(token);
            await BlacklistTokenAsync(token, expiration, cancellationToken);

            // Log the logout event
            await LogSecurityEventAsync(new SecurityAuditEvent
            {
                EventType = "LOGOUT",
                Action = "Logout",
                Resource = "Authentication",
                UserId = userId,
                IsSuccess = true
            }, cancellationToken);

            _logger.LogInformation("User {UserId} logged out successfully", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Authorization Domain

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
                .Join(_context.Permissions, rp => rp.PermissionId, p => p.PermissionId, (rp, p) => p)
                .AnyAsync(p => p.Name == permission, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> HasRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.RoleId, (ur, r) => r)
                .AnyAsync(r => r.Name == role, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role {Role} for user {UserId}", role, userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
                .Join(_context.Permissions, rp => rp.PermissionId, p => p.PermissionId, (rp, p) => p.Name)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.RoleId, (ur, r) => r.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> AddRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (existingRole) return true;

            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role {RoleId} to user {UserId}", roleId, userId);
            return false;
        }
    }

    public async Task<bool> RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (userRole == null) return true;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            return false;
        }
    }

    #endregion

    #region Token Management Domain

    public string GenerateAccessToken(User user, List<Claim>? additionalClaims = null)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.DisplayName),
                new("user_id", user.UserId),
                new("username", user.Username),
                new("email", user.Email),
                new("display_name", user.DisplayName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (additionalClaims != null)
                claims.AddRange(additionalClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogDebug("Generated access token for user {UserId}", user.UserId);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate access token for user {UserId}", user.UserId);
            throw;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateTokenStructure(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            // Verify it's a JWT token
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Invalid token algorithm or format");
                return null;
            }

            _logger.LogDebug("Token validated successfully");
            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogDebug("Token has expired");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    public DateTime GetTokenExpiration(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token expiration");
            return DateTime.UtcNow;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHash = ComputeTokenHash(token);
            return await _context.Set<BlacklistedToken>()
                .AnyAsync(bt => bt.TokenHash == tokenHash && bt.ExpiresAt > DateTime.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is blacklisted");
            return false;
        }
    }

    public async Task BlacklistTokenAsync(string token, DateTime expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHash = ComputeTokenHash(token);
            var blacklistedToken = new BlacklistedToken
            {
                TokenHash = tokenHash,
                BlacklistedAt = DateTime.UtcNow,
                ExpiresAt = expiration
            };

            _context.Set<BlacklistedToken>().Add(blacklistedToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token");
            throw;
        }
    }

    #endregion

    #region Encryption Domain

    public string Encrypt(string plainText)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = EncryptBytes(plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt string");
            throw new CryptographicException("String encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        try
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var encryptedBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = DecryptBytes(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt string");
            throw new CryptographicException("String decryption failed", ex);
        }
    }

    public string Hash(string input)
    {
        try
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash string");
            throw new CryptographicException("String hashing failed", ex);
        }
    }

    public bool VerifyHash(string input, string hash)
    {
        try
        {
            var inputHash = Hash(input);
            return string.Equals(inputHash, hash, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify hash");
            return false;
        }
    }

    public string GenerateSalt()
    {
        try
        {
            var saltBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate salt");
            throw new CryptographicException("Salt generation failed", ex);
        }
    }

    public byte[] EncryptBytes(byte[] data)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();

            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(data, 0, data.Length);
                csEncrypt.FlushFinalBlock();
            }

            return msEncrypt.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt bytes");
            throw new CryptographicException("Byte encryption failed", ex);
        }
    }

    public byte[] DecryptBytes(byte[] encryptedData)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData, iv.Length, encryptedData.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msResult = new MemoryStream();

            csDecrypt.CopyTo(msResult);
            return msResult.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt bytes");
            throw new CryptographicException("Byte decryption failed", ex);
        }
    }

    public string HashPassword(string password, string? salt = null)
    {
        try
        {
            salt ??= GenerateSalt();

            // Use PBKDF2 with high iteration count for password hashing
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 600000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return $"{salt}:{Convert.ToBase64String(hash)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password");
            throw new CryptographicException("Password hashing failed", ex);
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var parts = hash.Split(':');
            if (parts.Length != 2) return false;

            var salt = parts[0];
            var expectedHash = HashPassword(password, salt);

            return string.Equals(expectedHash, hash, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }

    #endregion

    #region Security Audit Domain

    public async Task LogSecurityEventAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            auditEvent.EventId = Guid.NewGuid().ToString();
            auditEvent.Timestamp = DateTime.UtcNow;

            _context.Set<SecurityAuditEvent>().Add(auditEvent);
            await _context.SaveChangesAsync(cancellationToken);

            // Log to application logger
            var logLevel = auditEvent.IsSuccess ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(logLevel, "Security Event: {EventType} - {Action} on {Resource} by {Username} from {IpAddress}. Success: {IsSuccess}",
                auditEvent.EventType, auditEvent.Action, auditEvent.Resource, auditEvent.Username, auditEvent.IpAddress, auditEvent.IsSuccess);

            // Check for suspicious activity
            if (!auditEvent.IsSuccess || IsSuspiciousEvent(auditEvent))
            {
                await IsUserBehaviorSuspiciousAsync(auditEvent.UserId ?? "unknown", auditEvent.Action ?? "unknown", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", auditEvent.EventType);
        }
    }

    public async Task LogLoginAttemptAsync(string username, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventType = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
            Action = "Login",
            Resource = "Authentication",
            Username = username,
            IpAddress = ipAddress,
            IsSuccess = success,
            ErrorMessage = success ? null : (reason ?? "Failed login attempt"),
            UserAgent = null // Could be passed as parameter if needed
        };

        await LogSecurityEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogPasswordChangeAsync(string userId, bool success, CancellationToken cancellationToken = default)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventType = success ? "PASSWORD_CHANGE_SUCCESS" : "PASSWORD_CHANGE_FAILED",
            Action = "ChangePassword",
            Resource = "UserAccount",
            UserId = userId,
            IsSuccess = success,
            ErrorMessage = success ? null : "Password change failed"
        };

        await LogSecurityEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogSuspiciousActivityAsync(string? userId, string activityType, string description, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "SUSPICIOUS_ACTIVITY",
                Action = activityType,
                Resource = "Security",
                UserId = userId,
                IpAddress = ipAddress,
                IsSuccess = false,
                ErrorMessage = description,
                Severity = "High"
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);

            // Create security threat
            var threat = new SecurityThreat
            {
                ThreatId = Guid.NewGuid().ToString(),
                ThreatType = activityType,
                Severity = "High",
                Description = description,
                UserId = userId,
                IpAddress = ipAddress,
                DetectedAt = DateTime.UtcNow,
                IsResolved = false,
                ThreatData = new Dictionary<string, object>
                {
                    ["SourceEvent"] = auditEvent.EventId,
                    ["AutoDetected"] = true
                }
            };

            await ReportThreatAsync(threat, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log suspicious activity: {ActivityType}", activityType);
        }
    }

    public async Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<SecurityAuditEvent>().AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(e => e.Timestamp >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.Timestamp <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.EventType))
                query = query.Where(e => e.EventType == filter.EventType);

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(e => e.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.IpAddress))
                query = query.Where(e => e.IpAddress == filter.IpAddress);

            if (filter.IsSuccess.HasValue)
                query = query.Where(e => e.IsSuccess == filter.IsSuccess.Value);

            return await query
                .OrderByDescending(e => e.Timestamp)
                .Take(filter.MaxResults ?? 1000)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return new List<SecurityAuditEvent>();
        }
    }

    public async Task LogPermissionChangeAsync(string userId, string action, string resource, CancellationToken cancellationToken = default)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "PERMISSION_CHANGE",
            Action = action,
            Resource = resource,
            UserId = userId,
            IsSuccess = true,
            Severity = "Medium"
        };

        await LogSecurityEventAsync(auditEvent, cancellationToken);
    }

    public async Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        var filter = new SecurityEventFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            UserId = userId
        };

        return await GetSecurityEventsAsync(filter, cancellationToken);
    }

    private bool IsSuspiciousEvent(SecurityAuditEvent auditEvent)
    {
        // Define suspicious patterns
        var suspiciousPatterns = new[]
        {
            "MULTIPLE_FAILED_LOGINS",
            "PRIVILEGE_ESCALATION",
            "UNAUTHORIZED_ACCESS",
            "DATA_EXPORT",
            "ADMIN_ACTION"
        };

        return suspiciousPatterns.Contains(auditEvent.EventType) ||
               auditEvent.EventType.Contains("FAILED") ||
               auditEvent.Severity == "High";
    }

    #endregion

    #region Threat Detection Domain

    public async Task<List<SecurityThreat>> DetectThreatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting threat detection scan");

            var threats = new List<SecurityThreat>();

            // Detect brute force attacks
            var bruteForceThreats = await DetectBruteForceAttacksAsync(cancellationToken);
            threats.AddRange(bruteForceThreats);

            // Detect suspicious IP addresses
            var suspiciousIpThreats = await DetectSuspiciousIpAddressesAsync(cancellationToken);
            threats.AddRange(suspiciousIpThreats);

            // Detect unusual user behavior
            var behaviorThreats = await DetectUnusualUserBehaviorAsync(cancellationToken);
            threats.AddRange(behaviorThreats);

            _logger.LogInformation("Threat detection completed. Found {ThreatCount} threats", threats.Count);

            return threats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during threat detection");
            throw;
        }
    }

    public async Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if IP is in known threat list
            var knownThreat = await _context.Set<SecurityThreat>()
                .AnyAsync(st => st.IpAddress == ipAddress &&
                               st.ThreatType == "SUSPICIOUS_IP" &&
                               !st.IsResolved, cancellationToken);

            if (knownThreat)
                return true;

            // Check for multiple failed login attempts from this IP
            var recentFailures = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.IpAddress == ipAddress &&
                             sae.EventType == "LOGIN_FAILED" &&
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            return recentFailures >= 10; // 10+ failed attempts in 1 hour
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if IP address {IpAddress} is suspicious", ipAddress);
            return false;
        }
    }

    public async Task<bool> IsUserBehaviorSuspiciousAsync(string userId, string action, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for unusual activity patterns
            var recentActions = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.UserId == userId &&
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            // Threshold for suspicious activity (more than 100 actions per hour)
            if (recentActions > 100)
            {
                await ReportSuspiciousUserBehaviorAsync(userId, action, $"Excessive activity: {recentActions} actions in 1 hour", cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user behavior for {UserId}", userId);
            return false;
        }
    }

    public async Task ReportThreatAsync(SecurityThreat threat, CancellationToken cancellationToken = default)
    {
        try
        {
            threat.ThreatId ??= Guid.NewGuid().ToString();
            threat.DetectedAt = DateTime.UtcNow;

            _context.Set<SecurityThreat>().Add(threat);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Security threat reported: {ThreatType} - {Description}", threat.ThreatType, threat.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting threat: {ThreatType}", threat.ThreatType);
            throw;
        }
    }

    public async Task<List<SecurityThreat>> GetActiveThreatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Set<SecurityThreat>()
                .Where(st => !st.IsResolved)
                .OrderByDescending(st => st.DetectedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active threats");
            return new List<SecurityThreat>();
        }
    }

    public async Task ResolveThreatAsync(string threatId, string resolution, CancellationToken cancellationToken = default)
    {
        try
        {
            var threat = await _context.Set<SecurityThreat>()
                .FirstOrDefaultAsync(st => st.ThreatId == threatId, cancellationToken);

            if (threat != null)
            {
                threat.IsResolved = true;
                threat.ResolvedAt = DateTime.UtcNow;
                threat.Resolution = resolution;

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Threat {ThreatId} resolved: {Resolution}", threatId, resolution);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving threat {ThreatId}", threatId);
            throw;
        }
    }

    #endregion

    #region Two-Factor Authentication Domain

    public async Task<string> GenerateSecretAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating 2FA secret for user {UserId}", userId);

            // Generate a random 32-byte secret
            var secretBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secretBytes);
            }

            var secret = Convert.ToBase64String(secretBytes);

            // Store or update the secret for the user
            var existingSettings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (existingSettings != null)
            {
                existingSettings.Secret = secret;
                existingSettings.IsEnabled = false; // Reset enabled status until verified
            }
            else
            {
                var newSettings = new UserTwoFactorSettings
                {
                    UserId = userId,
                    Secret = secret,
                    IsEnabled = false
                };
                _context.Set<UserTwoFactorSettings>().Add(newSettings);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA secret generated for user {UserId}", userId);
            return secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating 2FA secret for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateTwoFactorCodeAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating 2FA code for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId && tfs.IsEnabled, cancellationToken);

            if (settings?.Secret == null)
            {
                _logger.LogWarning("2FA not enabled or secret not found for user {UserId}", userId);
                return false;
            }

            // Validate TOTP code
            var isValid = ValidateTotpCode(settings.Secret, code);

            if (isValid)
            {
                _logger.LogInformation("2FA code validated successfully for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid 2FA code for user {UserId}", userId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating 2FA code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string> GenerateQrCodeAsync(string userId, string secret, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
                throw new ArgumentException($"User {userId} not found");

            // Create TOTP URI
            var issuer = "MonitoringGrid";
            var accountName = $"{user.Email}";
            var totpUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            _logger.LogInformation("QR code URI generated for user {UserId}", userId);

            // Return the URI - the frontend can generate the actual QR code
            return totpUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> EnableTwoFactorAsync(string userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Enabling 2FA for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (settings?.Secret == null)
            {
                _logger.LogWarning("2FA secret not found for user {UserId}", userId);
                return false;
            }

            // Validate the verification code
            if (!ValidateTotpCode(settings.Secret, verificationCode))
            {
                _logger.LogWarning("Invalid verification code for enabling 2FA for user {UserId}", userId);
                return false;
            }

            // Enable 2FA
            settings.IsEnabled = true;
            settings.EnabledAt = DateTime.UtcNow;

            // Generate recovery codes
            settings.RecoveryCodes = GenerateRecoveryCodes();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA enabled successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Disabling 2FA for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (settings != null)
            {
                settings.IsEnabled = false;
                settings.EnabledAt = null;
                settings.Secret = null; // Clear the secret
                settings.RecoveryCodes?.Clear(); // Clear recovery codes

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("2FA disabled for user {UserId}", userId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<string>> GenerateRecoveryCodesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating recovery codes for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (settings == null)
            {
                throw new ArgumentException($"2FA settings not found for user {userId}");
            }

            // Generate new recovery codes
            var recoveryCodes = GenerateRecoveryCodes();
            settings.RecoveryCodes = recoveryCodes;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recovery codes generated for user {UserId}", userId);
            return recoveryCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recovery codes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateRecoveryCodeAsync(string userId, string recoveryCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId && tfs.IsEnabled, cancellationToken);

            if (settings?.RecoveryCodes == null)
                return false;

            // Check if the recovery code exists and remove it (one-time use)
            if (settings.RecoveryCodes.Contains(recoveryCode))
            {
                settings.RecoveryCodes.Remove(recoveryCode);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Recovery code used for user {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating recovery code for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Configuration and Validation

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JWT settings
            if (string.IsNullOrEmpty(_jwtSettings.SecretKey) || _jwtSettings.SecretKey.Length < 32)
                return false;

            // Validate encryption settings
            if (string.IsNullOrEmpty(_encryptionSettings.EncryptionKey))
                return false;

            // Test database connectivity
            await _context.Database.CanConnectAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security configuration validation failed");
            return false;
        }
    }

    public async Task<SecurityHealthStatus> GetSecurityHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = new SecurityHealthStatus
            {
                IsHealthy = true,
                CheckTime = DateTime.UtcNow,
                Issues = new List<string>()
            };

            // Check active threats
            var activeThreats = await GetActiveThreatsAsync(cancellationToken);
            status.ActiveThreatsCount = activeThreats.Count;

            if (activeThreats.Count > 10)
            {
                status.IsHealthy = false;
                status.Issues.Add($"High number of active threats: {activeThreats.Count}");
            }

            // Check recent failed logins
            var recentFailures = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.EventType == "LOGIN_FAILED" &&
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            if (recentFailures > 100)
            {
                status.IsHealthy = false;
                status.Issues.Add($"High number of failed logins: {recentFailures} in last hour");
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security health status");
            return new SecurityHealthStatus
            {
                IsHealthy = false,
                CheckTime = DateTime.UtcNow,
                Issues = new List<string> { "Health check failed" }
            };
        }
    }

    #endregion

    #region Private Helper Methods

    private List<string> GenerateRecoveryCodes()
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = BitConverter.ToUInt32(bytes, 0).ToString("D8");
            codes.Add(code);
        }

        return codes;
    }

    private bool ValidateTotpCode(string secret, string code)
    {
        try
        {
            var secretBytes = Convert.FromBase64String(secret);
            var currentTimeStep = GetCurrentTimeStep();

            // Check current time step and adjacent ones (to account for clock drift)
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedCode = GenerateTotpCode(secretBytes, timeStep);

                if (expectedCode == code)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TOTP code");
            return false;
        }
    }

    private long GetCurrentTimeStep()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTime / TimeStepSeconds;
    }

    private string GenerateTotpCode(byte[] secret, long timeStep)
    {
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timeBytes);

        var offset = hash[hash.Length - 1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24) |
                   ((hash[offset + 1] & 0xFF) << 16) |
                   ((hash[offset + 2] & 0xFF) << 8) |
                   (hash[offset + 3] & 0xFF);

        return (code % (int)Math.Pow(10, CodeLength)).ToString($"D{CodeLength}");
    }

    private async Task<List<SecurityThreat>> DetectBruteForceAttacksAsync(CancellationToken cancellationToken)
    {
        var threats = new List<SecurityThreat>();

        try
        {
            // Group failed login attempts by IP address in the last hour
            var suspiciousIps = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.EventType == "LOGIN_FAILED" &&
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .GroupBy(sae => sae.IpAddress)
                .Where(g => g.Count() >= 20) // 20+ failed attempts in 1 hour
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var suspiciousIp in suspiciousIps)
            {
                if (string.IsNullOrEmpty(suspiciousIp.IpAddress)) continue;

                var threat = new SecurityThreat
                {
                    ThreatId = Guid.NewGuid().ToString(),
                    ThreatType = "BRUTE_FORCE_ATTACK",
                    Severity = "High",
                    Description = $"Brute force attack detected from IP {suspiciousIp.IpAddress} with {suspiciousIp.Count} failed login attempts",
                    IpAddress = suspiciousIp.IpAddress,
                    DetectedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ThreatData = new Dictionary<string, object>
                    {
                        ["FailedAttempts"] = suspiciousIp.Count,
                        ["TimeWindow"] = "1 hour"
                    }
                };

                threats.Add(threat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting brute force attacks");
        }

        return threats;
    }

    private async Task<List<SecurityThreat>> DetectSuspiciousIpAddressesAsync(CancellationToken cancellationToken)
    {
        var threats = new List<SecurityThreat>();

        try
        {
            // Detect IPs with multiple different usernames
            var suspiciousIps = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.EventType == "LOGIN_FAILED" &&
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-24))
                .GroupBy(sae => sae.IpAddress)
                .Where(g => g.Select(x => x.Username).Distinct().Count() >= 10) // 10+ different usernames
                .Select(g => new { IpAddress = g.Key, UserCount = g.Select(x => x.Username).Distinct().Count() })
                .ToListAsync(cancellationToken);

            foreach (var suspiciousIp in suspiciousIps)
            {
                if (string.IsNullOrEmpty(suspiciousIp.IpAddress)) continue;

                var threat = new SecurityThreat
                {
                    ThreatId = Guid.NewGuid().ToString(),
                    ThreatType = "SUSPICIOUS_IP",
                    Severity = "Medium",
                    Description = $"Suspicious IP {suspiciousIp.IpAddress} attempted login with {suspiciousIp.UserCount} different usernames",
                    IpAddress = suspiciousIp.IpAddress,
                    DetectedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ThreatData = new Dictionary<string, object>
                    {
                        ["UniqueUsernames"] = suspiciousIp.UserCount,
                        ["TimeWindow"] = "24 hours"
                    }
                };

                threats.Add(threat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting suspicious IP addresses");
        }

        return threats;
    }

    private async Task<List<SecurityThreat>> DetectUnusualUserBehaviorAsync(CancellationToken cancellationToken)
    {
        var threats = new List<SecurityThreat>();

        try
        {
            // Detect users with excessive activity
            var suspiciousUsers = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.Timestamp >= DateTime.UtcNow.AddHours(-1) &&
                             !string.IsNullOrEmpty(sae.UserId))
                .GroupBy(sae => sae.UserId)
                .Where(g => g.Count() >= 200) // 200+ actions in 1 hour
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var suspiciousUser in suspiciousUsers)
            {
                if (string.IsNullOrEmpty(suspiciousUser.UserId)) continue;

                var threat = new SecurityThreat
                {
                    ThreatId = Guid.NewGuid().ToString(),
                    ThreatType = "UNUSUAL_USER_BEHAVIOR",
                    Severity = "Medium",
                    Description = $"User {suspiciousUser.UserId} performed {suspiciousUser.Count} actions in 1 hour",
                    UserId = suspiciousUser.UserId,
                    DetectedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ThreatData = new Dictionary<string, object>
                    {
                        ["ActionCount"] = suspiciousUser.Count,
                        ["TimeWindow"] = "1 hour"
                    }
                };

                threats.Add(threat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting unusual user behavior");
        }

        return threats;
    }

    private async Task ReportSuspiciousUserBehaviorAsync(string userId, string action, string description, CancellationToken cancellationToken)
    {
        var threat = new SecurityThreat
        {
            ThreatId = Guid.NewGuid().ToString(),
            ThreatType = "SUSPICIOUS_USER_BEHAVIOR",
            Severity = "Medium",
            Description = description,
            UserId = userId,
            DetectedAt = DateTime.UtcNow,
            IsResolved = false,
            ThreatData = new Dictionary<string, object>
            {
                ["Action"] = action,
                ["AutoDetected"] = true
            }
        };

        await ReportThreatAsync(threat, cancellationToken);
    }

    private string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new char[12];

        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }

        return new string(password);
    }

    #endregion
}
