using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Single Sign-On (SSO) integration
/// </summary>
public interface ISsoService
{
    Task<SsoAuthResult> AuthenticateAsync(string token, CancellationToken cancellationToken = default);
    Task<SsoUser?> GetUserInfoAsync(string token, CancellationToken cancellationToken = default);
    Task<string> GetLoginUrlAsync(string returnUrl, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
