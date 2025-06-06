namespace MonitoringGrid.Core.Models;

/// <summary>
/// LDAP user model
/// </summary>
public class LdapUser
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Phone { get; set; }
    public List<string> Groups { get; set; } = new();
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// LDAP group model
/// </summary>
public class LdapGroup
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Members { get; set; } = new();
}
