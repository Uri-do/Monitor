using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;
using System.Security.Cryptography;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Two-factor authentication service for TOTP (Time-based One-Time Password) implementation
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<TwoFactorService> _logger;
    private const int CodeLength = 6;
    private const int TimeStepSeconds = 30;

    public TwoFactorService(
        MonitoringContext context,
        ILogger<TwoFactorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateSecretAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating 2FA secret for user {UserId}", userId);

            // Generate a random 32-byte secret
            var secretBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secretBytes);
            }

            var secret = Convert.ToBase64String(secretBytes);

            // Store or update the secret for the user
            var existingSettings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (existingSettings != null)
            {
                existingSettings.Secret = secret;
                existingSettings.IsEnabled = false; // Reset enabled status until verified
            }
            else
            {
                var newSettings = new UserTwoFactorSettings
                {
                    UserId = userId,
                    Secret = secret,
                    IsEnabled = false,
                    RecoveryCodes = new List<string>()
                };
                _context.Set<UserTwoFactorSettings>().Add(newSettings);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA secret generated for user {UserId}", userId);
            return secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating 2FA secret for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateCodeAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating 2FA code for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId && tfs.IsEnabled, cancellationToken);

            if (settings?.Secret == null)
            {
                _logger.LogWarning("2FA not enabled or secret not found for user {UserId}", userId);
                return false;
            }

            // Validate TOTP code
            var isValid = ValidateTotpCode(settings.Secret, code);

            if (isValid)
            {
                _logger.LogInformation("2FA code validated successfully for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid 2FA code for user {UserId}", userId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating 2FA code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string> GenerateQrCodeAsync(string userId, string secret, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating QR code for user {UserId}", userId);

            // Get user information
            var user = await _context.Set<AuthUser>()
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                throw new ArgumentException($"User {userId} not found");
            }

            // Create TOTP URI
            var issuer = "MonitoringGrid";
            var accountName = $"{user.Email}";
            var totpUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            _logger.LogInformation("QR code URI generated for user {UserId}", userId);
            
            // Return the URI - the frontend can generate the actual QR code
            return totpUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> EnableTwoFactorAsync(string userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Enabling 2FA for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (settings?.Secret == null)
            {
                _logger.LogWarning("2FA secret not found for user {UserId}", userId);
                return false;
            }

            // Validate the verification code
            if (!ValidateTotpCode(settings.Secret, verificationCode))
            {
                _logger.LogWarning("Invalid verification code for enabling 2FA for user {UserId}", userId);
                return false;
            }

            // Enable 2FA
            settings.IsEnabled = true;
            settings.EnabledAt = DateTime.UtcNow;

            // Generate recovery codes
            settings.RecoveryCodes = GenerateRecoveryCodes();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA enabled successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Disabling 2FA for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (settings == null)
            {
                _logger.LogWarning("2FA settings not found for user {UserId}", userId);
                return false;
            }

            settings.IsEnabled = false;
            settings.Secret = null;
            settings.RecoveryCodes.Clear();
            settings.EnabledAt = null;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA disabled successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<string>> GenerateRecoveryCodesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating recovery codes for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId, cancellationToken);

            if (settings == null)
            {
                throw new ArgumentException($"2FA settings not found for user {userId}");
            }

            // Generate new recovery codes
            var recoveryCodes = GenerateRecoveryCodes();
            settings.RecoveryCodes = recoveryCodes;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recovery codes generated for user {UserId}", userId);
            return recoveryCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recovery codes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateRecoveryCodeAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating recovery code for user {UserId}", userId);

            var settings = await _context.Set<UserTwoFactorSettings>()
                .FirstOrDefaultAsync(tfs => tfs.UserId == userId && tfs.IsEnabled, cancellationToken);

            if (settings?.RecoveryCodes == null || !settings.RecoveryCodes.Any())
            {
                _logger.LogWarning("No recovery codes found for user {UserId}", userId);
                return false;
            }

            // Check if the code exists in recovery codes
            if (!settings.RecoveryCodes.Contains(code))
            {
                _logger.LogWarning("Invalid recovery code for user {UserId}", userId);
                return false;
            }

            // Remove the used recovery code
            settings.RecoveryCodes.Remove(code);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recovery code validated and consumed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating recovery code for user {UserId}", userId);
            return false;
        }
    }

    private bool ValidateTotpCode(string secret, string code)
    {
        try
        {
            var secretBytes = Convert.FromBase64String(secret);
            var currentTimeStep = GetCurrentTimeStep();

            // Check current time step and adjacent ones (to account for clock drift)
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedCode = GenerateTotpCode(secretBytes, timeStep);
                
                if (expectedCode == code)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TOTP code");
            return false;
        }
    }

    private string GenerateTotpCode(byte[] secret, long timeStep)
    {
        var timeStepBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timeStepBytes);
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timeStepBytes);

        var offset = hash[hash.Length - 1] & 0x0F;
        var binaryCode = (hash[offset] & 0x7F) << 24
                        | (hash[offset + 1] & 0xFF) << 16
                        | (hash[offset + 2] & 0xFF) << 8
                        | (hash[offset + 3] & 0xFF);

        var code = binaryCode % (int)Math.Pow(10, CodeLength);
        return code.ToString().PadLeft(CodeLength, '0');
    }

    private long GetCurrentTimeStep()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTime / TimeStepSeconds;
    }

    private List<string> GenerateRecoveryCodes()
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < 10; i++) // Generate 10 recovery codes
        {
            var codeBytes = new byte[4];
            rng.GetBytes(codeBytes);
            var code = BitConverter.ToUInt32(codeBytes, 0).ToString("D8");
            codes.Add(code);
        }

        return codes;
    }
}




