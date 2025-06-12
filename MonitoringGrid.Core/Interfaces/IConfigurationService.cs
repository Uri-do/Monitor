using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for system configuration management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get configuration value by key
    /// </summary>
    Task<string?> GetConfigValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configuration value with type conversion
    /// </summary>
    Task<T?> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set configuration value
    /// </summary>
    Task<bool> SetConfigValueAsync(string key, string value, string? description = null, 
        bool isEncrypted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configuration values
    /// </summary>
    Task<List<Config>> GetAllConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configuration values by category
    /// </summary>
    Task<List<Config>> GetConfigByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete configuration value
    /// </summary>
    Task<bool> DeleteConfigAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if configuration key exists
    /// </summary>
    Task<bool> ConfigExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypt and store configuration value
    /// </summary>
    Task<bool> SetEncryptedConfigValueAsync(string key, string value, string? description = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get decrypted configuration value
    /// </summary>
    Task<string?> GetDecryptedConfigValueAsync(string key, CancellationToken cancellationToken = default);
}
