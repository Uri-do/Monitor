using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MonitoringGrid.Api.Observability;

namespace MonitoringGrid.Api.Authentication;

/// <summary>
/// API Key authentication handler for service-to-service authentication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key is provided in header
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var apiKeyInfo = await _apiKeyService.ValidateApiKeyAsync(providedApiKey);
            if (apiKeyInfo == null)
            {
                _logger.LogSecurityEvent("ApiKeyAuthenticationFailed", null, 
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    new Dictionary<string, object> { ["ProvidedKey"] = providedApiKey[..8] + "..." });
                
                return AuthenticateResult.Fail("Invalid API key");
            }

            if (!apiKeyInfo.IsActive)
            {
                _logger.LogSecurityEvent("InactiveApiKeyUsed", apiKeyInfo.Owner, 
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    new Dictionary<string, object> { ["KeyName"] = apiKeyInfo.Name });
                
                return AuthenticateResult.Fail("API key is inactive");
            }

            if (apiKeyInfo.ExpiresAt.HasValue && apiKeyInfo.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogSecurityEvent("ExpiredApiKeyUsed", apiKeyInfo.Owner, 
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    new Dictionary<string, object> { ["KeyName"] = apiKeyInfo.Name, ["ExpiredAt"] = apiKeyInfo.ExpiresAt.Value });
                
                return AuthenticateResult.Fail("API key has expired");
            }

            // Update last used timestamp
            await _apiKeyService.UpdateLastUsedAsync(apiKeyInfo.Id);

            // Create claims for the authenticated API key
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, apiKeyInfo.Name),
                new(ClaimTypes.NameIdentifier, apiKeyInfo.Id.ToString()),
                new("api_key_owner", apiKeyInfo.Owner),
                new("api_key_scopes", string.Join(",", apiKeyInfo.Scopes)),
                new("authentication_method", "api_key")
            };

            // Add scope-based claims
            foreach (var scope in apiKeyInfo.Scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation("API key authentication successful for {KeyName} owned by {Owner}",
                apiKeyInfo.Name, apiKeyInfo.Owner);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API key authentication");
            return AuthenticateResult.Fail("Authentication error occurred");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = $"{Scheme.Name} realm=\"{Options.Realm}\"";
        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration options for API key authentication
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
    public string Realm { get; set; } = "MonitoringGrid API";
}

/// <summary>
/// Service for managing API keys
/// </summary>
public interface IApiKeyService
{
    Task<ApiKeyInfo?> ValidateApiKeyAsync(string apiKey);
    Task<ApiKeyInfo> CreateApiKeyAsync(CreateApiKeyRequest request);
    Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(string? owner = null);
    Task<bool> RevokeApiKeyAsync(Guid keyId);
    Task UpdateLastUsedAsync(Guid keyId);
    Task<ApiKeyInfo?> GetApiKeyAsync(Guid keyId);
}

/// <summary>
/// In-memory API key service (in production, this would use a database)
/// </summary>
public class InMemoryApiKeyService : IApiKeyService
{
    private readonly List<ApiKeyInfo> _apiKeys = new();
    private readonly object _lock = new();

    public InMemoryApiKeyService()
    {
        // Initialize with some default API keys for development
        _apiKeys.AddRange(new[]
        {
            new ApiKeyInfo
            {
                Id = Guid.NewGuid(),
                Name = "Development Key",
                Key = "dev-key-12345678901234567890123456789012",
                Owner = "development",
                Scopes = new[] { "kpi:read", "kpi:execute", "alerts:read" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            },
            new ApiKeyInfo
            {
                Id = Guid.NewGuid(),
                Name = "Admin Key",
                Key = "admin-key-12345678901234567890123456789012",
                Owner = "admin",
                Scopes = new[] { "kpi:*", "alerts:*", "admin:*" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = null // No expiration
            }
        });
    }

    public Task<ApiKeyInfo?> ValidateApiKeyAsync(string apiKey)
    {
        lock (_lock)
        {
            var keyInfo = _apiKeys.FirstOrDefault(k => k.Key == apiKey);
            return Task.FromResult(keyInfo);
        }
    }

    public Task<ApiKeyInfo> CreateApiKeyAsync(CreateApiKeyRequest request)
    {
        lock (_lock)
        {
            var apiKey = new ApiKeyInfo
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Key = GenerateApiKey(),
                Owner = request.Owner,
                Scopes = request.Scopes.ToArray(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt
            };

            _apiKeys.Add(apiKey);
            return Task.FromResult(apiKey);
        }
    }

    public Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(string? owner = null)
    {
        lock (_lock)
        {
            var keys = _apiKeys.AsQueryable();
            
            if (!string.IsNullOrEmpty(owner))
            {
                keys = keys.Where(k => k.Owner == owner);
            }

            // Don't return the actual key value for security
            var result = keys.Select(k => new ApiKeyInfo
            {
                Id = k.Id,
                Name = k.Name,
                Key = "***", // Masked
                Owner = k.Owner,
                Scopes = k.Scopes,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt
            }).ToList();

            return Task.FromResult<IEnumerable<ApiKeyInfo>>(result);
        }
    }

    public Task<bool> RevokeApiKeyAsync(Guid keyId)
    {
        lock (_lock)
        {
            var apiKey = _apiKeys.FirstOrDefault(k => k.Id == keyId);
            if (apiKey != null)
            {
                apiKey.IsActive = false;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    public Task UpdateLastUsedAsync(Guid keyId)
    {
        lock (_lock)
        {
            var apiKey = _apiKeys.FirstOrDefault(k => k.Id == keyId);
            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }
    }

    public Task<ApiKeyInfo?> GetApiKeyAsync(Guid keyId)
    {
        lock (_lock)
        {
            var apiKey = _apiKeys.FirstOrDefault(k => k.Id == keyId);
            return Task.FromResult(apiKey);
        }
    }

    private static string GenerateApiKey()
    {
        return $"mgapi-{Guid.NewGuid():N}";
    }
}

/// <summary>
/// API key information
/// </summary>
public class ApiKeyInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Request to create a new API key
/// </summary>
public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}
