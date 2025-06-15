namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for entities that support soft delete
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Indicates whether the entity is deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// When the entity was deleted
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}
