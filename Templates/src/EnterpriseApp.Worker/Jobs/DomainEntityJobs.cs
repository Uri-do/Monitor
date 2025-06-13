using EnterpriseApp.Core.Common;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Core.Entities;

namespace EnterpriseApp.Worker.Jobs;

/// <summary>
/// Job for cleaning up inactive domain entities
/// </summary>
public class CleanupInactiveDomainEntitiesJob : BaseScheduledJob
{
    private readonly IDomainEntityService _domainEntityService;

    /// <summary>
    /// Initializes a new instance of the CleanupInactiveDomainEntitiesJob
    /// </summary>
    public CleanupInactiveDomainEntitiesJob(
        ILogger<CleanupInactiveDomainEntitiesJob> logger,
        IUnitOfWork unitOfWork,
        IDomainEntityService domainEntityService)
        : base(logger, unitOfWork)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
    }

    /// <summary>
    /// Job name
    /// </summary>
    public override string JobName => "CleanupInactiveDomainEntities";

    /// <summary>
    /// Job description
    /// </summary>
    public override string Description => "Cleans up domain entities that have been inactive for a specified period";

    /// <summary>
    /// Runs daily at 2 AM
    /// </summary>
    public override string CronExpression => "0 2 * * *";

    /// <summary>
    /// Executes the cleanup job
    /// </summary>
    protected override async Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var inactiveDays = GetParameter(context, "InactiveDays", 90);
        var batchSize = GetParameter(context, "BatchSize", 100);
        var dryRun = GetParameter(context, "DryRun", false);

        LogProgress("Starting cleanup of inactive domain entities (inactive for {InactiveDays} days)", inactiveDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-inactiveDays);
        var totalProcessed = 0;
        var totalDeleted = 0;

        try
        {
            var repository = UnitOfWork.Repository<DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                return Result.Failure(Error.Failure("Repository.NotFound", "DomainEntity repository not available"));
            }

            while (ShouldContinue(cancellationToken))
            {
                // Get batch of inactive entities
                var inactiveEntities = await repository.GetInactiveEntitiesAsync(cutoffDate, batchSize, cancellationToken);
                
                if (!inactiveEntities.Any())
                {
                    LogProgress("No more inactive entities found");
                    break;
                }

                LogProgress("Processing batch of {Count} inactive entities", inactiveEntities.Count());

                foreach (var entity in inactiveEntities)
                {
                    if (!ShouldContinue(cancellationToken))
                        break;

                    try
                    {
                        if (!dryRun)
                        {
                            await _domainEntityService.DeleteAsync(entity.Id, "SYSTEM_CLEANUP", cancellationToken);
                            totalDeleted++;
                        }

                        totalProcessed++;

                        if (totalProcessed % 50 == 0)
                        {
                            LogProgress("Processed {Processed} entities, deleted {Deleted}", totalProcessed, totalDeleted);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Failed to delete entity {EntityId}", entity.Id);
                        // Continue with next entity
                    }
                }

                // Save changes for this batch
                if (!dryRun)
                {
                    await UnitOfWork.SaveChangesAsync(cancellationToken);
                }

                // Small delay between batches to avoid overwhelming the system
                await Task.Delay(1000, cancellationToken);
            }

            SetMetadata(context, "TotalProcessed", totalProcessed);
            SetMetadata(context, "TotalDeleted", totalDeleted);
            SetMetadata(context, "DryRun", dryRun);

            var message = dryRun 
                ? $"Dry run completed. Would have deleted {totalDeleted} out of {totalProcessed} inactive entities"
                : $"Cleanup completed. Deleted {totalDeleted} out of {totalProcessed} inactive entities";

            LogProgress(message);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogError(ex, "Cleanup job failed after processing {Processed} entities", totalProcessed);
            return Result.Failure(Error.Failure("Cleanup.Failed", ex.Message));
        }
    }
}

/// <summary>
/// Job for generating domain entity statistics
/// </summary>
public class GenerateDomainEntityStatisticsJob : BaseScheduledJob
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the GenerateDomainEntityStatisticsJob
    /// </summary>
    public GenerateDomainEntityStatisticsJob(
        ILogger<GenerateDomainEntityStatisticsJob> logger,
        IUnitOfWork unitOfWork,
        IDomainEntityService domainEntityService,
        ICacheService cacheService)
        : base(logger, unitOfWork)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Job name
    /// </summary>
    public override string JobName => "GenerateDomainEntityStatistics";

    /// <summary>
    /// Job description
    /// </summary>
    public override string Description => "Generates and caches domain entity statistics";

    /// <summary>
    /// Runs every hour at minute 0
    /// </summary>
    public override string CronExpression => "0 * * * *";

    /// <summary>
    /// Executes the statistics generation job
    /// </summary>
    protected override async Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        LogProgress("Starting domain entity statistics generation");

        try
        {
            // Generate statistics
            var statistics = await _domainEntityService.GetStatisticsAsync(cancellationToken);

            // Cache the statistics
            var cacheKey = $"domainentity:statistics:{DateTime.UtcNow:yyyyMMdd}";
            await _cacheService.SetAsync(cacheKey, statistics, TimeSpan.FromHours(2), cancellationToken);

            // Also cache with a general key for quick access
            await _cacheService.SetAsync("domainentity:statistics:latest", statistics, TimeSpan.FromHours(1), cancellationToken);

            SetMetadata(context, "TotalEntities", statistics.TotalCount);
            SetMetadata(context, "ActiveEntities", statistics.ActiveCount);
            SetMetadata(context, "CacheKey", cacheKey);

            LogProgress("Statistics generated successfully. Total: {Total}, Active: {Active}", 
                statistics.TotalCount, statistics.ActiveCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to generate domain entity statistics");
            return Result.Failure(Error.Failure("Statistics.Failed", ex.Message));
        }
    }
}

