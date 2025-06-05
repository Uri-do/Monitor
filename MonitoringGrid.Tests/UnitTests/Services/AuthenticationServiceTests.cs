using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Tests.TestBase;
using Moq;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.Services;

public class AuthenticationServiceTests : UnitTestBase
{
    private readonly AuthenticationService _authenticationService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ISecurityAuditService> _mockAuditService;
    private readonly Mock<ITwoFactorService> _mockTwoFactorService;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly SecurityConfiguration _securityConfig;

    public AuthenticationServiceTests(TestFixture testFixture) : base(testFixture)
    {
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        _mockTwoFactorService = new Mock<ITwoFactorService>();
        _mockLogger = CreateMockLogger<AuthenticationService>();

        _securityConfig = new SecurityConfiguration
        {
            Jwt = new JwtSettings
            {
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 30
            },
            PasswordPolicy = new PasswordPolicy
            {
                MaxFailedAttempts = 5,
                LockoutDurationMinutes = 30,
                PasswordExpirationDays = 90
            }
        };

        var mockOptions = CreateMockOptions(_securityConfig);

        _authenticationService = new AuthenticationService(
            Context,
            _mockJwtTokenService.Object,
            _mockEncryptionService.Object,
            _mockAuditService.Object,
            _mockTwoFactorService.Object,
            mockOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "TestPassword123!",
            RememberMe = false
        };

        var user = new AuthUser
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = true
        };

