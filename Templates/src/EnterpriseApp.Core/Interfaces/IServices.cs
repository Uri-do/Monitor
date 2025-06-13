using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Enums;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Core.Interfaces;

/// <summary>
/// Domain service interface for DomainEntity business logic
/// </summary>
public interface IDomainEntityService
{
    /// <summary>
    /// Creates a new DomainEntity
    /// </summary>
    Task<DomainEntity> CreateAsync(CreateDomainEntityRequest request, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing DomainEntity
    /// </summary>
    Task<DomainEntity> UpdateAsync(int id, UpdateDomainEntityRequest request, string modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a DomainEntity (soft delete)
    /// </summary>
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a DomainEntity
    /// </summary>
    Task<DomainEntity> ActivateAsync(int id, string activatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a DomainEntity
    /// </summary>
    Task<DomainEntity> DeactivateAsync(int id, string deactivatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DomainEntity statistics
    /// </summary>
    Task<DomainEntityStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a DomainEntity for business rules
    /// </summary>
    Task<ValidationResult> ValidateAsync(DomainEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes bulk operations on DomainEntities
    /// </summary>
    Task<BulkOperationResult> ProcessBulkOperationAsync(BulkOperationRequest request, string processedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit service interface for tracking changes
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit event
    /// </summary>
    Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit event for entity creation
    /// </summary>
    Task LogCreationAsync(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit event for entity update
    /// </summary>
    Task LogUpdateAsync(string entityName, string entityId, string userId, object? oldValues = null, object? newValues = null, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit event for entity deletion
    /// </summary>
    Task LogDeletionAsync(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a custom audit event
    /// </summary>
    Task LogCustomAsync(string entityName, string entityId, string actionDescription, string userId, string? username = null, string? ipAddress = null, string severity = "Information", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit trail for an entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityName, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with filtering
    /// </summary>
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email with attachments
    /// </summary>
    Task<bool> SendEmailWithAttachmentsAsync(string to, string subject, string body, IEnumerable<EmailAttachment> attachments, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a templated email
    /// </summary>
    Task<bool> SendTemplatedEmailAsync(string to, string templateName, object model, CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification
    /// </summary>
    Task<bool> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notifications to multiple recipients
    /// </summary>
    Task<NotificationResult> SendBulkNotificationAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification templates
    /// </summary>
    Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache service interface
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached value
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a cached value
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached values by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}

/// <summary>
/// File storage service interface
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file
    /// </summary>
    Task<Stream> DownloadFileAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file
    /// </summary>
    Task<bool> DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata
    /// </summary>
    Task<FileMetadata?> GetFileMetadataAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a temporary download URL
    /// </summary>
    Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Background job service interface
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a background job
    /// </summary>
    Task<string> EnqueueAsync<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedules a background job
    /// </summary>
    Task<string> ScheduleAsync<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a recurring job
    /// </summary>
    Task<string> RecurringAsync<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);

    /// <summary>
    /// Deletes a job
    /// </summary>
    Task<bool> DeleteAsync(string jobId);

    /// <summary>
    /// Gets job status
    /// </summary>
    Task<JobStatus?> GetJobStatusAsync(string jobId);
}
