namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Azure Key Vault or similar secret management services
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// Gets a secret value by name
    /// </summary>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a secret value
    /// </summary>
    Task<bool> SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret
    /// </summary>
    Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret names
    /// </summary>
    Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a secret exists
    /// </summary>
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
}
