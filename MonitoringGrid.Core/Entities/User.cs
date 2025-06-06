using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// User entity for authentication and authorization
/// </summary>
[Table("Users", Schema = "auth")]
public class User
{
    [Key]
    [StringLength(50)]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PasswordSalt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; } = false;

    public bool TwoFactorEnabled { get; set; } = false;

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockoutEnd { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime? LastPasswordChange { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserPassword> PasswordHistory { get; set; } = new List<UserPassword>();

    // Domain methods
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    public bool IsPasswordExpired(int passwordExpirationDays)
    {
        if (!LastPasswordChange.HasValue)
            return true;

        return DateTime.UtcNow > LastPasswordChange.Value.AddDays(passwordExpirationDays);
    }

    public void UpdateLastLogin()
    {
        LastLogin = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        ModifiedDate = DateTime.UtcNow;
    }

    public void IncrementFailedLoginAttempts(int maxAttempts, int lockoutDurationMinutes)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
        }
        ModifiedDate = DateTime.UtcNow;
    }
}
