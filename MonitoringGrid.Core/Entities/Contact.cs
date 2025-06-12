using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a contact for notifications
/// </summary>
public class Contact
{
    public int ContactId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<IndicatorContact> IndicatorContacts { get; set; } = new List<IndicatorContact>();

    // Domain methods
    public bool CanReceiveEmail()
    {
        return !string.IsNullOrWhiteSpace(Email) && IsValidEmail(Email);
    }

    public bool CanReceiveSms()
    {
        return !string.IsNullOrWhiteSpace(Phone) && IsValidPhone(Phone);
    }

    public bool HasValidContactMethod()
    {
        return CanReceiveEmail() || CanReceiveSms();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Basic phone validation - can be enhanced based on requirements
        var phoneRegex = new System.Text.RegularExpressions.Regex(@"^\+?[\d\s\-\(\)]{10,}$");
        return phoneRegex.IsMatch(phone);
    }
}
