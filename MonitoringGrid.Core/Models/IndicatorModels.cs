using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Models;

/// <summary>
/// Enhanced filter options for indicator queries
/// </summary>
public class IndicatorFilterOptions
{
    public string? SearchText { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? Priorities { get; set; }
    public List<int>? OwnerContactIds { get; set; }
    public List<long>? CollectorIds { get; set; }
    public List<int>? SchedulerIds { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastRunAfter { get; set; }
    public DateTime? LastRunBefore { get; set; }
    public bool? HasScheduler { get; set; }
    public bool? IsCurrentlyRunning { get; set; }
    public PaginationOptions? Pagination { get; set; }
    public SortingOptions? Sorting { get; set; }
}

/// <summary>
/// Pagination options for queries
/// </summary>
public class PaginationOptions
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;

    public int Skip => (Page - 1) * PageSize;
    public int Take => Math.Min(PageSize, MaxPageSize);
}

/// <summary>
/// Sorting options for queries
/// </summary>
public class SortingOptions
{
    public string? SortBy { get; set; }
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

/// <summary>
/// Sort direction enumeration
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Priority filter options
/// </summary>
public class PriorityFilterOptions
{
    public List<string>? Priorities { get; set; }
    public bool OrderByPriority { get; set; } = true;
    public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// Request model for creating indicators
/// </summary>
public class CreateIndicatorRequest
{
    public string IndicatorName { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public string? IndicatorDesc { get; set; }
    public long CollectorId { get; set; }
    public string CollectorItemName { get; set; } = string.Empty;
    public int? SchedulerId { get; set; }

    // Alias properties for compatibility with services that expect uppercase ID
    public long CollectorID => CollectorId;
    public int? SchedulerID => SchedulerId;
    public bool IsActive { get; set; } = true;
    public int LastMinutes { get; set; } = 60;
    public string ThresholdType { get; set; } = string.Empty;
    public string ThresholdField { get; set; } = string.Empty;
    public string ThresholdComparison { get; set; } = string.Empty;
    public decimal ThresholdValue { get; set; }
    public string Priority { get; set; } = "medium";
    public int OwnerContactId { get; set; }
    public int? AverageLastDays { get; set; }
    public List<int>? ContactIds { get; set; }
}

/// <summary>
/// Request model for updating indicators
/// </summary>
public class UpdateIndicatorRequest
{
    public long IndicatorId { get; set; }
    public string? IndicatorName { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorDesc { get; set; }
    public long? CollectorId { get; set; }
    public string? CollectorItemName { get; set; }
    public int? SchedulerId { get; set; }

    // Alias properties for compatibility with services that expect uppercase ID
    public long IndicatorID => IndicatorId;
    public long? CollectorID => CollectorId;
    public int? SchedulerID => SchedulerId;
    public bool? IsActive { get; set; }
    public int? LastMinutes { get; set; }
    public string? ThresholdType { get; set; }
    public string? ThresholdField { get; set; }
    public string? ThresholdComparison { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? Priority { get; set; }
    public int? OwnerContactId { get; set; }
    public int? AverageLastDays { get; set; }
    public string? UpdateReason { get; set; }
}

/// <summary>
/// Options for indicator deletion
/// </summary>
public class DeleteIndicatorOptions
{
    public bool Force { get; set; } = false;
    public bool ArchiveData { get; set; } = true;
    public string? DeletionReason { get; set; }
    public bool CheckDependencies { get; set; } = true;

    // Alias for compatibility with services that expect ForceDelete
    public bool ForceDelete => Force;
}

/// <summary>
/// Options for contact assignment
/// </summary>
public class ContactAssignmentOptions
{
    public bool ValidateContacts { get; set; } = true;
    public bool AllowDuplicates { get; set; } = false;
    public string? AssignmentReason { get; set; }
}

/// <summary>
/// Options for contact removal
/// </summary>
public class ContactRemovalOptions
{
    public bool CheckDependencies { get; set; } = true;
    public string? RemovalReason { get; set; }
    public bool RemoveAll { get; set; } = false;
}

/// <summary>
/// Filter options for execution history
/// </summary>
public class HistoryFilterOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Days { get; set; } = 30;
    public bool? WasSuccessful { get; set; }
    public bool? AlertTriggered { get; set; }
    public PaginationOptions? Pagination { get; set; }
    public SortingOptions? Sorting { get; set; }
}

/// <summary>
/// Options for dashboard generation
/// </summary>
public class DashboardOptions
{
    public bool IncludeStatistics { get; set; } = true;
    public bool IncludeRecentExecutions { get; set; } = true;
    public bool IncludeTrends { get; set; } = true;
    public int RecentExecutionsCount { get; set; } = 10;
    public int TrendDays { get; set; } = 7;
    public bool RefreshCache { get; set; } = false;
}

/// <summary>
/// Options for test execution
/// </summary>
public class TestExecutionOptions
{
    public bool IncludeRawData { get; set; } = true;
    public bool IncludeDiagnostics { get; set; } = true;
    public int? TimeoutSeconds { get; set; }
    public string? TestContext { get; set; }
}

/// <summary>
/// Options for statistics generation
/// </summary>
public class StatisticsOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Days { get; set; } = 30;
    public bool IncludeTrends { get; set; } = true;
    public bool IncludeComparisons { get; set; } = true;
    public int DefaultDays { get; set; } = 30;
}

/// <summary>
/// Options for indicator execution
/// </summary>
public class ExecutionOptions
{
    public string? ExecutionContext { get; set; } = "Manual";
    public bool SaveResults { get; set; } = true;
    public bool TriggerAlerts { get; set; } = true;
    public int? TimeoutSeconds { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// Request for bulk operations
/// </summary>
public class BulkUpdateRequest
{
    public List<long> IndicatorIds { get; set; } = new();
    public BulkUpdateOperation Operation { get; set; }
    public bool? IsActive { get; set; }
    public string? Priority { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Bulk operation types
/// </summary>
public enum BulkUpdateOperation
{
    Activate,
    Deactivate,
    UpdatePriority,
    UpdateScheduler,
    UpdateOwner,
    Delete
}

/// <summary>
/// Result of bulk indicator operations
/// </summary>
public class IndicatorBulkOperationResult
{
    public int TotalRequested { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkOperationError> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Bulk operation error details
/// </summary>
public class BulkOperationError
{
    public long IndicatorId { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Individual item result in bulk operations
/// </summary>
public class BulkOperationItem
{
    public long Id { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Request for indicator validation
/// </summary>
public class ValidateIndicatorRequest
{
    public long? IndicatorId { get; set; }
    public CreateIndicatorRequest? CreateRequest { get; set; }
    public UpdateIndicatorRequest? UpdateRequest { get; set; }
    public bool ValidateConfiguration { get; set; } = true;
    public bool ValidateConnections { get; set; } = true;
    public bool ValidatePermissions { get; set; } = true;
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public Dictionary<string, object>? AdditionalInfo { get; set; }
}

/// <summary>
/// Validation error details
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Value { get; set; }
}

/// <summary>
/// Validation warning details
/// </summary>
public class ValidationWarning
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Value { get; set; }
}

/// <summary>
/// Options for performance metrics
/// </summary>
public class PerformanceMetricsOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IncludeExecutionTimes { get; set; } = true;
    public bool IncludeResourceUsage { get; set; } = true;
    public bool IncludeTrends { get; set; } = true;
    public int DefaultDays { get; set; } = 30;
}
