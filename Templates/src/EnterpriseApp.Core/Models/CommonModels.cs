namespace EnterpriseApp.Core.Models;

/// <summary>
/// Paged result wrapper
/// </summary>
/// <typeparam name="T">Type of items</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a paged result
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// API response wrapper
/// </summary>
/// <typeparam name="T">Type of data</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error code if request failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResult(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Non-generic API response
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse SuccessResult(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public new static ApiResponse ErrorResult(string message, string? errorCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Notification request model
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Recipient email or user ID
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Notification subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Notification message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.Email;

    /// <summary>
    /// Priority level
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Template name (if using template)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Template data
    /// </summary>
    public Dictionary<string, object>? TemplateData { get; set; }

    /// <summary>
    /// Scheduled send time (null for immediate)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Bulk notification request
/// </summary>
public class BulkNotificationRequest
{
    /// <summary>
    /// List of recipients
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Notification subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Notification message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.Email;

    /// <summary>
    /// Template name (if using template)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Template data
    /// </summary>
    public Dictionary<string, object>? TemplateData { get; set; }
}

/// <summary>
/// Notification result
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Number of successful notifications
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed notifications
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total number of notifications attempted
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;

    /// <summary>
    /// Failed recipients with error messages
    /// </summary>
    public Dictionary<string, string> Failures { get; set; } = new();

    /// <summary>
    /// Indicates if all notifications were successful
    /// </summary>
    public bool IsSuccess => FailureCount == 0;
}

/// <summary>
/// Notification template
/// </summary>
public class NotificationTemplate
{
    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

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
    public NotificationType Type { get; set; }

    /// <summary>
    /// Indicates if the template is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File content
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// File metadata
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// File key/identifier
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Who uploaded the file
    /// </summary>
    public string? UploadedBy { get; set; }

    /// <summary>
    /// File tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Job status
/// </summary>
public class JobStatus
{
    /// <summary>
    /// Job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job state
    /// </summary>
    public JobState State { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the job was started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Job result data
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error information if job failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Email notification
    /// </summary>
    Email = 0,

    /// <summary>
    /// SMS notification
    /// </summary>
    Sms = 1,

    /// <summary>
    /// Push notification
    /// </summary>
    Push = 2,

    /// <summary>
    /// In-app notification
    /// </summary>
    InApp = 3,

    /// <summary>
    /// Slack notification
    /// </summary>
    Slack = 4,

    /// <summary>
    /// Teams notification
    /// </summary>
    Teams = 5
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority
    /// </summary>
    Critical = 3
}

/// <summary>
/// Job states
/// </summary>
public enum JobState
{
    /// <summary>
    /// Job is enqueued
    /// </summary>
    Enqueued = 0,

    /// <summary>
    /// Job is scheduled
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Job is processing
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Succeeded = 3,

    /// <summary>
    /// Job failed
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Job was deleted
    /// </summary>
    Deleted = 5,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled = 6
}
