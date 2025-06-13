<!--#if (enableAuth)-->
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseApp.Core.Security;

/// <summary>
/// User entity for authentication and authorization
/// </summary>
[Table("Users", Schema = "auth")]
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [Key]
    [StringLength(50)]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique username for login
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address (also used for login)
    /// </summary>
    [Required]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the user
    /// </summary>
    [Required]
    [StringLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [StringLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    [StringLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Department or organizational unit
    /// </summary>
    [StringLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Job title
    /// </summary>
    [StringLength(100)]
    public string? Title { get; set; }

    /// <summary>
    /// Hashed password
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
    /// Indicates if the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if the email has been confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>
    /// Indicates if two-factor authentication is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// When the account lockout ends (if locked)
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// When the password was last changed
    /// </summary>
    public DateTime? LastPasswordChange { get; set; }

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user account was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the user account
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified the user account
    /// </summary>
    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// Roles assigned to this user
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Active refresh tokens for this user
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Password history for this user
    /// </summary>
    public virtual ICollection<UserPassword> PasswordHistory { get; set; } = new List<UserPassword>();

    // Business logic methods
    /// <summary>
    /// Checks if the account is locked out
    /// </summary>
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Locks the user account
    /// </summary>
    public void LockAccount(TimeSpan lockoutDuration)
    {
        LockoutEnd = DateTime.UtcNow.Add(lockoutDuration);
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlocks the user account
    /// </summary>
    public void UnlockAccount()
    {
        LockoutEnd = null;
        FailedLoginAttempts = 0;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed login attempt
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a successful login
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLogin = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the password hash
    /// </summary>
    public void UpdatePassword(string passwordHash, string? passwordSalt = null)
    {
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        LastPasswordChange = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the user's full name
    /// </summary>
    public string GetFullName()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            return $"{FirstName} {LastName}";
        
        return DisplayName;
    }

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    public bool HasRole(string roleName)
    {
        return UserRoles.Any(ur => ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all role names for this user
    /// </summary>
    public List<string> GetRoleNames()
    {
        return UserRoles.Select(ur => ur.Role.Name).ToList();
    }
}
<!--#endif-->
