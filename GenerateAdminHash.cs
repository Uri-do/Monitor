using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Generating PBKDF2 Hash for Admin Password ===");
        
        string password = "Admin123!";
        string salt = GenerateSalt();
        string hash = HashPassword(password, salt);
        
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Salt: {salt}");
        Console.WriteLine($"Hash: {hash}");
        Console.WriteLine();
        Console.WriteLine("SQL Update Statement:");
        Console.WriteLine($"UPDATE auth.Users SET PasswordHash = '{hash}', PasswordSalt = '{salt}', ModifiedDate = SYSUTCDATETIME() WHERE Username = 'admin'");
        Console.WriteLine();
        Console.WriteLine("=== Hash Generation Complete ===");
    }
    
    static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }
    
    static string HashPassword(string password, string salt)
    {
        try
        {
            // Use PBKDF2 with high iteration count for password hashing (same as SecurityService)
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 600000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return $"{salt}:{Convert.ToBase64String(hash)}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to hash password: {ex.Message}");
            throw;
        }
    }
}
