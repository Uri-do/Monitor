using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for entities that support validation
/// </summary>
public interface IValidatableEntity
{
    /// <summary>
    /// Validates the entity
    /// </summary>
    /// <returns>Validation result</returns>
    ValidationResult Validate();
}


