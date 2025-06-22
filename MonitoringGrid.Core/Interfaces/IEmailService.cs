namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for email notification services
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test email to verify configuration
    /// </summary>
    Task<bool> SendTestEmailAsync(string to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the email configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
