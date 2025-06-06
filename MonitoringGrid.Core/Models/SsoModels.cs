namespace MonitoringGrid.Core.Models;

/// <summary>
/// SSO authentication result
/// </summary>
public class SsoAuthResult
{
    public bool IsSuccess { get; set; }
    public SsoUser? User { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// SSO user model
/// </summary>
public class SsoUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, object> Claims { get; set; } = new();
}
