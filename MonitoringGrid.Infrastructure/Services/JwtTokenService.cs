using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// JWT token service for generating and validating tokens
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly MonitoringContext _context;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenService(
        IOptions<SecurityConfiguration> securityConfig,
        MonitoringContext context,
        ILogger<JwtTokenService> logger)
    {
        _jwtSettings = securityConfig.Value.Jwt;
        _context = context;
        _logger = logger;

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };
    }

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

            // Add roles from UserRoles navigation property
            if (user.UserRoles?.Any() == true)
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                        claims.Add(new Claim("role", userRole.Role.Name));

                        // Add permissions from role
                        if (userRole.Role.RolePermissions?.Any() == true)
                        {
                            foreach (var rolePermission in userRole.Role.RolePermissions)
                            {
                                if (rolePermission.Permission != null)
                                {
                                    claims.Add(new Claim("permission", rolePermission.Permission.Name));
                                }
                            }
                        }
                    }
                }
            }

            // Add additional claims
            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

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
        try
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            
            var refreshToken = Convert.ToBase64String(randomBytes);
            _logger.LogDebug("Generated refresh token");
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate refresh token");
            throw;
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
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
            var jsonToken = tokenHandler.ReadJwtToken(token);
            
            return jsonToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token expiration");
            return DateTime.MinValue;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHash = ComputeTokenHash(token);
            
            var blacklistedToken = await _context.Set<BlacklistedToken>()
                .FirstOrDefaultAsync(bt => bt.TokenHash == tokenHash, cancellationToken);

            var isBlacklisted = blacklistedToken != null && blacklistedToken.ExpiresAt > DateTime.UtcNow;
            
            if (isBlacklisted)
            {
                _logger.LogDebug("Token is blacklisted");
            }
            
            return isBlacklisted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if token is blacklisted");
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
                ExpiresAt = expiration,
                BlacklistedAt = DateTime.UtcNow
            };

            _context.Set<BlacklistedToken>().Add(blacklistedToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Token blacklisted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to blacklist token");
            throw;
        }
    }

    /// <summary>
    /// Clean up expired blacklisted tokens
    /// </summary>
    public async Task CleanupExpiredBlacklistedTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredTokens = await _context.Set<BlacklistedToken>()
                .Where(bt => bt.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                _context.Set<BlacklistedToken>().RemoveRange(expiredTokens);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired blacklisted tokens");
        }
    }

    /// <summary>
    /// Extract user ID from token
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst("user_id")?.Value ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract user ID from token");
            return null;
        }
    }

    /// <summary>
    /// Extract claims from token
    /// </summary>
    public Dictionary<string, string> GetClaimsFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return new Dictionary<string, string>();

            return principal.Claims.ToDictionary(c => c.Type, c => c.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract claims from token");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Check if token is about to expire
    /// </summary>
    public bool IsTokenNearExpiration(string token, int minutesBeforeExpiration = 5)
    {
        try
        {
            var expiration = GetTokenExpiration(token);
            return expiration <= DateTime.UtcNow.AddMinutes(minutesBeforeExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check token expiration");
            return true; // Assume expired on error
        }
    }

    private string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}

/// <summary>
/// Blacklisted token entity
/// </summary>
public class BlacklistedToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime BlacklistedAt { get; set; }
    public string? Reason { get; set; }
}