        var userPassword = new UserPassword
        {
            UserId = user.UserId,
            PasswordHash = "hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        // Setup mocks
        Context.Set<AuthUser>().Add(user);
        Context.Set<UserPassword>().Add(userPassword);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.Password, userPassword.PasswordHash))
            .Returns(true);

        _mockTwoFactorService.Setup(x => x.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<AuthUser>(), It.IsAny<List<System.Security.Claims.Claim>>()))
            .Returns("access-token");

        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _authenticationService.AuthenticateAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNull();
        result.Token!.AccessToken.Should().Be("access-token");
        result.Token.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
        result.User!.UserId.Should().Be(user.UserId);

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogLoginAttemptAsync(
            request.Username, 
            "127.0.0.1", 
            true, 
            null, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidUsername_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistentuser",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username or password.");
        result.Token.Should().BeNull();
        result.User.Should().BeNull();

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogLoginAttemptAsync(
            request.Username, 
            "127.0.0.1", 
            false, 
            "User not found", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        var user = new AuthUser
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = true
        };

        var userPassword = new UserPassword
        {
            UserId = user.UserId,
            PasswordHash = "hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        Context.Set<AuthUser>().Add(user);
        Context.Set<UserPassword>().Add(userPassword);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.Password, userPassword.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authenticationService.AuthenticateAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username or password.");

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogLoginAttemptAsync(
            request.Username, 
            "127.0.0.1", 
            false, 
            "Invalid password", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "TestPassword123!"
        };

        var user = new AuthUser
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = false // Inactive user
        };

        var userPassword = new UserPassword
        {
            UserId = user.UserId,
            PasswordHash = "hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        Context.Set<AuthUser>().Add(user);
        Context.Set<UserPassword>().Add(userPassword);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.Password, userPassword.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authenticationService.AuthenticateAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account is disabled. Please contact your administrator.");

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogLoginAttemptAsync(
            request.Username, 
            "127.0.0.1", 
            false, 
            "User inactive", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithExpiredPassword_ShouldReturnPasswordChangeRequired()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "TestPassword123!"
        };

        var user = new AuthUser
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = true
        };

        var userPassword = new UserPassword
        {
            UserId = user.UserId,
            PasswordHash = "hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-100), // Expired password (> 90 days)
            IsActive = true
        };

        Context.Set<AuthUser>().Add(user);
        Context.Set<UserPassword>().Add(userPassword);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.Password, userPassword.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authenticationService.AuthenticateAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.RequiresPasswordChange.Should().BeTrue();
        result.ErrorMessage.Should().Be("Password has expired. Please change your password.");
    }

    [Fact]
    public async Task AuthenticateAsync_WithTwoFactorEnabled_ShouldRequireTwoFactorCode()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "TestPassword123!"
            // No TwoFactorCode provided
        };

        var user = new AuthUser
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = true
        };

        var userPassword = new UserPassword
        {
            UserId = user.UserId,
            PasswordHash = "hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        var twoFactorSettings = new UserTwoFactorSettings
        {
            UserId = user.UserId,
            IsEnabled = true
        };

        Context.Set<AuthUser>().Add(user);
        Context.Set<UserPassword>().Add(userPassword);
        Context.Set<UserTwoFactorSettings>().Add(twoFactorSettings);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.Password, userPassword.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authenticationService.AuthenticateAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.RequiresTwoFactor.Should().BeTrue();
        result.ErrorMessage.Should().Be("Two-factor authentication code required.");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var token = "valid-token";

        _mockJwtTokenService.Setup(x => x.ValidateToken(token))
            .Returns(new System.Security.Claims.ClaimsPrincipal());

        _mockJwtTokenService.Setup(x => x.IsTokenBlacklistedAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _authenticationService.ValidateTokenAsync(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "invalid-token";

        _mockJwtTokenService.Setup(x => x.ValidateToken(token))
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = await _authenticationService.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "blacklisted-token";

        _mockJwtTokenService.Setup(x => x.ValidateToken(token))
            .Returns(new System.Security.Claims.ClaimsPrincipal());

        _mockJwtTokenService.Setup(x => x.IsTokenBlacklistedAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authenticationService.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldReturnTrue()
    {
        // Arrange
        var userId = "user-123";
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var userPassword = new UserPassword
        {
            UserId = userId,
            PasswordHash = "old-hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        Context.Set<UserPassword>().Add(userPassword);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.CurrentPassword, userPassword.PasswordHash))
            .Returns(true);

        _mockEncryptionService.Setup(x => x.Hash(request.NewPassword))
            .Returns("new-hashed-password");

        // Act
        var result = await _authenticationService.ChangePasswordAsync(userId, request);

        // Assert
        result.Should().BeTrue();

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogPasswordChangeAsync(
            userId, 
            true, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldReturnFalse()
    {
        // Arrange
        var userId = "user-123";
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var userPassword = new UserPassword
        {
            UserId = userId,
            PasswordHash = "old-hashed-password",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        Context.Set<UserPassword>().Add(userPassword);
        await Context.SaveChangesAsync();

        _mockEncryptionService.Setup(x => x.VerifyHash(request.CurrentPassword, userPassword.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authenticationService.ChangePasswordAsync(userId, request);

        // Assert
        result.Should().BeFalse();

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogPasswordChangeAsync(
            userId, 
            false, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldBlacklistToken()
    {
        // Arrange
        var token = "token-to-revoke";
        var expiration = DateTime.UtcNow.AddHours(1);

        _mockJwtTokenService.Setup(x => x.GetTokenExpiration(token))
            .Returns(expiration);

        _mockJwtTokenService.Setup(x => x.BlacklistTokenAsync(token, expiration, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authenticationService.RevokeTokenAsync(token);

        // Assert
        result.Should().BeTrue();

        _mockJwtTokenService.Verify(x => x.BlacklistTokenAsync(token, expiration, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeTokenAndInvalidateRefreshTokens()
    {
        // Arrange
        var userId = "user-123";
        var token = "access-token";

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = "refresh-token",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        Context.Set<RefreshToken>().Add(refreshToken);
        await Context.SaveChangesAsync();

        _mockJwtTokenService.Setup(x => x.GetTokenExpiration(token))
            .Returns(DateTime.UtcNow.AddHours(1));

        // Act
        await _authenticationService.LogoutAsync(userId, token);

        // Assert
        var updatedRefreshToken = await Context.Set<RefreshToken>().FindAsync(refreshToken.Id);
        updatedRefreshToken!.IsActive.Should().BeFalse();
        updatedRefreshToken.RevokedAt.Should().NotBeNull();

        _mockJwtTokenService.Verify(x => x.BlacklistTokenAsync(
            token, 
            It.IsAny<DateTime>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
