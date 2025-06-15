namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for entities that support audit tracking
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// When the entity was created
    /// </summary>
    DateTime CreatedDate { get; set; }

    /// <summary>
    /// Who created the entity
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// When the entity was last modified
    /// </summary>
    DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Who last modified the entity
    /// </summary>
    string? ModifiedBy { get; set; }
}
