namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service responsible for sending email notifications
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
    /// Validates email configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests email connectivity by sending a test message
    /// </summary>
    Task<bool> SendTestEmailAsync(string to, CancellationToken cancellationToken = default);
}
