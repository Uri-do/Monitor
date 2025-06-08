using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// User entity for authentication and authorization in the monitoring system.
/// Supports role-based access control, account lockout, and password management.
/// </summary>
[Table("Users", Schema = "auth")]
public class User
{
    /// <summary>
    /// Unique identifier for the user. Generated as GUID string.
    /// </summary>
    [Key]
    [StringLength(50)]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique username for login. Must be unique across the system.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's email address. Used for notifications and password recovery.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [StringLength(255)]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in the UI. Can be different from username.
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(255, ErrorMessage = "Display name cannot exceed 255 characters")]
    [MinLength(1, ErrorMessage = "Display name cannot be empty")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User's first name for personalization.
    /// </summary>
    [StringLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name for personalization.
    /// </summary>
    [StringLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Department or organizational unit the user belongs to.
    /// </summary>
    [StringLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Job title or role within the organization.
    /// </summary>
    [StringLength(100)]
    public string? Title { get; set; }

    /// <summary>
    /// Hashed password for authentication. Never store plain text passwords.
    /// </summary>
    [Required(ErrorMessage = "Password hash is required")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for password hashing. Enhances security against rainbow table attacks.
    /// </summary>
    [StringLength(255)]
    public string? PasswordSalt { get; set; }

    /// <summary>
    /// Indicates if the user account is active and can log in.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if the user's email address has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>
    /// Indicates if two-factor authentication is enabled for this user.
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Number of consecutive failed login attempts. Used for account lockout.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Failed login attempts cannot be negative")]
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// When the account lockout expires. Null if not locked out.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Timestamp of the user's last successful login.
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// When the user last changed their password. Used for password expiration.
    /// </summary>
    public DateTime? LastPasswordChange { get; set; }

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user account was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created this user account.
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified this user account.
    /// </summary>
    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// Roles assigned to this user for authorization.
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Active refresh tokens for this user's sessions.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Password history for preventing password reuse.
    /// </summary>
    public virtual ICollection<UserPassword> PasswordHistory { get; set; } = new List<UserPassword>();

    // Domain methods
    /// <summary>
    /// Determines if the user account is currently locked out.
    /// </summary>
    /// <returns>True if the account is locked out, false otherwise.</returns>
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if the user's password has expired based on the given expiration policy.
    /// </summary>
    /// <param name="passwordExpirationDays">Number of days after which passwords expire.</param>
    /// <returns>True if the password has expired, false otherwise.</returns>
    public bool IsPasswordExpired(int passwordExpirationDays)
    {
        if (!LastPasswordChange.HasValue)
            return true;

        return DateTime.UtcNow > LastPasswordChange.Value.AddDays(passwordExpirationDays);
    }

    /// <summary>
    /// Updates the last login timestamp to the current time.
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLogin = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets failed login attempts and removes any account lockout.
    /// Called after successful authentication.
    /// </summary>
    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments failed login attempts and applies lockout if threshold is reached.
    /// </summary>
    /// <param name="maxAttempts">Maximum allowed failed attempts before lockout.</param>
    /// <param name="lockoutDurationMinutes">Duration of lockout in minutes.</param>
    public void IncrementFailedLoginAttempts(int maxAttempts, int lockoutDurationMinutes)
    {
        if (maxAttempts <= 0) throw new ArgumentException("Max attempts must be positive", nameof(maxAttempts));
        if (lockoutDurationMinutes <= 0) throw new ArgumentException("Lockout duration must be positive", nameof(lockoutDurationMinutes));

        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
        }
        ModifiedDate = DateTime.UtcNow;
    }
}
