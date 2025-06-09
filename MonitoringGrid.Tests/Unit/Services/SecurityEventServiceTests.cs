using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Security;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Infrastructure.Data;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.Unit.Services;

/// <summary>
/// Unit tests for SecurityEventService
/// </summary>
public class SecurityEventServiceTests : IDisposable
{
    private readonly Mock<MonitoringContext> _contextMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<SecurityEventService>> _loggerMock;
    private readonly Mock<ICorrelationIdService> _correlationIdServiceMock;
    private readonly SecurityEventService _securityEventService;
    private readonly ITestOutputHelper _output;

    public SecurityEventServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _contextMock = new Mock<MonitoringContext>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<SecurityEventService>>();
        _correlationIdServiceMock = new Mock<ICorrelationIdService>();

        _correlationIdServiceMock.Setup(x => x.GetCorrelationId())
            .Returns("test-correlation-id");

        _securityEventService = new SecurityEventService(
            _contextMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _correlationIdServiceMock.Object);
    }

    [Fact]
    public async Task LogSecurityEventAsync_ShouldLogEventSuccessfully()
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            EventType = SecurityEventType.AuthenticationSuccess,
            UserId = "user123",
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            CorrelationId = "test-correlation-id",
            AdditionalData = new Dictionary<string, object>
            {
                ["TokenType"] = "JWT",
                ["LoginMethod"] = "Password"
            }
        };

        // Act
        await _securityEventService.LogSecurityEventAsync(securityEvent);

        // Assert
        // Verify logging was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _output.WriteLine($"Security event logged: {securityEvent.EventType}");
    }

    [Fact]
    public async Task IsTokenUsedAsync_WhenTokenNotUsed_ShouldReturnFalse()
    {
        // Arrange
        var tokenId = "unique-token-id";

        // Act
        var result = await _securityEventService.IsTokenUsedAsync(tokenId);

        // Assert
        Assert.False(result);
        
        _output.WriteLine($"Token {tokenId} is not used (as expected)");
    }

    [Fact]
    public async Task MarkTokenAsUsedAsync_ShouldMarkTokenAsUsed()
    {
        // Arrange
        var tokenId = "unique-token-id";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        await _securityEventService.MarkTokenAsUsedAsync(tokenId, expiry);
        var isUsed = await _securityEventService.IsTokenUsedAsync(tokenId);

        // Assert
        Assert.True(isUsed);
        
        _output.WriteLine($"Token {tokenId} marked as used successfully");
    }

    [Fact]
    public async Task IsSuspiciousActivityAsync_WithNormalActivity_ShouldReturnFalse()
    {
        // Arrange
        var userId = "user123";
        var ipAddress = "192.168.1.100";

        // Act
        var result = await _securityEventService.IsSuspiciousActivityAsync(userId, ipAddress);

        // Assert
        Assert.False(result);
        
        _output.WriteLine($"Normal activity detected for user {userId} from IP {ipAddress}");
    }

    [Fact]
    public async Task IsSuspiciousActivityAsync_WithExcessiveFailures_ShouldReturnTrue()
    {
        // Arrange
        var userId = "user123";
        var ipAddress = "192.168.1.100";

        // Simulate multiple failed authentication attempts
        for (int i = 0; i < 12; i++)
        {
            await _securityEventService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationFailure,
                UserId = userId,
                IpAddress = ipAddress,
                CorrelationId = $"correlation-{i}"
            });
        }

        // Act
        var result = await _securityEventService.IsSuspiciousActivityAsync(userId, ipAddress);

        // Assert
        Assert.True(result);
        
        _output.WriteLine($"Suspicious activity detected for user {userId} after multiple failures");
    }

    [Fact]
    public async Task IsSuspiciousActivityAsync_WithMultipleIPs_ShouldReturnTrue()
    {
        // Arrange
        var userId = "user123";
        var baseIp = "192.168.1.";

        // Simulate access from multiple IP addresses
        for (int i = 1; i <= 7; i++)
        {
            await _securityEventService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationSuccess,
                UserId = userId,
                IpAddress = $"{baseIp}{i}",
                CorrelationId = $"correlation-{i}"
            });
        }

        // Act
        var result = await _securityEventService.IsSuspiciousActivityAsync(userId, "192.168.1.1");

        // Assert
        Assert.True(result);
        
        _output.WriteLine($"Suspicious activity detected for user {userId} due to multiple IP addresses");
    }

    [Fact]
    public async Task IsSuspiciousActivityAsync_WithExcessiveRequestsFromIP_ShouldReturnTrue()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Simulate excessive requests from the same IP
        for (int i = 0; i < 6500; i++) // Exceed the 6000/hour limit
        {
            await _securityEventService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationSuccess,
                UserId = $"user{i % 10}",
                IpAddress = ipAddress,
                CorrelationId = $"correlation-{i}"
            });
        }

        // Act
        var result = await _securityEventService.IsSuspiciousActivityAsync("user1", ipAddress);

        // Assert
        Assert.True(result);
        
        _output.WriteLine($"Suspicious activity detected for IP {ipAddress} due to excessive requests");
    }

    [Fact]
    public async Task IsSuspiciousActivityAsync_WithMultipleUsersFromSameIP_ShouldReturnTrue()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Simulate multiple users from the same IP
        for (int i = 1; i <= 12; i++)
        {
            await _securityEventService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationSuccess,
                UserId = $"user{i}",
                IpAddress = ipAddress,
                CorrelationId = $"correlation-{i}"
            });
        }

        // Act
        var result = await _securityEventService.IsSuspiciousActivityAsync("user1", ipAddress);

        // Assert
        Assert.True(result);
        
        _output.WriteLine($"Suspicious activity detected for IP {ipAddress} due to multiple users");
    }

    [Fact]
    public async Task GetSecurityEventsAsync_ShouldReturnEvents()
    {
        // Arrange
        var filter = new SecurityEventFilter
        {
            EventType = SecurityEventType.AuthenticationFailure,
            FromDate = DateTime.UtcNow.AddHours(-1),
            ToDate = DateTime.UtcNow,
            PageSize = 10,
            PageNumber = 1
        };

        // Act
        var events = await _securityEventService.GetSecurityEventsAsync(filter);

        // Assert
        Assert.NotNull(events);
        Assert.IsType<List<SecurityEvent>>(events);
        
        _output.WriteLine($"Retrieved {events.Count} security events");
    }

    [Theory]
    [InlineData(SecurityEventType.AuthenticationFailure)]
    [InlineData(SecurityEventType.AuthenticationSuccess)]
    [InlineData(SecurityEventType.AuthorizationFailure)]
    [InlineData(SecurityEventType.SuspiciousActivity)]
    [InlineData(SecurityEventType.RateLimitExceeded)]
    public async Task LogSecurityEventAsync_WithDifferentEventTypes_ShouldLogCorrectly(SecurityEventType eventType)
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            EventType = eventType,
            UserId = "user123",
            IpAddress = "192.168.1.100",
            CorrelationId = "test-correlation-id"
        };

        // Act
        await _securityEventService.LogSecurityEventAsync(securityEvent);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(eventType.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _output.WriteLine($"Successfully logged security event: {eventType}");
    }

    [Fact]
    public async Task SecurityEventService_WithExceptionInLogging_ShouldHandleGracefully()
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            EventType = SecurityEventType.AuthenticationFailure,
            UserId = "user123",
            IpAddress = "192.168.1.100"
        };

        // Setup logger to throw exception
        _loggerMock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new InvalidOperationException("Logging error"));

        // Act & Assert - Should not throw
        await _securityEventService.LogSecurityEventAsync(securityEvent);
        
        _output.WriteLine("Exception handling in security event logging test passed");
    }

    [Fact]
    public async Task TokenReplayProtection_ShouldPreventReuse()
    {
        // Arrange
        var tokenId = "replay-test-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        await _securityEventService.MarkTokenAsUsedAsync(tokenId, expiry);
        
        // Try to use the same token again
        var isUsedFirstCheck = await _securityEventService.IsTokenUsedAsync(tokenId);
        var isUsedSecondCheck = await _securityEventService.IsTokenUsedAsync(tokenId);

        // Assert
        Assert.True(isUsedFirstCheck);
        Assert.True(isUsedSecondCheck);
        
        _output.WriteLine($"Token replay protection working correctly for token {tokenId}");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
