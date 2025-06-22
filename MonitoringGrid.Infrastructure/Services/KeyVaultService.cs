using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Azure Key Vault service for secure secrets management
/// </summary>
public class KeyVaultService : IKeyVaultService
{
    private readonly AzureKeyVaultSettings _settings;
    private readonly ILogger<KeyVaultService> _logger;
    private readonly SecretClient? _secretClient;
    private readonly Dictionary<string, (string Value, DateTime CachedAt)> _secretCache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public KeyVaultService(
        IOptions<SecurityConfiguration> securityConfig,
        ILogger<KeyVaultService> logger)
    {
        _settings = securityConfig.Value.Encryption.KeyVault;
        _logger = logger;
        _secretCache = new Dictionary<string, (string, DateTime)>();

        if (_settings.IsEnabled && !string.IsNullOrWhiteSpace(_settings.VaultUrl))
        {
            try
            {
                _secretClient = CreateSecretClient();
                _logger.LogInformation("Azure Key Vault client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault client");
            }
        }
        else
        {
            _logger.LogWarning("Azure Key Vault is not enabled or configured");
        }
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return string.Empty;
            }

            // Check cache first
            if (_secretCache.TryGetValue(secretName, out var cachedSecret))
            {
                if (DateTime.UtcNow - cachedSecret.CachedAt < _cacheExpiration)
                {
                    _logger.LogDebug("Retrieved secret {SecretName} from cache", secretName);
                    return cachedSecret.Value;
                }
                else
                {
                    _secretCache.Remove(secretName);
                }
            }

            _logger.LogDebug("Retrieving secret {SecretName} from Key Vault", secretName);

            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var secretValue = response.Value.Value;

            // Cache the secret
            _secretCache[secretName] = (secretValue, DateTime.UtcNow);

