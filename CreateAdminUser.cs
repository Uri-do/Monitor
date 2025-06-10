using System;
using System.Security.Cryptography;
using System.Text;

namespace MonitoringGrid.Tools
{
    /// <summary>
    /// Tool to generate correct password hash for admin user
    /// Uses the same PBKDF2 method as SecurityService
    /// </summary>
    public class CreateAdminUser
    {
        public static void Main(string[] args)
        {
            string password = "Admin123!";
            string salt = GenerateSalt();
            string hash = HashPassword(password, salt);
            
            Console.WriteLine("=== Admin User Password Hash Generator ===");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"Salt: {salt}");
            Console.WriteLine($"Hash: {hash}");
            Console.WriteLine();
            Console.WriteLine("SQL to create admin user:");
            Console.WriteLine($"DECLARE @PasswordHash NVARCHAR(255) = '{hash}'");
            Console.WriteLine("-- Use this hash in the INSERT statement for auth.Users");
        }
        
        public static string GenerateSalt()
        {
            var saltBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
        
        public static string HashPassword(string password, string salt)
        {
            // Use PBKDF2 with high iteration count for password hashing (same as SecurityService)
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 600000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            
            return $"{salt}:{Convert.ToBase64String(hash)}";
        }
    }
}
