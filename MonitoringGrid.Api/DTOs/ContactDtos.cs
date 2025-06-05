using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// Contact data transfer object for API responses
/// </summary>
public class ContactDto
{
    public int ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<KpiSummaryDto> AssignedKpis { get; set; } = new();
}

/// <summary>
/// Contact creation/update request
/// </summary>
public class CreateContactRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Contact update request
/// </summary>
public class UpdateContactRequest : CreateContactRequest
{
    public int ContactId { get; set; }
}

/// <summary>
/// KPI summary for contact assignments
/// </summary>
public class KpiSummaryDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public byte Priority { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Contact assignment request
/// </summary>
public class ContactAssignmentRequest
{
    public int ContactId { get; set; }
    public List<int> KpiIds { get; set; } = new();
}

/// <summary>
/// Bulk contact operation request
/// </summary>
public class BulkContactOperationRequest
{
    public List<int> ContactIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "activate", "deactivate", "delete"
}

/// <summary>
/// Contact validation result
/// </summary>
public class ContactValidationDto
{
    public int ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool EmailValid { get; set; }
    public bool PhoneValid { get; set; }
    public string? EmailError { get; set; }
    public string? PhoneError { get; set; }
    public bool CanReceiveEmail { get; set; }
    public bool CanReceiveSms { get; set; }
}

/// <summary>
/// Phone number validation attribute
/// </summary>
public class PhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true; // Allow null/empty for optional phone numbers

        var phone = value.ToString()!;
        
        // Basic phone validation - adjust regex as needed for your requirements
        var phoneRegex = new System.Text.RegularExpressions.Regex(@"^\+?[\d\s\-\(\)]{10,}$");
        return phoneRegex.IsMatch(phone);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a valid phone number";
    }
}
