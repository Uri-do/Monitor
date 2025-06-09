using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Blacklisted token entity for JWT token revocation
/// </summary>
[Table("BlacklistedTokens", Schema = "auth")]
public class BlacklistedToken
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Hash of the blacklisted token (for security, we don't store the actual token)
    /// </summary>
    [Required]
    [StringLength(255)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// When the token was blacklisted
    /// </summary>
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the token expires (for cleanup purposes)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Reason for blacklisting (optional)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// User who blacklisted the token (optional)
    /// </summary>
    [StringLength(50)]
    public string? BlacklistedBy { get; set; }

    /// <summary>
    /// IP address from which the blacklisting was requested (optional)
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    // Domain methods
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }
}
