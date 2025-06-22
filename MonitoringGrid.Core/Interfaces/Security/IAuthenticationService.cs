using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Interfaces.Security;

/// <summary>
/// Interface for authentication services
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    Task<Result<User>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    Task<Result<User>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a JWT token for a user
    /// </summary>
    Task<Result<string>> GenerateTokenAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an authentication token
    /// </summary>
    Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user and invalidates their tokens
    /// </summary>
    Task<Result<bool>> LogoutAsync(string token, CancellationToken cancellationToken = default);
}
