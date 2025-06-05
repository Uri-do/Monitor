using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Slack integration service
/// </summary>
public interface ISlackService
{
    Task<bool> SendMessageAsync(string channel, string message, CancellationToken cancellationToken = default);
    Task<bool> SendAlertAsync(string channel, AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<bool> SendRichMessageAsync(string channel, SlackMessage message, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Microsoft Teams integration service
/// </summary>
public interface ITeamsService
{
    Task<bool> SendMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default);
    Task<bool> SendAlertAsync(string webhookUrl, AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<bool> SendAdaptiveCardAsync(string webhookUrl, TeamsAdaptiveCard card, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for webhook integration service
/// </summary>
public interface IWebhookService
{
    Task<bool> SendWebhookAsync(WebhookConfiguration webhook, object payload, CancellationToken cancellationToken = default);
    Task<bool> SendAlertWebhookAsync(WebhookConfiguration webhook, AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<WebhookTestResult> TestWebhookAsync(WebhookConfiguration webhook, CancellationToken cancellationToken = default);
    Task<List<WebhookDeliveryLog>> GetDeliveryLogsAsync(int webhookId, DateTime? startDate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for external API integration service
/// </summary>
public interface IExternalApiService
{
    Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<bool> PostAsync<T>(string endpoint, T data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<string> GetStringAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for LDAP/Active Directory integration
/// </summary>
public interface ILdapService
{
    Task<LdapUser?> AuthenticateUserAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<LdapUser?> GetUserAsync(string username, CancellationToken cancellationToken = default);
    Task<List<LdapUser>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<LdapGroup>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Single Sign-On (SSO) integration
/// </summary>
public interface ISsoService
{
    Task<SsoAuthResult> AuthenticateAsync(string token, CancellationToken cancellationToken = default);
    Task<SsoUser?> GetUserInfoAsync(string token, CancellationToken cancellationToken = default);
    Task<string> GetLoginUrlAsync(string returnUrl, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for audit logging service
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(string userId, string action, string resource, object? details = null, CancellationToken cancellationToken = default);
    Task LogLoginAsync(string userId, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default);
    Task LogConfigurationChangeAsync(string userId, string configType, object oldValue, object newValue, CancellationToken cancellationToken = default);
    Task LogAlertActionAsync(string userId, int alertId, string action, string? notes = null, CancellationToken cancellationToken = default);
    Task<List<AuditLogEntry>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for reporting service
/// </summary>
public interface IReportingService
{
    Task<byte[]> GenerateKpiReportAsync(KpiReportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateAlertReportAsync(AlertReportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePerformanceReportAsync(PerformanceReportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCustomReportAsync(CustomReportRequest request, CancellationToken cancellationToken = default);
    Task<List<ReportTemplate>> GetReportTemplatesAsync(CancellationToken cancellationToken = default);
    Task<ReportSchedule> ScheduleReportAsync(ReportScheduleRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for notification routing service
/// </summary>
public interface INotificationRoutingService
{
    Task<NotificationResult> RouteNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<List<NotificationChannel>> GetAvailableChannelsAsync(CancellationToken cancellationToken = default);
    Task<NotificationPreferences> GetUserPreferencesAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserPreferencesAsync(string userId, NotificationPreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for data export service
/// </summary>
public interface IDataExportService
{
    Task<byte[]> ExportKpiDataAsync(DataExportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAlertDataAsync(DataExportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAuditDataAsync(DataExportRequest request, CancellationToken cancellationToken = default);
    Task<ExportJob> ScheduleExportAsync(ScheduledExportRequest request, CancellationToken cancellationToken = default);
    Task<List<ExportJob>> GetExportJobsAsync(string? userId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for backup and restore service
/// </summary>
public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(BackupRequest request, CancellationToken cancellationToken = default);
    Task<RestoreResult> RestoreBackupAsync(RestoreRequest request, CancellationToken cancellationToken = default);
    Task<List<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);
    Task<BackupValidationResult> ValidateBackupAsync(string backupId, CancellationToken cancellationToken = default);
}
