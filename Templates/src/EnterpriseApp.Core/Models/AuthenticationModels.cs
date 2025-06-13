<!--#if (enableAuth)-->
using System.ComponentModel.DataAnnotations;

namespace EnterpriseApp.Core.Models;

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username or email
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remember me flag
    /// </summary>
    public bool RememberMe { get; set; }

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    public string? TwoFactorCode { get; set; }
}

/// <summary>
/// Authentication result
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Indicates if authentication was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// JWT token if authentication was successful
    /// </summary>
    public JwtToken? Token { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Indicates if two-factor authentication is required
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Indicates if password change is required
    /// </summary>
    public bool RequiresPasswordChange { get; set; }
}

/// <summary>
/// JWT token model
/// </summary>
public class JwtToken
{
    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Refresh token expiration time
    /// </summary>
    public DateTime RefreshExpiresAt { get; set; }

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token scopes
    /// </summary>
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// User information model
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Indicates if the user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLogin { get; set; }
}

/// <summary>
/// Change password request
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Reset password request
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Reset token
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Create user request
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Username
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name
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
    /// Department
    /// </summary>
    [StringLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Job title
    /// </summary>
    [StringLength(100)]
    public string? Title { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Role IDs to assign
    /// </summary>
    public List<string> RoleIds { get; set; } = new();

    /// <summary>
    /// Send welcome email
    /// </summary>
    public bool SendWelcomeEmail { get; set; } = true;
}

/// <summary>
/// Update user request
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name
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
    /// Department
    /// </summary>
    [StringLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Job title
    /// </summary>
    [StringLength(100)]
    public string? Title { get; set; }

    /// <summary>
    /// Indicates if the user is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Create role request
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Role name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Permission IDs to assign
    /// </summary>
    public List<string> PermissionIds { get; set; } = new();
}

/// <summary>
/// Update role request
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// Role name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the role is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Password validation result
/// </summary>
public class PasswordValidationResult
{
    /// <summary>
    /// Indicates if the password is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Password strength score (0-100)
    /// </summary>
    public int StrengthScore { get; set; }

    /// <summary>
    /// Password strength level
    /// </summary>
    public PasswordStrength Strength { get; set; }
}

/// <summary>
/// Password strength levels
/// </summary>
public enum PasswordStrength
{
    /// <summary>
    /// Very weak password
    /// </summary>
    VeryWeak = 0,

    /// <summary>
    /// Weak password
    /// </summary>
    Weak = 1,

    /// <summary>
    /// Fair password
    /// </summary>
    Fair = 2,

    /// <summary>
    /// Good password
    /// </summary>
    Good = 3,

    /// <summary>
    /// Strong password
    /// </summary>
    Strong = 4,

    /// <summary>
    /// Very strong password
    /// </summary>
    VeryStrong = 5
}
<!--#endif-->
