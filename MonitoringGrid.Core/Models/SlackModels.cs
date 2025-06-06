namespace MonitoringGrid.Core.Models;

/// <summary>
/// Slack message model
/// </summary>
public class SlackMessage
{
    public string Channel { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? IconEmoji { get; set; }
    public List<SlackAttachment> Attachments { get; set; } = new();
    public List<SlackBlock> Blocks { get; set; } = new();
}

/// <summary>
/// Slack attachment model
/// </summary>
public class SlackAttachment
{
    public string? Color { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public List<SlackField> Fields { get; set; } = new();
    public string? Footer { get; set; }
    public long? Timestamp { get; set; }
}

/// <summary>
/// Slack field model
/// </summary>
public class SlackField
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Short { get; set; } = true;
}

/// <summary>
/// Slack block model
/// </summary>
public class SlackBlock
{
    public string Type { get; set; } = string.Empty;
    public object? Text { get; set; }
    public List<object>? Elements { get; set; }
}
