using System.Security.Cryptography;

namespace MonitoringGrid.Infrastructure.Utilities;

/// <summary>
/// Utility class for password hashing operations
/// Consolidates functionality from HashGenerator and PasswordHashTool projects
/// </summary>
public static class PasswordHashUtility
{
    /// <summary>
    /// Generates a secure random salt for password hashing
    /// </summary>
    /// <returns>Base64 encoded salt string</returns>
    public static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// Hashes a password using PBKDF2 with the same parameters as SecurityService
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="salt">Base64 encoded salt (optional - will generate if not provided)</param>
    /// <returns>Formatted hash string in "salt:hash" format</returns>
    public static string HashPassword(string password, string? salt = null)
    {
        try
        {
            salt ??= GenerateSalt();

            // Use PBKDF2 with high iteration count for password hashing (same as SecurityService)
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 600000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return $"{salt}:{Convert.ToBase64String(hash)}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to hash password: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates SQL statement to create admin user with proper password hash
    /// </summary>
    /// <param name="username">Username (default: admin)</param>
    /// <param name="password">Password (default: Admin123!)</param>
    /// <param name="email">Email address</param>
    /// <returns>SQL INSERT statement</returns>
    public static string GenerateAdminUserSql(
        string username = "admin", 
        string password = "Admin123!", 
        string email = "admin@monitoringgrid.com")
    {
        var hash = HashPassword(password);
        var userId = Guid.NewGuid().ToString();

        return $@"
-- Generated Admin User Creation Script
DECLARE @AdminUserId NVARCHAR(50) = '{userId}'
DECLARE @PasswordHash NVARCHAR(255) = '{hash}'

INSERT INTO auth.Users (
    UserId, Username, Email, DisplayName, FirstName, LastName, 
    PasswordHash, IsActive, EmailConfirmed, CreatedDate, ModifiedDate, CreatedBy
)
VALUES (
    @AdminUserId, '{username}', '{email}', 'System Administrator', 
    'System', 'Administrator', @PasswordHash, 1, 1, 
    SYSUTCDATETIME(), SYSUTCDATETIME(), 'SYSTEM'
)

-- Assign admin role (assuming role exists)
INSERT INTO auth.UserRoles (UserId, RoleId)
SELECT @AdminUserId, RoleId FROM auth.Roles WHERE RoleName = 'Administrator'

PRINT '✅ Admin user created successfully'
PRINT 'Username: {username}'
PRINT 'Password: {password}'
";
    }

    /// <summary>
    /// Console application entry point for generating password hashes
    /// Replaces the standalone HashGenerator and PasswordHashTool projects
    /// </summary>
    public static void RunHashGeneratorTool(string[] args)
    {
        Console.WriteLine("=== MonitoringGrid Password Hash Generator ===");
        Console.WriteLine();

        string password = args.Length > 0 ? args[0] : "Admin123!";
        string? customSalt = args.Length > 1 ? args[1] : null;

        try
        {
            string salt = customSalt ?? GenerateSalt();
            string hash = HashPassword(password, salt);

            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"Salt: {salt}");
            Console.WriteLine($"Hash: {hash}");
            Console.WriteLine();
            Console.WriteLine("SQL Update Statement:");
            Console.WriteLine($"UPDATE auth.Users SET PasswordHash = '{hash}', ModifiedDate = SYSUTCDATETIME() WHERE Username = 'admin'");
            Console.WriteLine();
            Console.WriteLine("Complete Admin User Creation SQL:");
            Console.WriteLine(GenerateAdminUserSql("admin", password));
            Console.WriteLine();
            Console.WriteLine("=== Hash Generation Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
