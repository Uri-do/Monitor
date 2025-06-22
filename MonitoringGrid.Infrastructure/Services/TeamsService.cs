using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Microsoft Teams integration service for sending notifications
/// </summary>
public class TeamsService : ITeamsService
{
    private readonly TeamsConfiguration _config;
    private readonly ILogger<TeamsService> _logger;
    private readonly HttpClient _httpClient;

    public TeamsService(
        IOptions<TeamsConfiguration> config,
        ILogger<TeamsService> logger,
        HttpClient httpClient)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Teams integration is disabled");
                return false;
            }

            var payload = new
            {
                text = message,
                themeColor = _config.DefaultThemeColor
            };

            return await SendWebhookAsync(webhookUrl, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message to webhook {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> SendAlertAsync(string webhookUrl, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Teams integration is disabled");
                return false;
            }

            var themeColor = GetAlertThemeColor(alert.Severity);
            var activityImage = GetAlertActivityImage(alert.Severity);

            var card = new TeamsAdaptiveCard
            {
                Body = new List<object>
                {
                    new
                    {
                        type = "TextBlock",
                        text = "🚨 Alert Triggered",
                        weight = "Bolder",
                        size = "Medium",
                        color = "Attention"
                    },
                    new
                    {
                        type = "TextBlock",
                        text = alert.Subject,
                        weight = "Bolder",
                        size = "Large",
                        wrap = true
                    },
                    new
                    {
                        type = "TextBlock",
                        text = alert.Description,
                        wrap = true,
                        spacing = "Medium"
                    },
                    new
                    {
                        type = "FactSet",
                        facts = new[]
                        {
                            new { title = "KPI", value = alert.Indicator },
                            new { title = "Owner", value = alert.Owner },
                            new { title = "Severity", value = alert.Severity },
                            new { title = "Priority", value = alert.Priority.ToString() },
                            new { title = "Current Value", value = alert.CurrentValue.ToString("N2") },
                            new { title = "Historical Value", value = alert.HistoricalValue.ToString("N2") },
                            new { title = "Deviation", value = $"{alert.Deviation:N2}%" },
                            new { title = "Triggered", value = alert.TriggerTime.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                        }
                    }
                },
                Actions = new List<object>
                {
                    new
                    {
                        type = "Action.OpenUrl",
                        title = "View Details",
                        url = $"{_config.BaseUrl}/kpi/{alert.KpiId}"
                    },
                    new
                    {
                        type = "Action.OpenUrl",
                        title = "View Dashboard",
                        url = $"{_config.BaseUrl}/dashboard"
                    }
                }
            };

            return await SendAdaptiveCardAsync(webhookUrl, card, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams alert to webhook {WebhookUrl} for KPI {KpiId}", webhookUrl, alert.KpiId);
            return false;
        }
    }

    public async Task<bool> SendRichMessageAsync(string webhookUrl, object message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Teams integration is disabled");
                return false;
            }

            // Handle different message types
            if (message is TeamsAdaptiveCard card)
            {
                return await SendAdaptiveCardAsync(webhookUrl, card, cancellationToken);
            }
            else
            {
                // For generic objects, send as webhook payload
                return await SendWebhookAsync(webhookUrl, message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams rich message to webhook {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> ValidateWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Teams integration is disabled");
                return false;
            }

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                _logger.LogError("Teams webhook URL is empty");
                return false;
            }

            // Test webhook with a simple message
            var testPayload = new
            {
                text = "Webhook validation test from Monitoring Grid",
                themeColor = "00FF00"
            };

            var result = await SendWebhookAsync(webhookUrl, testPayload, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Teams webhook {WebhookUrl} is valid", webhookUrl);
                return true;
            }
            else
            {
                _logger.LogError("Teams webhook {WebhookUrl} validation failed", webhookUrl);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Teams webhook {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> SendAdaptiveCardAsync(string webhookUrl, TeamsAdaptiveCard card, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogDebug("Teams integration is disabled");
                return false;
            }

            var payload = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = card
                    }
                }
            };

            return await SendWebhookAsync(webhookUrl, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams adaptive card to webhook {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                _logger.LogWarning("Teams integration is disabled");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.DefaultWebhookUrl))
            {
                _logger.LogError("Teams default webhook URL is not configured");
                return false;
            }

            // Test webhook with a simple message
            var testPayload = new
            {
                text = "Configuration test from Monitoring Grid",
                themeColor = "00FF00"
            };

            var result = await SendWebhookAsync(_config.DefaultWebhookUrl, testPayload, cancellationToken);
            
            if (result)
            {
                _logger.LogInformation("Teams configuration is valid");
                return true;
            }
            else
            {
                _logger.LogError("Teams configuration validation failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Teams configuration");
            return false;
        }
    }

    private async Task<bool> SendWebhookAsync(string webhookUrl, object payload, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams webhook message sent successfully");
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send Teams webhook message. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams webhook message");
            return false;
        }
    }

    private string GetAlertThemeColor(string severity)
    {
        return severity.ToLower() switch
        {
            "emergency" => "FF0000", // Red
            "critical" => "FF4500",  // Orange Red
            "high" => "FFA500",      // Orange
            "medium" => "FFFF00",    // Yellow
            "low" => "00FF00",       // Green
            _ => "808080"            // Gray
        };
    }

    private string GetAlertActivityImage(string severity)
    {
        return severity.ToLower() switch
        {
            "emergency" => "https://img.icons8.com/color/48/000000/error.png",
            "critical" => "https://img.icons8.com/color/48/000000/high-priority.png",
            "high" => "https://img.icons8.com/color/48/000000/warning-shield.png",
            "medium" => "https://img.icons8.com/color/48/000000/info.png",
            "low" => "https://img.icons8.com/color/48/000000/checkmark.png",
            _ => "https://img.icons8.com/color/48/000000/analytics.png"
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
/// Teams configuration
/// </summary>
public class TeamsConfiguration
{
    public bool IsEnabled { get; set; } = false;
    public string DefaultWebhookUrl { get; set; } = string.Empty;
    public string DefaultThemeColor { get; set; } = "0078D4";
    public string BaseUrl { get; set; } = "https://localhost:5173";
    public List<string> AlertWebhooks { get; set; } = new();
    public Dictionary<string, string> ChannelMappings { get; set; } = new();
    public bool EnableAdaptiveCards { get; set; } = true;
}
