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

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Failed(params string[] errors)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Failed(IEnumerable<string> errors)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }
}
