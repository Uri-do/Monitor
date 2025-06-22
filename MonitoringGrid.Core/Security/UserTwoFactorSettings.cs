using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Security;

/// <summary>
/// User two-factor authentication settings entity
/// </summary>
public class UserTwoFactorSettings
{
    /// <summary>
    /// User ID (primary key)
    /// </summary>
    [Key]
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether two-factor authentication is enabled for this user
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Secret key for TOTP generation (encrypted)
    /// </summary>
    [StringLength(255)]
    public string? Secret { get; set; }

    /// <summary>
    /// Recovery codes for account recovery (JSON array, encrypted)
    /// </summary>
    [Required]
    public string RecoveryCodes { get; set; } = "[]";

    /// <summary>
    /// When two-factor authentication was enabled
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// Navigation property to User entity
    /// </summary>
    public virtual Entities.User? User { get; set; }

    /// <summary>
    /// Helper property to get recovery codes as list
    /// </summary>
    public List<string> GetRecoveryCodesAsList()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(RecoveryCodes) ?? new();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Helper method to set recovery codes from list
    /// </summary>
    public void SetRecoveryCodesFromList(List<string> codes)
    {
        RecoveryCodes = System.Text.Json.JsonSerializer.Serialize(codes);
    }

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    public void Enable(string secret, List<string> recoveryCodes)
    {
        IsEnabled = true;
        Secret = secret;
        SetRecoveryCodesFromList(recoveryCodes);
        EnabledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        Secret = null;
        RecoveryCodes = "[]";
        EnabledAt = null;
    }

    /// <summary>
    /// Use a recovery code (removes it from the list)
    /// </summary>
    public bool UseRecoveryCode(string code)
    {
        var codes = GetRecoveryCodesAsList();
        if (codes.Remove(code))
        {
            SetRecoveryCodesFromList(codes);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if there are recovery codes available
    /// </summary>
    public bool HasRecoveryCodes()
    {
        return GetRecoveryCodesAsList().Count > 0;
    }
}
