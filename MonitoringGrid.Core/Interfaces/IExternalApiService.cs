namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for external API integration service
/// </summary>
public interface IExternalApiService
{
    Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<bool> PostAsync<T>(string endpoint, T data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<string> GetStringAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}
