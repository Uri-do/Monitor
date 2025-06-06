using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for LDAP/Active Directory integration
/// </summary>
public interface ILdapService
{
    Task<LdapUser?> AuthenticateUserAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<LdapUser?> GetUserAsync(string username, CancellationToken cancellationToken = default);
    Task<List<LdapUser>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<LdapGroup>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
