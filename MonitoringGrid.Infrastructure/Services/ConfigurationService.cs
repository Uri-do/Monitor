using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service implementation for system configuration management
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly MonitoringContext _context;
    private readonly ISecurityService _securityService;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        MonitoringContext context,
        ISecurityService securityService,
        ILogger<ConfigurationService> logger)
    {
        _context = context;
        _securityService = securityService;
        _logger = logger;
    }

    public async Task<string?> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting configuration value for key: {Key}", key);

        var config = await _context.Config
            .FirstOrDefaultAsync(c => c.ConfigKey == key, cancellationToken);

        if (config == null)
        {
            _logger.LogWarning("Configuration key not found: {Key}", key);
            return null;
        }

        // Decrypt if marked as encrypted
        if (config.IsEncrypted && !string.IsNullOrEmpty(config.ConfigValue))
        {
            try
            {
                return _securityService.Decrypt(config.ConfigValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt configuration value for key: {Key}", key);
                return null;
            }
        }

        return config.ConfigValue;
    }

    public async Task<T?> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await GetConfigValueAsync(key, cancellationToken);
        if (value == null)
            return default(T);

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), value, true);

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert configuration value for key {Key} to type {Type}", key, typeof(T).Name);
            return default(T);
        }
    }

    public async Task<bool> SetConfigValueAsync(string key, string value, string? description = null, 
        bool isEncrypted = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting configuration value for key: {Key}", key);

        try
        {
            var config = await _context.Config
                .FirstOrDefaultAsync(c => c.ConfigKey == key, cancellationToken);

            var valueToStore = value;
            if (isEncrypted && !string.IsNullOrEmpty(value))
            {
                valueToStore = _securityService.Encrypt(value);
            }

            if (config == null)
            {
                // Create new configuration
                config = new Config
                {
                    ConfigKey = key,
                    ConfigValue = valueToStore,
                    Description = description,
                    IsEncrypted = isEncrypted,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.Config.Add(config);
            }
            else
            {
                // Update existing configuration
                if (config.IsReadOnly)
                {
                    _logger.LogWarning("Attempted to modify read-only configuration key: {Key}", key);
                    return false;
                }

                config.ConfigValue = valueToStore;
                config.Description = description ?? config.Description;
                config.IsEncrypted = isEncrypted;
                config.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Configuration value set for key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set configuration value for key: {Key}", key);
            return false;
        }
    }

    public async Task<List<Config>> GetAllConfigAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all configuration values");

        return await _context.Config
            .OrderBy(c => c.Category)
            .ThenBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Config>> GetConfigByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting configuration values for category: {Category}", category);

        return await _context.Config
            .Where(c => c.Category == category)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteConfigAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting configuration value for key: {Key}", key);

        try
        {
            var config = await _context.Config
                .FirstOrDefaultAsync(c => c.ConfigKey == key, cancellationToken);

            if (config == null)
            {
                _logger.LogWarning("Configuration key not found for deletion: {Key}", key);
                return false;
            }

            if (config.IsReadOnly)
            {
                _logger.LogWarning("Attempted to delete read-only configuration key: {Key}", key);
                return false;
            }

            _context.Config.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Configuration value deleted for key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete configuration value for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ConfigExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.Config
            .AnyAsync(c => c.ConfigKey == key, cancellationToken);
    }

    public async Task<bool> SetEncryptedConfigValueAsync(string key, string value, string? description = null, 
        CancellationToken cancellationToken = default)
    {
        return await SetConfigValueAsync(key, value, description, isEncrypted: true, cancellationToken);
    }

    public async Task<string?> GetDecryptedConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetConfigValueAsync(key, cancellationToken);
    }
}
