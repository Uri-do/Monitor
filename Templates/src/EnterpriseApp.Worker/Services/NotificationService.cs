using System.Net.Mail;
using System.Net;
using System.Text;

namespace EnterpriseApp.Worker.Services;

/// <summary>
/// Interface for notification services
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email notification
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a job completion notification
    /// </summary>
    Task SendJobCompletionNotificationAsync(string jobName, bool success, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a system alert notification
    /// </summary>
    Task SendSystemAlertAsync(string alertType, string message, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a worker health notification
    /// </summary>
    Task SendWorkerHealthNotificationAsync(bool isHealthy, string? message = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;
    private readonly SmtpClient _smtpClient;

    /// <summary>
    /// Initializes a new instance of the NotificationService
    /// </summary>
    public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _smtpClient = CreateSmtpClient();
    }

    /// <summary>
    /// Sends an email notification
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromAddress = _configuration.GetValue<string>("Email:FromAddress") ?? "noreply@enterpriseapp.com";
            var fromName = _configuration.GetValue<string>("Email:FromName") ?? "EnterpriseApp Worker";

            using var message = new MailMessage();
            message.From = new MailAddress(fromAddress, fromName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            await _smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw;
        }
    }

    /// <summary>
    /// Sends a job completion notification
    /// </summary>
    public async Task SendJobCompletionNotificationAsync(string jobName, bool success, string? details = null, CancellationToken cancellationToken = default)
    {
        var adminEmail = _configuration.GetValue<string>("Notifications:AdminEmail");
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogDebug("Admin email not configured, skipping job completion notification");
            return;
        }

        var status = success ? "Completed Successfully" : "Failed";
        var subject = $"Job {status}: {jobName}";
        
        var body = new StringBuilder();
        body.AppendLine($"<h2>Job Execution Report</h2>");
        body.AppendLine($"<p><strong>Job Name:</strong> {jobName}</p>");
        body.AppendLine($"<p><strong>Status:</strong> <span style='color: {(success ? "green" : "red")}'>{status}</span></p>");
        body.AppendLine($"<p><strong>Execution Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        
        if (!string.IsNullOrEmpty(details))
        {
            body.AppendLine($"<p><strong>Details:</strong></p>");
            body.AppendLine($"<pre>{details}</pre>");
        }

        body.AppendLine($"<hr>");
        body.AppendLine($"<p><small>This is an automated message from EnterpriseApp Worker Service.</small></p>");

        await SendEmailAsync(adminEmail, subject, body.ToString(), true, cancellationToken);
    }

    /// <summary>
    /// Sends a system alert notification
    /// </summary>
    public async Task SendSystemAlertAsync(string alertType, string message, string? details = null, CancellationToken cancellationToken = default)
    {
        var adminEmail = _configuration.GetValue<string>("Notifications:AdminEmail");
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogDebug("Admin email not configured, skipping system alert notification");
            return;
        }

        var subject = $"System Alert: {alertType}";
        
        var body = new StringBuilder();
        body.AppendLine($"<h2 style='color: red'>System Alert</h2>");
        body.AppendLine($"<p><strong>Alert Type:</strong> {alertType}</p>");
        body.AppendLine($"<p><strong>Message:</strong> {message}</p>");
        body.AppendLine($"<p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        
        if (!string.IsNullOrEmpty(details))
        {
            body.AppendLine($"<p><strong>Details:</strong></p>");
            body.AppendLine($"<pre>{details}</pre>");
        }

        body.AppendLine($"<hr>");
        body.AppendLine($"<p><small>This is an automated alert from EnterpriseApp Worker Service.</small></p>");

        await SendEmailAsync(adminEmail, subject, body.ToString(), true, cancellationToken);
    }

    /// <summary>
    /// Sends a worker health notification
    /// </summary>
    public async Task SendWorkerHealthNotificationAsync(bool isHealthy, string? message = null, CancellationToken cancellationToken = default)
    {
        var adminEmail = _configuration.GetValue<string>("Notifications:AdminEmail");
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogDebug("Admin email not configured, skipping worker health notification");
            return;
        }

        // Only send notifications for unhealthy status or recovery
        var sendNotification = _configuration.GetValue<bool>("Notifications:SendHealthNotifications", true);
        if (!sendNotification)
        {
            return;
        }

        var status = isHealthy ? "Healthy" : "Unhealthy";
        var subject = $"Worker Service Status: {status}";
        
        var body = new StringBuilder();
        body.AppendLine($"<h2>Worker Service Health Report</h2>");
        body.AppendLine($"<p><strong>Status:</strong> <span style='color: {(isHealthy ? "green" : "red")}'>{status}</span></p>");
        body.AppendLine($"<p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        
        if (!string.IsNullOrEmpty(message))
        {
            body.AppendLine($"<p><strong>Message:</strong> {message}</p>");
        }

        body.AppendLine($"<hr>");
        body.AppendLine($"<p><small>This is an automated health report from EnterpriseApp Worker Service.</small></p>");

        await SendEmailAsync(adminEmail, subject, body.ToString(), true, cancellationToken);
    }

    /// <summary>
    /// Creates and configures the SMTP client
    /// </summary>
    private SmtpClient CreateSmtpClient()
    {
        var smtpServer = _configuration.GetValue<string>("Email:SmtpServer") ?? "localhost";
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 25);
        var useSsl = _configuration.GetValue<bool>("Email:UseSsl", false);
        var username = _configuration.GetValue<string>("Email:Username");
        var password = _configuration.GetValue<string>("Email:Password");

        var client = new SmtpClient(smtpServer, smtpPort)
        {
            EnableSsl = useSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        return client;
    }

    /// <summary>
    /// Disposes the notification service
    /// </summary>
    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}

/// <summary>
/// Mock notification service for testing
/// </summary>
public class MockNotificationService : INotificationService
{
    private readonly ILogger<MockNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the MockNotificationService
    /// </summary>
    public MockNotificationService(ILogger<MockNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends an email notification (mock)
    /// </summary>
    public Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock Email - To: {To}, Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a job completion notification (mock)
    /// </summary>
    public Task SendJobCompletionNotificationAsync(string jobName, bool success, string? details = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock Job Completion Notification - Job: {JobName}, Success: {Success}", jobName, success);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a system alert notification (mock)
    /// </summary>
    public Task SendSystemAlertAsync(string alertType, string message, string? details = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock System Alert - Type: {AlertType}, Message: {Message}", alertType, message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a worker health notification (mock)
    /// </summary>
    public Task SendWorkerHealthNotificationAsync(bool isHealthy, string? message = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock Worker Health Notification - Healthy: {IsHealthy}, Message: {Message}", isHealthy, message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for notification service registration
/// </summary>
public static class NotificationServiceExtensions
{
    /// <summary>
    /// Adds notification services
    /// </summary>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var useRealNotifications = configuration.GetValue<bool>("Notifications:UseRealNotifications", true);
        
        if (useRealNotifications)
        {
            services.AddSingleton<INotificationService, NotificationService>();
        }
        else
        {
            services.AddSingleton<INotificationService, MockNotificationService>();
        }

        return services;
    }
}
