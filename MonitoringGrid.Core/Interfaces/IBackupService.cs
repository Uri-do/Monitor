using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for backup and restore service
/// </summary>
public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(BackupRequest request, CancellationToken cancellationToken = default);
    Task<RestoreResult> RestoreBackupAsync(RestoreRequest request, CancellationToken cancellationToken = default);
    Task<List<Entities.BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);
    Task<BackupValidationResult> ValidateBackupAsync(string backupId, CancellationToken cancellationToken = default);
}
