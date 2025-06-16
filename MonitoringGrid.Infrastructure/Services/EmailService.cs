using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Configuration;
using System.Net;
using System.Net.Mail;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service responsible for sending email notifications
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> config, ILogger<EmailService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(new[] { to }, subject, body, isHtml, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateSmtpClient();
            using var message = CreateMailMessage(to, subject, body, isHtml);

            _logger.LogDebug("Sending email to {Recipients}: {Subject}", string.Join(", ", to), subject);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", to));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}: {Message}", string.Join(", ", to), ex.Message);
            return false;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic validation - check required fields
            if (string.IsNullOrWhiteSpace(_config.SmtpServer) ||
                string.IsNullOrWhiteSpace(_config.FromAddress))
            {
                _logger.LogError("Email configuration is invalid: SMTP server and from address are required");
                return false;
            }

            using var client = CreateSmtpClient();
            
            // Test connection without sending email
            await Task.Run(() =>
            {
                // SmtpClient doesn't have an async method to test connection
                // This is a simple validation that the client can be created
                return client != null;
            }, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration validation failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> SendTestEmailAsync(string to, CancellationToken cancellationToken = default)
    {
        var subject = "Monitoring Grid - Test Email";
        var body = $@"
            <html>
            <body>
                <h2>Monitoring Grid Test Email</h2>
                <p>This is a test email from the Monitoring Grid system.</p>
                <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p><strong>Configuration:</strong></p>
                <ul>
                    <li>SMTP Server: {_config.SmtpServer}</li>
                    <li>SMTP Port: {_config.SmtpPort}</li>
                    <li>SSL Enabled: {_config.EnableSsl}</li>
                    <li>From Address: {_config.FromAddress}</li>
                </ul>
                <p>If you received this email, the email configuration is working correctly.</p>
            </body>
            </html>";

        return await SendEmailAsync(to, subject, body, true, cancellationToken);
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_config.SmtpServer, _config.SmtpPort)
        {
            EnableSsl = _config.EnableSsl,
            Timeout = _config.TimeoutSeconds * 1000, // Convert seconds to milliseconds
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_config.Username, _config.Password)
        };

        return client;
    }

    private MailMessage CreateMailMessage(IEnumerable<string> to, string subject, string body, bool isHtml)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_config.FromAddress, _config.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        foreach (var recipient in to)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                message.To.Add(recipient);
            }
        }

        return message;
    }
}
