using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Webhook integration service for sending HTTP notifications
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<WebhookService> _logger;
    private readonly HttpClient _httpClient;

    public WebhookService(
        MonitoringContext context,
        ILogger<WebhookService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook sent successfully to {Url}", url);
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send webhook to {Url}. Status: {StatusCode}, Response: {Response}",
                    url, response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", url);
            return false;
        }
    }

    public async Task<bool> SendWebhookAsync(string url, object payload, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add custom headers
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook sent successfully to {Url} with custom headers", url);
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send webhook to {Url} with custom headers. Status: {StatusCode}, Response: {Response}",
                    url, response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url} with custom headers", url);
            return false;
        }
    }

    public async Task<bool> ValidateWebhookAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogError("Webhook URL is empty");
                return false;
            }

            // Test webhook with a simple message
            var testPayload = new
            {
                eventType = "webhook.validation",
                timestamp = DateTime.UtcNow,
                message = "Webhook validation test from Monitoring Grid"
            };

            var result = await SendWebhookAsync(url, testPayload, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Webhook {Url} is valid", url);
                return true;
            }
            else
            {
                _logger.LogError("Webhook {Url} validation failed", url);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate webhook {Url}", url);
            return false;
        }
    }

    public async Task<bool> SendWebhookAsync(WebhookConfiguration webhook, object payload, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var deliveryLog = new WebhookDeliveryLog
        {
            WebhookId = webhook.WebhookId,
            DeliveryTime = DateTime.UtcNow
        };

        try
        {
            if (!webhook.IsActive)
            {
                _logger.LogDebug("Webhook {WebhookId} is disabled", webhook.WebhookId);
                return false;
            }

            _logger.LogDebug("Sending webhook {WebhookId} to {Url}", webhook.WebhookId, webhook.Url);

            // Prepare the request
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(new HttpMethod(webhook.HttpMethod), webhook.Url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add custom headers
            foreach (var header in webhook.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Set timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(webhook.TimeoutSeconds));

            // Send the request
            var response = await _httpClient.SendAsync(request, cts.Token);
            stopwatch.Stop();

            deliveryLog.StatusCode = (int)response.StatusCode;
            deliveryLog.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            deliveryLog.IsSuccess = response.IsSuccessStatusCode;

            if (response.IsSuccessStatusCode)
            {
                deliveryLog.Response = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Webhook {WebhookId} sent successfully. Status: {StatusCode}, Time: {ResponseTime}ms", 
                    webhook.WebhookId, response.StatusCode, deliveryLog.ResponseTimeMs);
                
                await LogDeliveryAsync(deliveryLog, cancellationToken);
                return true;
            }
            else
            {
                deliveryLog.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                deliveryLog.Response = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.LogError("Webhook {WebhookId} failed. Status: {StatusCode}, Response: {Response}", 
                    webhook.WebhookId, response.StatusCode, deliveryLog.Response);
                
                await LogDeliveryAsync(deliveryLog, cancellationToken);
                
                // Retry if configured
                if (webhook.RetryCount > 0)
                {
                    return await RetryWebhookAsync(webhook, payload, 1, cancellationToken);
                }
                
                return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            deliveryLog.IsSuccess = false;
            deliveryLog.ErrorMessage = "Request cancelled";
            deliveryLog.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            
            _logger.LogWarning("Webhook {WebhookId} request was cancelled", webhook.WebhookId);
            await LogDeliveryAsync(deliveryLog, cancellationToken);
            return false;
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            deliveryLog.IsSuccess = false;
            deliveryLog.ErrorMessage = "Request timeout";
            deliveryLog.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            
            _logger.LogError("Webhook {WebhookId} request timed out after {Timeout}s", webhook.WebhookId, webhook.TimeoutSeconds);
            await LogDeliveryAsync(deliveryLog, cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            deliveryLog.IsSuccess = false;
            deliveryLog.ErrorMessage = ex.Message;
            deliveryLog.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            
            _logger.LogError(ex, "Failed to send webhook {WebhookId}: {Message}", webhook.WebhookId, ex.Message);
            await LogDeliveryAsync(deliveryLog, cancellationToken);
            return false;
        }
    }

    public async Task<bool> SendAlertWebhookAsync(WebhookConfiguration webhook, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if this webhook should be triggered for this alert severity
            if (webhook.TriggerSeverities.Any() &&
                !webhook.TriggerSeverities.Contains(alert.Severity, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Webhook {WebhookId} not triggered for severity {Severity}", webhook.WebhookId, alert.Severity);
                return false;
            }

            // Build payload from template or use default structure
            object payload;
            if (!string.IsNullOrWhiteSpace(webhook.PayloadTemplate))
            {
                payload = BuildPayloadFromTemplate(webhook.PayloadTemplate, alert);
            }
            else
            {
                payload = new
                {
                    eventType = "alert.triggered",
                    timestamp = alert.TriggerTime,
                    alert = new
                    {
                        id = alert.AlertId,
                        kpiId = alert.KpiId,
                        indicator = alert.Indicator,
                        owner = alert.Owner,
                        severity = alert.Severity,
                        priority = alert.Priority,
                        currentValue = alert.CurrentValue,
                        historicalValue = alert.HistoricalValue,
                        deviation = alert.Deviation,
                        subject = alert.Subject,
                        description = alert.Description,
                        triggerTime = alert.TriggerTime,
                        notifiedContacts = alert.NotifiedContacts
                    }
                };
            }

            return await SendWebhookAsync(webhook, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert webhook {WebhookId} for alert {AlertId}", webhook.WebhookId, alert.AlertId);
            return false;
        }
    }

    public async Task<WebhookTestResult> TestWebhookAsync(WebhookConfiguration webhook, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testResult = new WebhookTestResult
        {
            TestTime = DateTime.UtcNow
        };

        try
        {
            var testPayload = new
            {
                eventType = "webhook.test",
                timestamp = DateTime.UtcNow,
                message = "This is a test webhook from Monitoring Grid",
                webhookId = webhook.WebhookId,
                webhookName = webhook.Name
            };

            var json = JsonSerializer.Serialize(testPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(new HttpMethod(webhook.HttpMethod), webhook.Url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            foreach (var header in webhook.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(webhook.TimeoutSeconds));

            var response = await _httpClient.SendAsync(request, cts.Token);
            stopwatch.Stop();

            testResult.StatusCode = (int)response.StatusCode;
            testResult.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            testResult.IsSuccess = response.IsSuccessStatusCode;
            testResult.Response = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                testResult.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }

            _logger.LogInformation("Webhook test completed for {WebhookId}. Success: {IsSuccess}, Status: {StatusCode}, Time: {ResponseTime}ms", 
                webhook.WebhookId, testResult.IsSuccess, testResult.StatusCode, testResult.ResponseTimeMs);

            return testResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            testResult.IsSuccess = false;
            testResult.ErrorMessage = ex.Message;
            testResult.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;

            _logger.LogError(ex, "Webhook test failed for {WebhookId}: {Message}", webhook.WebhookId, ex.Message);
            return testResult;
        }
    }

    public async Task<List<WebhookDeliveryLog>> GetDeliveryLogsAsync(int webhookId, DateTime? startDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<WebhookDeliveryLog>()
                .Where(log => log.WebhookId == webhookId);

            if (startDate.HasValue)
            {
                query = query.Where(log => log.DeliveryTime >= startDate.Value);
            }

            return await query
                .OrderByDescending(log => log.DeliveryTime)
                .Take(100) // Limit to last 100 logs
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get delivery logs for webhook {WebhookId}", webhookId);
            return new List<WebhookDeliveryLog>();
        }
    }

    private async Task<bool> RetryWebhookAsync(WebhookConfiguration webhook, object payload, int retryAttempt, CancellationToken cancellationToken)
    {
        if (retryAttempt > webhook.RetryCount)
        {
            _logger.LogError("Webhook {WebhookId} failed after {RetryCount} retries", webhook.WebhookId, webhook.RetryCount);
            return false;
        }

        _logger.LogInformation("Retrying webhook {WebhookId}, attempt {RetryAttempt}/{RetryCount}", 
            webhook.WebhookId, retryAttempt, webhook.RetryCount);

        // Exponential backoff: 2^retryAttempt seconds
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        await Task.Delay(delay, cancellationToken);

        var success = await SendWebhookAsync(webhook, payload, cancellationToken);
        if (!success && retryAttempt < webhook.RetryCount)
        {
            return await RetryWebhookAsync(webhook, payload, retryAttempt + 1, cancellationToken);
        }

        return success;
    }

    private async Task LogDeliveryAsync(WebhookDeliveryLog deliveryLog, CancellationToken cancellationToken)
    {
        try
        {
            _context.Set<WebhookDeliveryLog>().Add(deliveryLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log webhook delivery for webhook {WebhookId}", deliveryLog.WebhookId);
        }
    }

    private object BuildPayloadFromTemplate(string template, AlertNotificationDto alert)
    {
        // Simple template replacement - in production, consider using a proper template engine
        var json = template
            .Replace("{{alertId}}", alert.AlertId.ToString())
            .Replace("{{kpiId}}", alert.KpiId.ToString())
            .Replace("{{indicator}}", alert.Indicator)
            .Replace("{{owner}}", alert.Owner)
            .Replace("{{severity}}", alert.Severity)
            .Replace("{{priority}}", alert.Priority.ToString())
            .Replace("{{currentValue}}", alert.CurrentValue.ToString())
            .Replace("{{historicalValue}}", alert.HistoricalValue.ToString())
            .Replace("{{deviation}}", alert.Deviation.ToString())
            .Replace("{{subject}}", alert.Subject)
            .Replace("{{description}}", alert.Description)
            .Replace("{{triggerTime}}", alert.TriggerTime.ToString("O"));

        try
        {
            return JsonSerializer.Deserialize<object>(json) ?? new object();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse webhook payload template");
            return new { error = "Invalid payload template" };
        }
    }
}
