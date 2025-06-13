<!--#if (enableAuth)-->
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseApp.Core.Security;

/// <summary>
/// Refresh token entity for JWT token management
/// </summary>
[Table("RefreshTokens", Schema = "auth")]
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID this token belongs to
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token value
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the token is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the token was revoked (if revoked)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Who revoked the token
    /// </summary>
    [StringLength(100)]
    public string? RevokedBy { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    [StringLength(500)]
    public string? RevokedReason { get; set; }

    /// <summary>
    /// IP address where the token was created
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    // Navigation properties
    /// <summary>
    /// User this token belongs to
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    // Business logic methods
    /// <summary>
    /// Checks if the token is expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Checks if the token is valid (active and not expired)
    /// </summary>
    public bool IsValid()
    {
        return IsActive && !IsExpired() && RevokedAt == null;
    }

    /// <summary>
    /// Revokes the token
    /// </summary>
    public void Revoke(string revokedBy, string reason)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedReason = reason;
    }
}

/// <summary>
/// User password history for preventing password reuse
/// </summary>
[Table("UserPasswords", Schema = "auth")]
public class UserPassword
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID this password belongs to
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Password hash
    /// </summary>
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Password salt
    /// </summary>
    [StringLength(255)]
    public string? PasswordSalt { get; set; }

    /// <summary>
    /// When the password was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this is the current active password
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Who created this password entry
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// User this password belongs to
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
<!--#endif-->
