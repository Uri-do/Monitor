namespace MonitoringGrid.Core.Models;

/// <summary>
/// Teams adaptive card model
/// </summary>
public class TeamsAdaptiveCard
{
    public string Type { get; set; } = "AdaptiveCard";
    public string Version { get; set; } = "1.4";
    public List<object> Body { get; set; } = new();
    public List<object>? Actions { get; set; }
}
