namespace MonitoringGrid.Core.Models;

/// <summary>
/// Backup request model
/// </summary>
public class BackupRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> IncludedTables { get; set; } = new();
    public bool IncludeConfiguration { get; set; } = true;
    public bool IncludeHistoricalData { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Backup result model
/// </summary>
public class BackupResult
{
    public string BackupId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Restore request model
/// </summary>
public class RestoreRequest
{
    public string BackupId { get; set; } = string.Empty;
    public bool RestoreConfiguration { get; set; } = true;
    public bool RestoreHistoricalData { get; set; } = true;
    public List<string>? SpecificTables { get; set; }
}

/// <summary>
/// Restore result model
/// </summary>
public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public DateTime RestoredAt { get; set; }
    public List<string> RestoredTables { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Backup validation result model
/// </summary>
public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}
