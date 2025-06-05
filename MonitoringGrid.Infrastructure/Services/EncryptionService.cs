using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using System.Security.Cryptography;
using System.Text;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Encryption service for data protection and hashing
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly EncryptionSettings _settings;
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _encryptionKey;
    private readonly byte[] _hashingSalt;

    public EncryptionService(
        IOptions<SecurityConfiguration> securityConfig,
        ILogger<EncryptionService> logger)
    {
        _settings = securityConfig.Value.Encryption;
        _logger = logger;

        // Initialize encryption key and salt
        _encryptionKey = DeriveKeyFromPassword(_settings.EncryptionKey, "encryption");
        _hashingSalt = DeriveKeyFromPassword(_settings.HashingSalt, "hashing");
    }

    public string Encrypt(string plainText)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = EncryptBytes(plainBytes);
            
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new CryptographicException("Encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        try
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var encryptedBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = DecryptBytes(encryptedBytes);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new CryptographicException("Decryption failed", ex);
        }
    }

    public byte[] EncryptBytes(byte[] data)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(data, 0, data.Length);
                csEncrypt.FlushFinalBlock();
            }

            return msEncrypt.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt bytes");
            throw new CryptographicException("Byte encryption failed", ex);
        }
    }

    public byte[] DecryptBytes(byte[] encryptedData)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract the actual encrypted data
            var cipherData = new byte[encryptedData.Length - iv.Length];
            Array.Copy(encryptedData, iv.Length, cipherData, 0, cipherData.Length);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msResult = new MemoryStream();
            
            csDecrypt.CopyTo(msResult);
            return msResult.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt bytes");
            throw new CryptographicException("Byte decryption failed", ex);
        }
    }

    public string Hash(string input)
    {
        try
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using var pbkdf2 = new Rfc2898DeriveBytes(input, _hashingSalt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32); // 256-bit hash
            
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash input");
            throw new CryptographicException("Hashing failed", ex);
        }
    }

    public bool VerifyHash(string input, string hash)
    {
        try
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash))
                return false;

            var computedHash = Hash(input);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedHash),
                Convert.FromBase64String(hash));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify hash");
            return false;
        }
    }

    public string GenerateSalt()
    {
        try
        {
            var saltBytes = new byte[32]; // 256-bit salt
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            
            return Convert.ToBase64String(saltBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate salt");
            throw new CryptographicException("Salt generation failed", ex);
        }
    }

    /// <summary>
    /// Encrypt sensitive PII data with additional protection
    /// </summary>
    public string EncryptPii(string piiData)
    {
        try
        {
            if (string.IsNullOrEmpty(piiData))
                return string.Empty;

            // Add additional entropy for PII data
            var entropy = GenerateEntropy();
            var dataWithEntropy = $"{entropy}:{piiData}";
            
            return Encrypt(dataWithEntropy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt PII data");
            throw new CryptographicException("PII encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypt sensitive PII data
    /// </summary>
    public string DecryptPii(string encryptedPiiData)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedPiiData))
                return string.Empty;

            var decryptedData = Decrypt(encryptedPiiData);
            
            // Remove entropy
            var parts = decryptedData.Split(':', 2);
            return parts.Length == 2 ? parts[1] : decryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt PII data");
            throw new CryptographicException("PII decryption failed", ex);
        }
    }

    /// <summary>
    /// Generate a secure random token
    /// </summary>
    public string GenerateSecureToken(int length = 32)
    {
        try
        {
            var tokenBytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            
            return Convert.ToBase64String(tokenBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate secure token");
            throw new CryptographicException("Token generation failed", ex);
        }
    }

    /// <summary>
    /// Hash password with salt using Argon2 (more secure for passwords)
    /// </summary>
    public string HashPassword(string password, string? salt = null)
    {
        try
        {
            salt ??= GenerateSalt();
            
            // Use Argon2 for password hashing (more secure than PBKDF2)
            // In a real implementation, use a library like Konscious.Security.Cryptography
            // For now, using PBKDF2 with high iteration count
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 600000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            
            return $"{salt}:{Convert.ToBase64String(hash)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password");
            throw new CryptographicException("Password hashing failed", ex);
        }
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            var parts = hashedPassword.Split(':', 2);
            if (parts.Length != 2)
                return false;

            var salt = parts[0];
            var hash = parts[1];
            
            var computedHash = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(hashedPassword));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }

    /// <summary>
    /// Securely wipe sensitive data from memory
    /// </summary>
    public void SecureWipe(byte[] data)
    {
        if (data != null)
        {
            RandomNumberGenerator.Fill(data);
            Array.Clear(data, 0, data.Length);
        }
    }

    /// <summary>
    /// Generate encryption key from password using PBKDF2
    /// </summary>
    private byte[] DeriveKeyFromPassword(string password, string purpose)
    {
        var salt = Encoding.UTF8.GetBytes($"MonitoringGrid.{purpose}.Salt.2024");
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256-bit key
    }

    /// <summary>
    /// Generate additional entropy for PII encryption
    /// </summary>
    private string GenerateEntropy()
    {
        var entropyBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(entropyBytes);
        return Convert.ToBase64String(entropyBytes);
    }

    /// <summary>
    /// Encrypt data using ChaCha20-Poly1305 (if available)
    /// </summary>
    public byte[] EncryptWithChaCha20(byte[] data, byte[] key, byte[] nonce)
    {
        try
        {
            // ChaCha20-Poly1305 implementation would go here
            // For now, fallback to AES
            return EncryptBytes(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChaCha20 encryption failed, falling back to AES");
            return EncryptBytes(data);
        }
    }

    /// <summary>
    /// Generate cryptographically secure random bytes
    /// </summary>
    public byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks
    /// </summary>
    public bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return a == b;

        if (a.Length != b.Length)
            return false;

        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
