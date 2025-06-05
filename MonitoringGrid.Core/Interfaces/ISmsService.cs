namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service responsible for sending SMS notifications via email gateway
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends SMS to a single phone number via email gateway
    /// </summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends SMS to multiple phone numbers via email gateway
    /// </summary>
    Task<bool> SendSmsAsync(IEnumerable<string> phoneNumbers, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates SMS configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests SMS connectivity by sending a test message
    /// </summary>
    Task<bool> SendTestSmsAsync(string phoneNumber, CancellationToken cancellationToken = default);
}
