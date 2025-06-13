using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Infrastructure.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the EmailService class
    /// </summary>
    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(new[] { to }, subject, body, isHtml, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_emailSettings.Enabled)
            {
                _logger.LogWarning("Email service is disabled. Email not sent to: {Recipients}", string.Join(", ", to));
                return false;
            }

            using var client = CreateSmtpClient();
            using var message = CreateMailMessage(to, subject, body, isHtml);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to: {Recipients}", string.Join(", ", to));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {Recipients}", string.Join(", ", to));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailWithAttachmentsAsync(string to, string subject, string body, IEnumerable<EmailAttachment> attachments, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_emailSettings.Enabled)
            {
                _logger.LogWarning("Email service is disabled. Email with attachments not sent to: {Recipient}", to);
                return false;
            }

            using var client = CreateSmtpClient();
            using var message = CreateMailMessage(new[] { to }, subject, body, isHtml);

            // Add attachments
            foreach (var attachment in attachments)
            {
                var stream = new MemoryStream(attachment.Content);
                var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                message.Attachments.Add(mailAttachment);
            }

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email with {AttachmentCount} attachments sent successfully to: {Recipient}", 
                attachments.Count(), to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments to: {Recipient}", to);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendTemplatedEmailAsync(string to, string templateName, object model, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await LoadEmailTemplateAsync(templateName);
            if (template == null)
            {
                _logger.LogError("Email template '{TemplateName}' not found", templateName);
                return false;
            }

            var subject = ProcessTemplate(template.Subject, model);
            var body = ProcessTemplate(template.Body, model);

            return await SendEmailAsync(to, subject, body, true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email '{TemplateName}' to: {Recipient}", templateName, to);
            return false;
        }
    }

    /// <summary>
    /// Creates an SMTP client with configured settings
    /// </summary>
    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
            Timeout = _emailSettings.TimeoutSeconds * 1000
        };

        return client;
    }

    /// <summary>
    /// Creates a mail message with the specified parameters
    /// </summary>
    private MailMessage CreateMailMessage(IEnumerable<string> to, string subject, string body, bool isHtml)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        foreach (var recipient in to)
        {
            message.To.Add(recipient);
        }

        // Add reply-to if configured
        if (!string.IsNullOrEmpty(_emailSettings.ReplyToEmail))
        {
            message.ReplyToList.Add(new MailAddress(_emailSettings.ReplyToEmail));
        }

        return message;
    }

    /// <summary>
    /// Loads an email template by name
    /// </summary>
    private async Task<EmailTemplate?> LoadEmailTemplateAsync(string templateName)
    {
        try
        {
            var templatePath = Path.Combine(_emailSettings.TemplatesPath, $"{templateName}.json");
            
            if (!File.Exists(templatePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(templatePath);
            return System.Text.Json.JsonSerializer.Deserialize<EmailTemplate>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load email template: {TemplateName}", templateName);
            return null;
        }
    }

    /// <summary>
    /// Processes a template with the provided model
    /// </summary>
    private static string ProcessTemplate(string template, object model)
    {
        // Simple template processing - replace {{PropertyName}} with model values
        // In a real application, you might want to use a more sophisticated template engine like Handlebars or Razor
        
        var result = template;
        var properties = model.GetType().GetProperties();

        foreach (var property in properties)
        {
            var placeholder = $"{{{{{property.Name}}}}}";
            var value = property.GetValue(model)?.ToString() ?? string.Empty;
            result = result.Replace(placeholder, value);
        }

        return result;
    }

    /// <summary>
    /// Validates email addresses
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Email settings configuration
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Indicates if email service is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// SMTP host server
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = "noreply@example.com";

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = "Enterprise App";

    /// <summary>
    /// Reply-to email address
    /// </summary>
    public string ReplyToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Path to email templates
    /// </summary>
    public string TemplatesPath { get; set; } = "EmailTemplates";
}

/// <summary>
/// Email template model
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// Template subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Template body
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Template type
    /// </summary>
    public string Type { get; set; } = "Html";

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