            _logger.LogInformation("Successfully retrieved secret {SecretName} from Key Vault", secretName);
            return secretValue;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Key Vault", secretName);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            return string.Empty;
        }
    }

    public async Task<bool> SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return false;
            }

            _logger.LogDebug("Setting secret {SecretName} in Key Vault", secretName);

            var secret = new KeyVaultSecret(secretName, secretValue);
            await _secretClient.SetSecretAsync(secret, cancellationToken);

            // Update cache
            _secretCache[secretName] = (secretValue, DateTime.UtcNow);

            _logger.LogInformation("Successfully set secret {SecretName} in Key Vault", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName} in Key Vault", secretName);
            return false;
        }
    }

    public async Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return false;
            }

            _logger.LogDebug("Deleting secret {SecretName} from Key Vault", secretName);

            var operation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);

            // Remove from cache
            _secretCache.Remove(secretName);

            _logger.LogInformation("Successfully deleted secret {SecretName} from Key Vault", secretName);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Key Vault for deletion", secretName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName} from Key Vault", secretName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return new List<string>();
            }

            _logger.LogDebug("Retrieving secret names from Key Vault");

            var secretNames = new List<string>();
            await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                secretNames.Add(secretProperties.Name);
            }

            _logger.LogInformation("Retrieved {Count} secret names from Key Vault", secretNames.Count);
            return secretNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret names from Key Vault");
            return new List<string>();
        }
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return false;
            }

            _logger.LogDebug("Checking if secret {SecretName} exists in Key Vault", secretName);

            await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Secret {SecretName} does not exist in Key Vault", secretName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if secret {SecretName} exists in Key Vault", secretName);
            return false;
        }
    }

    public async Task<string> GetConnectionStringAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var secretName = $"ConnectionStrings--{name}";
            return await GetSecretAsync(secretName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve connection string {Name} from Key Vault", name);
            return string.Empty;
        }
    }

    /// <summary>
    /// Get database connection string with automatic fallback
    /// </summary>
    public async Task<string> GetDatabaseConnectionStringAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get from Key Vault first
            var connectionString = await GetConnectionStringAsync(databaseName, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogDebug("Retrieved database connection string for {DatabaseName} from Key Vault", databaseName);
                return connectionString;
            }

            // Fallback to environment variables or configuration
            _logger.LogWarning("Database connection string for {DatabaseName} not found in Key Vault, using fallback", databaseName);
            return Environment.GetEnvironmentVariable($"ConnectionStrings__{databaseName}") ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve database connection string for {DatabaseName}", databaseName);
            return string.Empty;
        }
    }

    /// <summary>
    /// Store multiple secrets in batch
    /// </summary>
    public async Task SetSecretsAsync(Dictionary<string, string> secrets, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return;
            }

            _logger.LogInformation("Setting {Count} secrets in Key Vault", secrets.Count);

            var tasks = secrets.Select(kvp => SetSecretAsync(kvp.Key, kvp.Value, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully set {Count} secrets in Key Vault", secrets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set multiple secrets in Key Vault");
            throw;
        }
    }

    /// <summary>
    /// Rotate a secret with backup
    /// </summary>
    public async Task<bool> RotateSecretAsync(string secretName, string newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return false;
            }

            _logger.LogInformation("Rotating secret {SecretName}", secretName);

            // Get current secret for backup
            var currentSecret = await GetSecretAsync(secretName, cancellationToken);
            
            // Create backup
            var backupSecretName = $"{secretName}-backup-{DateTime.UtcNow:yyyyMMddHHmmss}";
            if (!string.IsNullOrWhiteSpace(currentSecret))
            {
                await SetSecretAsync(backupSecretName, currentSecret, cancellationToken);
                _logger.LogDebug("Created backup secret {BackupSecretName}", backupSecretName);
            }

            // Set new secret
            await SetSecretAsync(secretName, newValue, cancellationToken);

            _logger.LogInformation("Successfully rotated secret {SecretName}", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate secret {SecretName}", secretName);
            return false;
        }
    }

    /// <summary>
    /// Validate Key Vault connectivity
    /// </summary>
    public async Task<bool> ValidateConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("Key Vault client is not initialized");
                return false;
            }

            // Try to list secrets to validate connectivity
            var secretsEnumerator = _secretClient.GetPropertiesOfSecretsAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
            try
            {
                if (await secretsEnumerator.MoveNextAsync())
                {
                    // Successfully retrieved at least one secret property
                }
            }
            finally
            {
                await secretsEnumerator.DisposeAsync();
            }

            _logger.LogInformation("Key Vault connectivity validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key Vault connectivity validation failed");
            return false;
        }
    }

    /// <summary>
    /// Clear secret cache
    /// </summary>
    public void ClearCache()
    {
        _secretCache.Clear();
        _logger.LogDebug("Secret cache cleared");
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public SecretCacheStatistics GetCacheStatistics()
    {
        var now = DateTime.UtcNow;
        var validEntries = _secretCache.Values.Count(v => now - v.CachedAt < _cacheExpiration);
        var expiredEntries = _secretCache.Count - validEntries;

        return new SecretCacheStatistics
        {
            TotalEntries = _secretCache.Count,
            ValidEntries = validEntries,
            ExpiredEntries = expiredEntries,
            CacheHitRate = _secretCache.Count > 0 ? (double)validEntries / _secretCache.Count * 100 : 0
        };
    }

    private SecretClient CreateSecretClient()
    {
        var vaultUri = new Uri(_settings.VaultUrl);

        if (_settings.UseManagedIdentity)
        {
            _logger.LogDebug("Using Managed Identity for Key Vault authentication");
            var credential = new DefaultAzureCredential();
            return new SecretClient(vaultUri, credential);
        }
        else
        {
            _logger.LogDebug("Using Client Secret for Key Vault authentication");
            var credential = new ClientSecretCredential(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret);
            return new SecretClient(vaultUri, credential);
        }
    }

    /// <summary>
    /// Cleanup expired cache entries
    /// </summary>
    private void CleanupExpiredCacheEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _secretCache
            .Where(kvp => now - kvp.Value.CachedAt >= _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _secretCache.Remove(key);
        }

        if (expiredKeys.Any())
        {
            _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }
}

/// <summary>
/// Secret cache statistics
/// </summary>
public class SecretCacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public double CacheHitRate { get; set; }
}
