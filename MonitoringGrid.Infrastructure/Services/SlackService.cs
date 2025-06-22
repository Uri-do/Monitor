using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Slack integration service for sending notifications
/// </summary>
public class SlackService : ISlackService
{
    private readonly SlackConfiguration _config;
    private readonly ILogger<SlackService> _logger;
    private readonly HttpClient _httpClient;

    public SlackService(
        IOptions<SlackConfiguration> config,
        ILogger<SlackService> logger,
        HttpClient httpClient)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.BotToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MonitoringGrid/1.0");
    }

    public async Task<bool> SendMessageAsync(string channel, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Slack integration is disabled");
                return false;
            }

            var slackMessage = new SlackMessage
            {
                Channel = channel,
                Text = message,
                Username = _config.BotName,
                IconEmoji = _config.DefaultEmoji
            };

            return await SendRichMessageAsync(channel, (object)slackMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack message to channel {Channel}", channel);
            return false;
        }
    }

    public async Task<bool> SendAlertAsync(string channel, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Slack integration is disabled");
                return false;
            }

            var color = GetAlertColor(alert.Severity);
            var emoji = GetAlertEmoji(alert.Severity);

            var slackMessage = new SlackMessage
            {
                Channel = channel,
                Text = $"{emoji} *Alert Triggered*",
                Username = _config.BotName,
                IconEmoji = _config.DefaultEmoji,
                Attachments = new List<SlackAttachment>
                {
                    new SlackAttachment
                    {
                        Color = color,
                        Title = $"🚨 {alert.Subject}",
                        Text = alert.Description,
                        Fields = new List<SlackField>
                        {
                            new SlackField { Title = "KPI", Value = alert.Indicator, Short = true },
                            new SlackField { Title = "Owner", Value = alert.Owner, Short = true },
                            new SlackField { Title = "Severity", Value = alert.Severity, Short = true },
                            new SlackField { Title = "Priority", Value = alert.Priority.ToString(), Short = true },
                            new SlackField { Title = "Current Value", Value = alert.CurrentValue.ToString("N2"), Short = true },
                            new SlackField { Title = "Historical Value", Value = alert.HistoricalValue.ToString("N2"), Short = true },
                            new SlackField { Title = "Deviation", Value = $"{alert.Deviation:N2}%", Short = true },
                            new SlackField { Title = "Triggered", Value = $"<!date^{((DateTimeOffset)alert.TriggerTime).ToUnixTimeSeconds()}^{{date_short_pretty}} {{time}}|{alert.TriggerTime:yyyy-MM-dd HH:mm:ss}>", Short = true }
                        },
                        Footer = "Monitoring Grid",
                        Timestamp = ((DateTimeOffset)alert.TriggerTime).ToUnixTimeSeconds()
                    }
                }
            };

            return await SendRichMessageAsync(channel, (object)slackMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack alert to channel {Channel} for KPI {KpiId}", channel, alert.KpiId);
            return false;
        }
    }

    public async Task<bool> SendRichMessageAsync(string channel, object message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Slack integration is disabled");
                return false;
            }

            // Handle different message types
            object payload;
            if (message is SlackMessage slackMessage)
            {
                payload = new
                {
                    channel = slackMessage.Channel,
                    text = slackMessage.Text,
                    username = slackMessage.Username,
                    icon_emoji = slackMessage.IconEmoji,
                    attachments = slackMessage.Attachments.Select(a => new
                    {
                        color = a.Color,
                        title = a.Title,
                        text = a.Text,
                        fields = a.Fields.Select(f => new
                        {
                            title = f.Title,
                            value = f.Value,
                            @short = f.Short
                        }),
                        footer = a.Footer,
                        ts = a.Timestamp
                    }),
                    blocks = slackMessage.Blocks
                };
            }
            else
            {
                // For generic objects, serialize as-is
                payload = message;
            }

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://slack.com/api/chat.postMessage", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var slackResponse = JsonSerializer.Deserialize<SlackApiResponse>(responseContent);

                if (slackResponse?.Ok == true)
                {
                    _logger.LogInformation("Slack message sent successfully to channel {Channel}", channel);
                    return true;
                }
                else
                {
                    _logger.LogError("Slack API returned error: {Error}", slackResponse?.Error ?? "Unknown error");
                    return false;
                }
            }
            else
            {
                _logger.LogError("Failed to send Slack message. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rich Slack message to channel {Channel}", channel);
            return false;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogWarning("Slack integration is disabled");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.BotToken))
            {
                _logger.LogError("Slack bot token is not configured");
                return false;
            }

            // Test API connection
            var response = await _httpClient.GetAsync("https://slack.com/api/auth.test", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var slackResponse = JsonSerializer.Deserialize<SlackApiResponse>(responseContent);

                if (slackResponse?.Ok == true)
                {
                    _logger.LogInformation("Slack configuration is valid");
                    return true;
                }
                else
                {
                    _logger.LogError("Slack authentication failed: {Error}", slackResponse?.Error ?? "Unknown error");
                    return false;
                }
            }
            else
            {
                _logger.LogError("Failed to validate Slack configuration. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Slack configuration");
            return false;
        }
    }

    private string GetAlertColor(string severity)
    {
        return severity.ToLower() switch
        {
            "emergency" => "#FF0000", // Red
            "critical" => "#FF4500",  // Orange Red
            "high" => "#FFA500",      // Orange
            "medium" => "#FFFF00",    // Yellow
            "low" => "#00FF00",       // Green
            _ => "#808080"            // Gray
        };
    }

    private string GetAlertEmoji(string severity)
    {
        return severity.ToLower() switch
        {
            "emergency" => "🚨",
            "critical" => "🔥",
            "high" => "⚠️",
            "medium" => "⚡",
            "low" => "ℹ️",
            _ => "📊"
        };
    }

    private string GetPriorityText(int priority)
    {
        return priority switch
        {
            1 => "Critical",
            2 => "High",
            3 => "Medium",
            _ => "Low"
        };
    }
}

/// <summary>
/// Slack API response model
/// </summary>
public class SlackApiResponse
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
}

/// <summary>
/// Slack configuration
/// </summary>
public class SlackConfiguration
{
    public bool IsEnabled { get; set; } = false;
    public string BotToken { get; set; } = string.Empty;
    public string BotName { get; set; } = "MonitoringGrid";
    public string DefaultEmoji { get; set; } = ":robot_face:";
    public string DefaultChannel { get; set; } = "#monitoring";
    public List<string> AlertChannels { get; set; } = new();
    public Dictionary<string, string> ChannelMappings { get; set; } = new();
}
