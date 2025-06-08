using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a KPI type definition with metadata and validation requirements.
/// Defines the structure and behavior for different categories of KPIs
/// (e.g., success_rate, transaction_volume, threshold, trend_analysis).
/// </summary>
public class KpiType
{
    /// <summary>
    /// Unique identifier for the KPI type (e.g., "success_rate", "threshold").
    /// </summary>
    [Key]
    [Required(ErrorMessage = "KPI Type ID is required")]
    [MaxLength(50, ErrorMessage = "KPI Type ID cannot exceed 50 characters")]
    [RegularExpression(@"^[a-z_]+$", ErrorMessage = "KPI Type ID must contain only lowercase letters and underscores")]
    public string KpiTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the KPI type.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    [MinLength(1, ErrorMessage = "Name cannot be empty")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what this KPI type monitors and how it works.
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of required field names for this KPI type.
    /// Example: ["deviation", "lastMinutes"] for success_rate type.
    /// </summary>
    [Required(ErrorMessage = "Required fields specification is required")]
    public string RequiredFields { get; set; } = string.Empty;

    /// <summary>
    /// Default stored procedure name for this KPI type.
    /// Used as a template when creating new KPIs of this type.
    /// </summary>
    [MaxLength(255)]
    [RegularExpression(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$",
        ErrorMessage = "Invalid stored procedure name format")]
    public string? DefaultStoredProcedure { get; set; }

    /// <summary>
    /// Whether this KPI type is active and available for creating new KPIs.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this KPI type was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this KPI type was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// KPIs that use this type definition.
    /// </summary>
    public virtual ICollection<KPI> KPIs { get; set; } = new List<KPI>();

    // Domain methods
    /// <summary>
    /// Gets the required fields as a strongly-typed array.
    /// </summary>
    /// <returns>Array of required field names.</returns>
    public string[] GetRequiredFieldsArray()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(RequiredFields) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Validates that a KPI configuration has all required fields for this type.
    /// </summary>
    /// <param name="kpiConfiguration">KPI configuration to validate.</param>
    /// <returns>List of missing required fields.</returns>
    public List<string> ValidateKpiConfiguration(Dictionary<string, object?> kpiConfiguration)
    {
        var missingFields = new List<string>();
        var requiredFields = GetRequiredFieldsArray();

        foreach (var field in requiredFields)
        {
            if (!kpiConfiguration.ContainsKey(field) || kpiConfiguration[field] == null)
            {
                missingFields.Add(field);
            }
        }

        return missingFields;
    }

    /// <summary>
    /// Checks if this KPI type requires a specific field.
    /// </summary>
    /// <param name="fieldName">Name of the field to check.</param>
    /// <returns>True if the field is required for this KPI type.</returns>
    public bool RequiresField(string fieldName)
    {
        var requiredFields = GetRequiredFieldsArray();
        return requiredFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a user-friendly description of the required fields.
    /// </summary>
    /// <returns>Formatted string describing required fields.</returns>
    public string GetRequiredFieldsDescription()
    {
        var fields = GetRequiredFieldsArray();
        if (fields.Length == 0)
            return "No specific fields required";

        return $"Required fields: {string.Join(", ", fields)}";
    }
}
