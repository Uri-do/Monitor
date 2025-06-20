using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using BulkOperationResult = MonitoringGrid.Core.Models.BulkOperationResult;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Unified data management service interface consolidating bulk operations and database seeding
/// Replaces: IBulkOperationsService, IDbSeeder
/// </summary>
public interface IDataManagementService
{
    #region Bulk Operations Domain
    
    /// <summary>
    /// Creates multiple Indicators in a single transaction
    /// </summary>
    Task<BulkOperationResult> BulkCreateIndicatorsAsync(IEnumerable<CreateIndicatorRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple Indicators in a single transaction
    /// </summary>
    Task<BulkOperationResult> BulkUpdateIndicatorsAsync(IEnumerable<UpdateIndicatorRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple Indicators in a single transaction
    /// </summary>
    Task<BulkOperationResult> BulkDeleteIndicatorsAsync(IEnumerable<long> indicatorIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates multiple contacts in a single transaction
    /// </summary>
    Task<BulkOperationResult> BulkCreateContactsAsync(IEnumerable<CreateContactRequest> requests, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates multiple contacts in a single transaction
    /// </summary>
    Task<BulkOperationResult> BulkUpdateContactsAsync(IEnumerable<UpdateContactRequest> requests, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes multiple contacts in a single transaction
    /// </summary>
    Task<BulkOperationResult> BulkDeleteContactsAsync(IEnumerable<int> contactIds, CancellationToken cancellationToken = default);

    #endregion

    #region Data Archiving Domain
    
    /// <summary>
    /// Archives historical data older than the specified date
    /// </summary>
    Task<BulkOperationResult> ArchiveHistoricalDataAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Archives alert logs older than the specified date
    /// </summary>
    Task<BulkOperationResult> ArchiveAlertLogsAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Archives audit events older than the specified date
    /// </summary>
    Task<BulkOperationResult> ArchiveAuditEventsAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default);

    #endregion

    #region Database Maintenance Domain
    
    /// <summary>
    /// Optimizes database indexes for better performance
    /// </summary>
    Task<BulkOperationResult> BulkOptimizeIndexesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs database cleanup operations
    /// </summary>
    Task<BulkOperationResult> CleanupDatabaseAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates database statistics for query optimization
    /// </summary>
    Task<BulkOperationResult> UpdateDatabaseStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rebuilds fragmented indexes
    /// </summary>
    Task<BulkOperationResult> RebuildFragmentedIndexesAsync(double fragmentationThreshold = 30.0, CancellationToken cancellationToken = default);

    #endregion

    #region Database Seeding Domain
    
    /// <summary>
    /// Seeds the database with initial test data
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Seeds the database with production data
    /// </summary>
    Task SeedProductionDataAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Seeds specific data types
    /// </summary>
    Task SeedUsersAsync(CancellationToken cancellationToken = default);
    Task SeedRolesAndPermissionsAsync(CancellationToken cancellationToken = default);
    Task SeedIndicatorsAsync(CancellationToken cancellationToken = default);
    Task SeedContactsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the database has been seeded
    /// </summary>
    Task<bool> IsDatabaseSeededAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the database to initial state
    /// </summary>
    Task ResetDatabaseAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Data Validation Domain
    
    /// <summary>
    /// Validates data integrity across the database
    /// </summary>
    Task<DataValidationResult> ValidateDataIntegrityAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fixes common data integrity issues
    /// </summary>
    Task<BulkOperationResult> FixDataIntegrityIssuesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates foreign key relationships
    /// </summary>
    Task<DataValidationResult> ValidateForeignKeyRelationshipsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Import/Export Domain
    
    /// <summary>
    /// Exports data to various formats
    /// </summary>
    Task<ExportResult> ExportDataAsync(ExportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Imports data from various formats
    /// </summary>
    Task<ImportResult> ImportDataAsync(ImportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates import data before processing
    /// </summary>
    Task<ImportValidationResult> ValidateImportDataAsync(ImportRequest request, CancellationToken cancellationToken = default);

    #endregion
}
