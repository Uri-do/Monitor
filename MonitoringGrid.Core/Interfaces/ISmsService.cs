namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for SMS notification services
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS to a single phone number
    /// </summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS to multiple phone numbers
    /// </summary>
    Task<bool> SendSmsAsync(IEnumerable<string> phoneNumbers, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test SMS to verify configuration
    /// </summary>
    Task<bool> SendTestSmsAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the SMS configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
