using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Refresh token entity for JWT token management
/// </summary>
[Table("RefreshTokens", Schema = "auth")]
public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTime? RevokedAt { get; set; }

    [StringLength(100)]
    public string? RevokedBy { get; set; }

    [StringLength(500)]
    public string? RevokedReason { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    // Domain methods
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    public bool IsRevoked()
    {
        return RevokedAt.HasValue;
    }

    public bool IsValid()
    {
        return IsActive && !IsExpired() && !IsRevoked();
    }

    public void Revoke(string? revokedBy = null, string? reason = null)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedReason = reason;
    }
}
