using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Enhanced service interface for Indicator management operations with Result pattern
/// Replaces IKpiService with enterprise-grade error handling
/// </summary>
public interface IIndicatorService
{
    /// <summary>
    /// Get all indicators with optional filtering and pagination
    /// </summary>
    Task<Result<PagedResult<Indicator>>> GetAllIndicatorsAsync(
        IndicatorFilterOptions? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator by ID with enhanced error handling
    /// </summary>
    Task<Result<Indicator>> GetIndicatorByIdAsync(long indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active indicators with filtering options
    /// </summary>
    Task<Result<List<Indicator>>> GetActiveIndicatorsAsync(
        IndicatorFilterOptions? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicators by owner contact ID with pagination
    /// </summary>
    Task<Result<PagedResult<Indicator>>> GetIndicatorsByOwnerAsync(
        int ownerContactId,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicators by priority with enhanced filtering
    /// </summary>
    Task<Result<List<Indicator>>> GetIndicatorsByPriorityAsync(
        string priority,
        IndicatorFilterOptions? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicators that are due for execution with priority ordering
    /// </summary>
    Task<Result<List<Indicator>>> GetDueIndicatorsAsync(
        PriorityFilterOptions? priorityFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new indicator with comprehensive validation
    /// </summary>
    Task<Result<Indicator>> CreateIndicatorAsync(
        CreateIndicatorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing indicator with change tracking
    /// </summary>
    Task<Result<Indicator>> UpdateIndicatorAsync(
        UpdateIndicatorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an indicator with dependency checking
    /// </summary>
    Task<Result> DeleteIndicatorAsync(
        long indicatorId,
        DeleteIndicatorOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add contacts to an indicator with validation
    /// </summary>
    Task<Result> AddContactsToIndicatorAsync(
        long indicatorId,
        List<int> contactIds,
        ContactAssignmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove contacts from an indicator with dependency checking
    /// </summary>
    Task<Result> RemoveContactsFromIndicatorAsync(
        long indicatorId,
        List<int> contactIds,
        ContactRemovalOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator execution history with enhanced filtering
    /// </summary>
    Task<Result<PagedResult<IndicatorValueTrend>>> GetIndicatorHistoryAsync(
        long indicatorId,
        HistoryFilterOptions? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive indicator dashboard data
    /// </summary>
    Task<Result<IndicatorDashboard>> GetIndicatorDashboardAsync(
        DashboardOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test indicator execution without saving results with detailed diagnostics
    /// </summary>
    Task<Result<IndicatorTestResult>> TestIndicatorAsync(
        long indicatorId,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive indicator statistics with trend analysis
    /// </summary>
    Task<Result<IndicatorStatistics>> GetIndicatorStatisticsAsync(
        long indicatorId,
        StatisticsOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an indicator manually with comprehensive result tracking
    /// </summary>
    Task<Result<IndicatorExecutionResult>> ExecuteIndicatorAsync(
        long indicatorId,
        ExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations for multiple indicators
    /// </summary>
    Task<Result<BulkOperationResult>> BulkUpdateIndicatorsAsync(
        BulkUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate indicator configuration
    /// </summary>
    Task<Result<ValidationResult>> ValidateIndicatorAsync(
        ValidateIndicatorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator performance metrics
    /// </summary>
    Task<Result<IndicatorPerformanceMetrics>> GetIndicatorPerformanceAsync(
        long indicatorId,
        PerformanceMetricsOptions? options = null,
        CancellationToken cancellationToken = default);
}












