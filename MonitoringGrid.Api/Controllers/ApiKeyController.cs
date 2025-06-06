using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Authentication;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Observability;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing API keys
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyController> _logger;

    public ApiKeyController(
        IApiKeyService apiKeyService,
        ILogger<ApiKeyController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all API keys (admin only)
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "owner" })]
    public async Task<ActionResult<IEnumerable<ApiKeyDto>>> GetApiKeys([FromQuery] string? owner = null)
    {
        try
        {
            var apiKeys = await _apiKeyService.GetApiKeysAsync(owner);
            var dtos = apiKeys.Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} API keys for owner: {Owner}", dtos.Count, owner ?? "all");

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            return StatusCode(500, "An error occurred while retrieving API keys");
        }
    }

    /// <summary>
    /// Get specific API key by ID
    /// </summary>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
    public async Task<ActionResult<ApiKeyDto>> GetApiKey(Guid id)
    {
        try
        {
            var apiKey = await _apiKeyService.GetApiKeyAsync(id);
            if (apiKey == null)
            {
                return NotFound($"API key with ID {id} not found");
            }

            var dto = MapToDto(apiKey);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key {ApiKeyId}", id);
            return StatusCode(500, "An error occurred while retrieving the API key");
        }
    }

    /// <summary>
    /// Create a new API key
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedDto>> CreateApiKey([FromBody] CreateApiKeyDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createRequest = new CreateApiKeyRequest
            {
                Name = request.Name,
                Owner = request.Owner,
                Scopes = request.Scopes,
                ExpiresAt = request.ExpiresAt
            };

            var apiKey = await _apiKeyService.CreateApiKeyAsync(createRequest);

            var result = new ApiKeyCreatedDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = apiKey.Key, // Only returned once during creation
                Owner = apiKey.Owner,
                Scopes = apiKey.Scopes.ToList(),
                IsActive = apiKey.IsActive,
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt
            };

            _logger.LogSecurityEvent("ApiKeyCreated", User.Identity?.Name, 
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                new Dictionary<string, object>
                {
                    ["ApiKeyId"] = apiKey.Id,
                    ["ApiKeyName"] = apiKey.Name,
                    ["Owner"] = apiKey.Owner,
                    ["Scopes"] = string.Join(",", apiKey.Scopes)
                });

            return CreatedAtAction(nameof(GetApiKey), new { id = apiKey.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, "An error occurred while creating the API key");
        }
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeApiKey(Guid id)
    {
        try
        {
            var apiKey = await _apiKeyService.GetApiKeyAsync(id);
            if (apiKey == null)
            {
                return NotFound($"API key with ID {id} not found");
            }

            var success = await _apiKeyService.RevokeApiKeyAsync(id);
            if (!success)
            {
                return BadRequest("Failed to revoke API key");
            }

            _logger.LogSecurityEvent("ApiKeyRevoked", User.Identity?.Name, 
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                new Dictionary<string, object>
                {
                    ["ApiKeyId"] = id,
                    ["ApiKeyName"] = apiKey.Name,
                    ["Owner"] = apiKey.Owner
                });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId}", id);
            return StatusCode(500, "An error occurred while revoking the API key");
        }
    }

    /// <summary>
    /// Get API key usage statistics
    /// </summary>
    [HttpGet("statistics")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new string[] { })]
    public async Task<ActionResult<ApiKeyStatisticsDto>> GetApiKeyStatistics()
    {
        try
        {
            var apiKeys = await _apiKeyService.GetApiKeysAsync();
            var keyList = apiKeys.ToList();

            var statistics = new ApiKeyStatisticsDto
            {
                TotalKeys = keyList.Count,
                ActiveKeys = keyList.Count(k => k.IsActive),
                InactiveKeys = keyList.Count(k => !k.IsActive),
                ExpiredKeys = keyList.Count(k => k.ExpiresAt.HasValue && k.ExpiresAt.Value < DateTime.UtcNow),
                KeysByOwner = keyList.GroupBy(k => k.Owner).ToDictionary(g => g.Key, g => g.Count()),
                RecentlyUsedKeys = keyList.Count(k => k.LastUsedAt.HasValue && k.LastUsedAt.Value > DateTime.UtcNow.AddDays(-7)),
                NeverUsedKeys = keyList.Count(k => !k.LastUsedAt.HasValue)
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key statistics");
            return StatusCode(500, "An error occurred while retrieving API key statistics");
        }
    }

    /// <summary>
    /// Test API key authentication (for the current key)
    /// </summary>
    [HttpGet("test")]
    [AllowAnonymous]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    public ActionResult<ApiKeyTestResultDto> TestApiKey()
    {
        var result = new ApiKeyTestResultDto
        {
            IsValid = User.Identity?.IsAuthenticated == true,
            KeyName = User.FindFirst(ClaimTypes.Name)?.Value,
            Owner = User.FindFirst("api_key_owner")?.Value,
            Scopes = User.FindFirst("api_key_scopes")?.Value?.Split(',').ToList() ?? new List<string>(),
            AuthenticationMethod = User.FindFirst("authentication_method")?.Value,
            TestTime = DateTime.UtcNow
        };

        if (result.IsValid)
        {
            _logger.LogInformation("API key test successful for {KeyName} owned by {Owner}", 
                result.KeyName, result.Owner);
        }
        else
        {
            _logger.LogWarning("API key test failed - no valid authentication found");
        }

        return Ok(result);
    }

    private static ApiKeyDto MapToDto(ApiKeyInfo apiKey)
    {
        return new ApiKeyDto
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Owner = apiKey.Owner,
            Scopes = apiKey.Scopes.ToList(),
            IsActive = apiKey.IsActive,
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt
        };
    }
}

/// <summary>
/// DTO for API key information (without the actual key)
/// </summary>
public class ApiKeyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// DTO for creating a new API key
/// </summary>
public class CreateApiKeyDto
{
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// DTO returned when an API key is created (includes the actual key)
/// </summary>
public class ApiKeyCreatedDto : ApiKeyDto
{
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// DTO for API key statistics
/// </summary>
public class ApiKeyStatisticsDto
{
    public int TotalKeys { get; set; }
    public int ActiveKeys { get; set; }
    public int InactiveKeys { get; set; }
    public int ExpiredKeys { get; set; }
    public int RecentlyUsedKeys { get; set; }
    public int NeverUsedKeys { get; set; }
    public Dictionary<string, int> KeysByOwner { get; set; } = new();
}

/// <summary>
/// DTO for API key test result
/// </summary>
public class ApiKeyTestResultDto
{
    public bool IsValid { get; set; }
    public string? KeyName { get; set; }
    public string? Owner { get; set; }
    public List<string> Scopes { get; set; } = new();
    public string? AuthenticationMethod { get; set; }
    public DateTime TestTime { get; set; }
}