/// <summary>
/// Job for processing domain entity data exports
/// </summary>
public class ProcessDataExportJob : BaseJob<DataExportParameters>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IFileService _fileService;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Initializes a new instance of the ProcessDataExportJob
    /// </summary>
    public ProcessDataExportJob(
        ILogger<ProcessDataExportJob> logger,
        IUnitOfWork unitOfWork,
        IDomainEntityService domainEntityService,
        IFileService fileService,
        IEmailService emailService)
        : base(logger, unitOfWork)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    /// <summary>
    /// Job name
    /// </summary>
    public override string JobName => "ProcessDataExport";

    /// <summary>
    /// Job description
    /// </summary>
    public override string Description => "Processes data export requests and sends results via email";

    /// <summary>
    /// Executes the data export job
    /// </summary>
    protected override async Task<Result> ExecuteJobAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = GetJobParameters(context);

        LogProgress("Starting data export for user {UserId}, format: {Format}", 
            parameters.UserId, parameters.Format);

        try
        {
            // Get data based on filters
            var repository = UnitOfWork.Repository<DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                return Result.Failure(Error.Failure("Repository.NotFound", "DomainEntity repository not available"));
            }

            var entities = await repository.GetFilteredEntitiesAsync(
                parameters.Category,
                parameters.Status,
                parameters.IsActive,
                cancellationToken);

            LogProgress("Retrieved {Count} entities for export", entities.Count());

            // Generate export file
            var exportData = await GenerateExportDataAsync(entities, parameters.Format, cancellationToken);
            
            // Save file
            var fileName = $"domainentities_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{parameters.Format.ToString().ToLower()}";
            var filePath = await _fileService.SaveFileAsync(fileName, exportData, cancellationToken);

            LogProgress("Export file generated: {FileName}", fileName);

            // Send email with download link
            if (!string.IsNullOrEmpty(parameters.EmailAddress))
            {
                await _emailService.SendDataExportEmailAsync(
                    parameters.EmailAddress,
                    fileName,
                    filePath,
                    entities.Count(),
                    cancellationToken);

                LogProgress("Export email sent to {Email}", parameters.EmailAddress);
            }

            SetMetadata(context, "ExportedCount", entities.Count());
            SetMetadata(context, "FileName", fileName);
            SetMetadata(context, "FilePath", filePath);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogError(ex, "Data export failed for user {UserId}", parameters.UserId);
            
            // Send error notification email
            if (!string.IsNullOrEmpty(parameters.EmailAddress))
            {
                try
                {
                    await _emailService.SendDataExportErrorEmailAsync(
                        parameters.EmailAddress,
                        ex.Message,
                        cancellationToken);
                }
                catch (Exception emailEx)
                {
                    LogError(emailEx, "Failed to send error notification email");
                }
            }

            return Result.Failure(Error.Failure("Export.Failed", ex.Message));
        }
    }

    /// <summary>
    /// Generates export data in the specified format
    /// </summary>
    private async Task<byte[]> GenerateExportDataAsync(IEnumerable<DomainEntity> entities, ExportFormat format, CancellationToken cancellationToken)
    {
        return format switch
        {
            ExportFormat.Excel => await GenerateExcelExportAsync(entities, cancellationToken),
            ExportFormat.Csv => await GenerateCsvExportAsync(entities, cancellationToken),
            ExportFormat.Json => await GenerateJsonExportAsync(entities, cancellationToken),
            ExportFormat.Xml => await GenerateXmlExportAsync(entities, cancellationToken),
            _ => throw new NotSupportedException($"Export format {format} is not supported")
        };
    }

    private async Task<byte[]> GenerateExcelExportAsync(IEnumerable<DomainEntity> entities, CancellationToken cancellationToken)
    {
        // Implementation would use a library like EPPlus or ClosedXML
        // For now, return placeholder
        await Task.Delay(100, cancellationToken);
        return System.Text.Encoding.UTF8.GetBytes("Excel export placeholder");
    }

    private async Task<byte[]> GenerateCsvExportAsync(IEnumerable<DomainEntity> entities, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        var csv = "Id,Name,Description,Category,Status,IsActive,CreatedDate\n";
        foreach (var entity in entities)
        {
            csv += $"{entity.Id},{entity.Name},{entity.Description},{entity.Category},{entity.Status},{entity.IsActive},{entity.CreatedDate:yyyy-MM-dd}\n";
        }
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    private async Task<byte[]> GenerateJsonExportAsync(IEnumerable<DomainEntity> entities, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        var json = System.Text.Json.JsonSerializer.Serialize(entities, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private async Task<byte[]> GenerateXmlExportAsync(IEnumerable<DomainEntity> entities, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        // XML export implementation
        return System.Text.Encoding.UTF8.GetBytes("<entities></entities>");
    }
}

/// <summary>
/// Parameters for data export job
/// </summary>
public class DataExportParameters
{
    /// <summary>
    /// User ID requesting the export
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email address to send the export to
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Export format
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Excel;

    /// <summary>
    /// Category filter
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Status filter
    /// </summary>
    public Core.Enums.DomainEntityStatus? Status { get; set; }

    /// <summary>
    /// Active status filter
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Export format enumeration
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Excel format
    /// </summary>
    Excel = 0,

    /// <summary>
    /// CSV format
    /// </summary>
    Csv = 1,

    /// <summary>
    /// JSON format
    /// </summary>
    Json = 2,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 3
}
