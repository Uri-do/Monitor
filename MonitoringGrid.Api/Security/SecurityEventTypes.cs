namespace MonitoringGrid.Api.Security;

/// <summary>
/// Security event service interface
/// </summary>
public interface ISecurityEventService
{
    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);

    /// <summary>
    /// Checks if a token has been used (replay protection)
    /// </summary>
    Task<bool> IsTokenUsedAsync(string tokenId);

    /// <summary>
    /// Marks a token as used
    /// </summary>
    Task MarkTokenAsUsedAsync(string tokenId, DateTime expiry);

    /// <summary>
    /// Checks for suspicious activity patterns
    /// </summary>
    Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress);

    /// <summary>
    /// Gets security events for analysis
    /// </summary>
    Task<List<SecurityEvent>> GetSecurityEventsAsync(SecurityEventFilter filter);
}

/// <summary>
/// Security event model
/// </summary>
public class SecurityEvent
{
    public int Id { get; set; }
    public SecurityEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    AuthenticationSuccess,
    AuthenticationFailure,
    AuthorizationFailure,
    TokenReplayAttempt,
    SuspiciousActivity,
    RateLimitExceeded,
    InvalidApiKey,
    PasswordChange,
    AccountLockout,
    PrivilegeEscalation
}

/// <summary>
/// Security event filter
/// </summary>
public class SecurityEventFilter
{
    public SecurityEventType? EventType { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
}
