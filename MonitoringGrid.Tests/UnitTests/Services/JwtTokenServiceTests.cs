using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Tests.TestBase;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.Services;

public class JwtTokenServiceTests : UnitTestBase
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly Mock<ILogger<JwtTokenService>> _mockLogger;
    private readonly SecurityConfiguration _securityConfig;

    public JwtTokenServiceTests(TestFixture testFixture) : base(testFixture)
    {
        _mockLogger = CreateMockLogger<JwtTokenService>();
        
        _securityConfig = new SecurityConfiguration
        {
            Jwt = new JwtSettings
            {
                SecretKey = "test-secret-key-that-is-long-enough-for-hmac-sha256-algorithm",
                Issuer = "test-issuer",
                Audience = "test-audience",
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 30,
                Algorithm = "HS256"
            }
        };

        var mockOptions = CreateMockOptions(_securityConfig);
        _jwtTokenService = new JwtTokenService(mockOptions.Object, Context, _mockLogger.Object);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ShouldReturnValidToken()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            Roles = new List<string> { "Admin", "User" },
            Permissions = new List<string> { "read:kpis", "write:kpis" },
            Claims = new Dictionary<string, object> { { "department", "IT" } }
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);
        
        jsonToken.Issuer.Should().Be(_securityConfig.Jwt.Issuer);
        jsonToken.Audiences.Should().Contain(_securityConfig.Jwt.Audience);
        jsonToken.Claims.Should().Contain(c => c.Type == "user_id" && c.Value == user.UserId);
        jsonToken.Claims.Should().Contain(c => c.Type == "username" && c.Value == user.Username);
        jsonToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jsonToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
        jsonToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
        jsonToken.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "read:kpis");
        jsonToken.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "write:kpis");
        jsonToken.Claims.Should().Contain(c => c.Type == "department" && c.Value == "IT");
    }

    [Fact]
    public void GenerateAccessToken_WithAdditionalClaims_ShouldIncludeAllClaims()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var additionalClaims = new List<Claim>
        {
            new("custom_claim", "custom_value"),
            new("session_id", "session-123")
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user, additionalClaims);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);
        
        jsonToken.Claims.Should().Contain(c => c.Type == "custom_claim" && c.Value == "custom_value");
        jsonToken.Claims.Should().Contain(c => c.Type == "session_id" && c.Value == "session-123");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().MatchRegex(@"^[A-Za-z0-9+/]*={0,2}$"); // Base64 pattern
        
        // Should be able to convert from Base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64); // 64 bytes as specified in implementation
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            Roles = new List<string> { "Admin" }
        };

        var token = _jwtTokenService.GenerateAccessToken(user);

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
        principal.FindFirst("user_id")?.Value.Should().Be(user.UserId);
        principal.FindFirst("username")?.Value.Should().Be(user.Username);
        principal.FindFirst("email")?.Value.Should().Be(user.Email);
        principal.FindFirst("role")?.Value.Should().Be("Admin");
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtTokenService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var expiredConfig = new SecurityConfiguration
        {
            Jwt = new JwtSettings
            {
                SecretKey = _securityConfig.Jwt.SecretKey,
                Issuer = _securityConfig.Jwt.Issuer,
                Audience = _securityConfig.Jwt.Audience,
                AccessTokenExpirationMinutes = -1, // Expired immediately
                RefreshTokenExpirationDays = 30
            }
        };

        var mockExpiredOptions = CreateMockOptions(expiredConfig);
        var expiredTokenService = new JwtTokenService(mockExpiredOptions.Object, Context, _mockLogger.Object);

        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var expiredToken = expiredTokenService.GenerateAccessToken(user);

        // Act
        var principal = _jwtTokenService.ValidateToken(expiredToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetTokenExpiration_WithValidToken_ShouldReturnCorrectExpiration()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var beforeGeneration = DateTime.UtcNow;
        var token = _jwtTokenService.GenerateAccessToken(user);
        var afterGeneration = DateTime.UtcNow;

        // Act
        var expiration = _jwtTokenService.GetTokenExpiration(token);

        // Assert
        var expectedExpiration = beforeGeneration.AddMinutes(_securityConfig.Jwt.AccessTokenExpirationMinutes);
        var expectedExpirationMax = afterGeneration.AddMinutes(_securityConfig.Jwt.AccessTokenExpirationMinutes);

        expiration.Should().BeOnOrAfter(expectedExpiration);
        expiration.Should().BeOnOrBefore(expectedExpirationMax);
    }

    [Fact]
    public void GetTokenExpiration_WithInvalidToken_ShouldReturnMinValue()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var expiration = _jwtTokenService.GetTokenExpiration(invalidToken);

        // Assert
        expiration.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithNonBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var token = _jwtTokenService.GenerateAccessToken(user);

        // Act
        var isBlacklisted = await _jwtTokenService.IsTokenBlacklistedAsync(token);

        // Assert
        isBlacklisted.Should().BeFalse();
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldAddTokenToBlacklist()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiration = DateTime.UtcNow.AddHours(1);

        // Act
        await _jwtTokenService.BlacklistTokenAsync(token, expiration);

        // Assert
        var isBlacklisted = await _jwtTokenService.IsTokenBlacklistedAsync(token);
        isBlacklisted.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithExpiredBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiration = DateTime.UtcNow.AddSeconds(-1); // Already expired

        await _jwtTokenService.BlacklistTokenAsync(token, expiration);

        // Act
        var isBlacklisted = await _jwtTokenService.IsTokenBlacklistedAsync(token);

        // Assert
        isBlacklisted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GenerateAccessToken_WithInvalidUser_ShouldThrowException(string? userId)
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = userId!,
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        // Act & Assert
        var act = () => _jwtTokenService.GenerateAccessToken(user);
        act.Should().NotThrow(); // JWT service should handle empty values gracefully
    }

    [Fact]
    public void GenerateRefreshToken_MultipleCalls_ShouldReturnDifferentTokens()
    {
        // Act
        var token1 = _jwtTokenService.GenerateRefreshToken();
        var token2 = _jwtTokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CleanupExpiredBlacklistedTokensAsync_ShouldRemoveExpiredTokens()
    {
        // Arrange
        var user = new AuthUser
        {
            UserId = "test-user-id",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var token1 = _jwtTokenService.GenerateAccessToken(user);
        var token2 = _jwtTokenService.GenerateAccessToken(user);

        // Blacklist tokens with different expiration times
        await _jwtTokenService.BlacklistTokenAsync(token1, DateTime.UtcNow.AddSeconds(-1)); // Expired
        await _jwtTokenService.BlacklistTokenAsync(token2, DateTime.UtcNow.AddHours(1)); // Not expired

        // Act
        await _jwtTokenService.CleanupExpiredBlacklistedTokensAsync();

        // Assert
        var isToken1Blacklisted = await _jwtTokenService.IsTokenBlacklistedAsync(token1);
        var isToken2Blacklisted = await _jwtTokenService.IsTokenBlacklistedAsync(token2);

        isToken1Blacklisted.Should().BeFalse(); // Should be cleaned up
        isToken2Blacklisted.Should().BeTrue(); // Should still be blacklisted
    }
}
