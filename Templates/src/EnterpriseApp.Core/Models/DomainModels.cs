using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Core.Models;

/// <summary>
/// Request model for creating a DomainEntity
/// </summary>
public class CreateDomainEntityRequest
{
    /// <summary>
    /// Name of the DomainEntity
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the DomainEntity
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category of the DomainEntity
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Priority level (1-5)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Tags associated with the DomainEntity
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// External reference ID
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Request model for updating a DomainEntity
/// </summary>
public class UpdateDomainEntityRequest
{
    /// <summary>
    /// Name of the DomainEntity
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the DomainEntity
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category of the DomainEntity
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Priority level (1-5)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Status of the DomainEntity
    /// </summary>
    public DomainEntityStatus Status { get; set; }

    /// <summary>
    /// Tags associated with the DomainEntity
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// External reference ID
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Statistics for DomainEntity
/// </summary>
public class DomainEntityStatistics
{
    /// <summary>
    /// Total number of DomainEntities
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of active DomainEntities
    /// </summary>
    public int ActiveCount { get; set; }

    /// <summary>
    /// Number of inactive DomainEntities
    /// </summary>
    public int InactiveCount { get; set; }

    /// <summary>
    /// Count by status
    /// </summary>
    public Dictionary<DomainEntityStatus, int> StatusCounts { get; set; } = new();

    /// <summary>
    /// Count by category
    /// </summary>
    public Dictionary<string, int> CategoryCounts { get; set; } = new();

    /// <summary>
    /// Count by priority
    /// </summary>
    public Dictionary<int, int> PriorityCounts { get; set; } = new();

    /// <summary>
    /// Recent activity count (last 30 days)
    /// </summary>
    public int RecentActivityCount { get; set; }

    /// <summary>
    /// Average items per DomainEntity
    /// </summary>
    public double AverageItemsPerEntity { get; set; }
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates if validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Property name that failed validation
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Attempted value
    /// </summary>
    public object? AttemptedValue { get; set; }
}

/// <summary>
/// Validation warning
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Property name
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Warning code
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// Bulk operation request
/// </summary>
public class BulkOperationRequest
{
    /// <summary>
    /// Operation type
    /// </summary>
    public BulkOperationType Operation { get; set; }

    /// <summary>
    /// Entity IDs to operate on
    /// </summary>
    public List<int> EntityIds { get; set; } = new();

    /// <summary>
    /// Additional parameters for the operation
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Bulk operation result
/// </summary>
public class BulkOperationResult
{
    /// <summary>
    /// Number of entities processed successfully
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of entities that failed processing
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total number of entities processed
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;

    /// <summary>
    /// Errors that occurred during processing
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();

    /// <summary>
    /// Indicates if the operation was completely successful
    /// </summary>
    public bool IsSuccess => FailureCount == 0;
}

/// <summary>
/// Bulk operation error
/// </summary>
public class BulkOperationError
{
    /// <summary>
    /// Entity ID that failed
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// Bulk operation types
/// </summary>
public enum BulkOperationType
{
    /// <summary>
    /// Activate entities
    /// </summary>
    Activate,

    /// <summary>
    /// Deactivate entities
    /// </summary>
    Deactivate,

    /// <summary>
    /// Delete entities
    /// </summary>
    Delete,

    /// <summary>
    /// Update entities
    /// </summary>
    Update,

    /// <summary>
    /// Archive entities
    /// </summary>
    Archive,

    /// <summary>
    /// Restore entities
    /// </summary>
    Restore
}

/// <summary>
/// Audit log filter
/// </summary>
public class AuditLogFilter
{
    /// <summary>
    /// Entity name to filter by
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Entity ID to filter by
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// User ID to filter by
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Action to filter by
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// Start date for filtering
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 50;
}
