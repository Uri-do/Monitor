using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Configuration;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service responsible for sending SMS notifications via email gateway
/// </summary>
public class SmsService : ISmsService
{
    private readonly IEmailService _emailService;
    private readonly MonitoringOptions _config;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        IEmailService emailService,
        IOptions<MonitoringOptions> config,
        ILogger<SmsService> logger)
    {
        _emailService = emailService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        return await SendSmsAsync(new[] { phoneNumber }, message, cancellationToken);
    }

    public async Task<bool> SendSmsAsync(IEnumerable<string> phoneNumbers, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.SmsGateway))
            {
                _logger.LogError("SMS gateway not configured");
                return false;
            }

            var validPhoneNumbers = phoneNumbers.Where(IsValidPhoneNumber).ToList();
            if (!validPhoneNumbers.Any())
            {
                _logger.LogWarning("No valid phone numbers provided for SMS");
                return false;
            }

            _logger.LogDebug("Sending SMS to {Count} recipients via email gateway", validPhoneNumbers.Count);

            // Convert phone numbers to email addresses using the SMS gateway
            var emailAddresses = validPhoneNumbers.Select(phone => FormatSmsEmailAddress(phone)).ToList();

            // Format message for SMS (keep it short)
            var smsMessage = FormatSmsMessage(message);

            // Send via email service
            var success = await _emailService.SendEmailAsync(
                emailAddresses, 
                "Alert", // Simple subject for SMS
                smsMessage, 
                false, // Plain text for SMS
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("SMS sent successfully to {Count} recipients", validPhoneNumbers.Count);
            }
            else
            {
                _logger.LogError("Failed to send SMS to recipients");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.SmsGateway))
            {
                _logger.LogError("SMS gateway is not configured");
                return false;
            }

            // Validate that the email service is configured (since SMS uses email gateway)
            var emailValid = await _emailService.ValidateConfigurationAsync(cancellationToken);
            if (!emailValid)
            {
                _logger.LogError("Email service configuration is invalid, SMS cannot work");
                return false;
            }

            _logger.LogDebug("SMS configuration is valid");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS configuration validation failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> SendTestSmsAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var message = $"Monitoring Grid Test SMS - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        return await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    private string FormatSmsEmailAddress(string phoneNumber)
    {
        // Clean the phone number (remove spaces, dashes, parentheses)
        var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        // Remove leading country code if present (assuming US numbers)
        if (cleanPhone.StartsWith("1") && cleanPhone.Length == 11)
        {
            cleanPhone = cleanPhone.Substring(1);
        }

        // Format as email address using the SMS gateway
        return $"{cleanPhone}@{_config.SmsGateway}";
    }

    private static string FormatSmsMessage(string message)
    {
        // SMS messages should be short (160 characters max for standard SMS)
        const int maxLength = 160;
        
        if (message.Length <= maxLength)
            return message;

        // Truncate and add ellipsis
        return message.Substring(0, maxLength - 3) + "...";
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Basic phone number validation
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        // Should have at least 10 digits (US format)
        return digitsOnly.Length >= 10;
    }
}
