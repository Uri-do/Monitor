using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Middleware;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Response optimization service for compression, ETags, and streaming
/// </summary>
public interface IResponseOptimizationService
{
    /// <summary>
    /// Generates ETag for response caching
    /// </summary>
    string GenerateETag(object data);

    /// <summary>
    /// Checks if client has current version based on ETag
    /// </summary>
    bool IsClientCacheValid(string clientETag, string currentETag);

    /// <summary>
    /// Compresses response data
    /// </summary>
    Task<byte[]> CompressResponseAsync(string data, CompressionLevel compressionLevel = CompressionLevel.Optimal);

    /// <summary>
    /// Creates a streaming response for large datasets
    /// </summary>
    Task<IActionResult> CreateStreamingResponseAsync<T>(IAsyncEnumerable<T> data, string contentType = "application/json");

    /// <summary>
    /// Optimizes response based on client capabilities
    /// </summary>
    Task<OptimizedResponse> OptimizeResponseAsync(object data, HttpContext context);

    /// <summary>
    /// Creates a partial response based on requested fields
    /// </summary>
    object CreatePartialResponse(object data, string[]? fields);

    /// <summary>
    /// Gets response optimization metrics
    /// </summary>
    ResponseOptimizationMetrics GetMetrics();
}

/// <summary>
/// Implementation of response optimization service
/// </summary>
public class ResponseOptimizationService : IResponseOptimizationService
{
    private readonly ILogger<ResponseOptimizationService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ResponseOptimizationMetrics _metrics;
    private readonly JsonSerializerOptions _jsonOptions;

    public ResponseOptimizationService(
        ILogger<ResponseOptimizationService> logger,
        ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _metrics = new ResponseOptimizationMetrics();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Generates strong ETag based on data content
    /// </summary>
    public string GenerateETag(object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            var hash = Convert.ToBase64String(hashBytes);
            
            _metrics.RecordETagGeneration();
            return $"\"{hash}\"";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate ETag");
            return $"\"{Guid.NewGuid()}\"";
        }
    }

    /// <summary>
    /// Validates client cache using ETag comparison
    /// </summary>
    public bool IsClientCacheValid(string clientETag, string currentETag)
    {
        if (string.IsNullOrEmpty(clientETag) || string.IsNullOrEmpty(currentETag))
            return false;

        var isValid = string.Equals(clientETag.Trim('"'), currentETag.Trim('"'), StringComparison.Ordinal);
        
        if (isValid)
            _metrics.RecordCacheHit();
        else
            _metrics.RecordCacheMiss();

        return isValid;
    }

    /// <summary>
    /// Compresses response data using Gzip
    /// </summary>
    public async Task<byte[]> CompressResponseAsync(string data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var originalSize = Encoding.UTF8.GetByteCount(data);

        try
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, compressionLevel))
            using (var writer = new StreamWriter(gzip, Encoding.UTF8))
            {
                await writer.WriteAsync(data);
            }

            var compressedData = output.ToArray();
            var compressedSize = compressedData.Length;
            var compressionRatio = (double)compressedSize / originalSize;

            _metrics.RecordCompression(originalSize, compressedSize);

            _logger.LogDebug("Response compressed: {OriginalSize} -> {CompressedSize} bytes ({CompressionRatio:P1}) [{CorrelationId}]",
                originalSize, compressedSize, compressionRatio, correlationId);

            return compressedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response compression failed [{CorrelationId}]", correlationId);
            return Encoding.UTF8.GetBytes(data);
        }
    }

    /// <summary>
    /// Creates streaming response for large datasets
    /// </summary>
    public async Task<IActionResult> CreateStreamingResponseAsync<T>(IAsyncEnumerable<T> data, string contentType = "application/json")
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Creating streaming response [{CorrelationId}]", correlationId);

            return new StreamingJsonResult<T>(data, _jsonOptions, _logger, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streaming response [{CorrelationId}]", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Optimizes response based on client capabilities and preferences
    /// </summary>
    public async Task<OptimizedResponse> OptimizeResponseAsync(object data, HttpContext context)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var optimizedResponse = new OptimizedResponse { OriginalData = data };

        try
        {
            // Generate ETag
            optimizedResponse.ETag = GenerateETag(data);

            // Check if client has current version
            var clientETag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            if (!string.IsNullOrEmpty(clientETag) && IsClientCacheValid(clientETag, optimizedResponse.ETag))
            {
                optimizedResponse.IsNotModified = true;
                _logger.LogDebug("Client cache is valid, returning 304 Not Modified [{CorrelationId}]", correlationId);
                return optimizedResponse;
            }

            // Handle partial responses (field selection)
            var fields = context.Request.Query["fields"].FirstOrDefault()?.Split(',');
            if (fields?.Length > 0)
            {
                optimizedResponse.Data = CreatePartialResponse(data, fields);
                _metrics.RecordPartialResponse();
            }
            else
            {
                optimizedResponse.Data = data;
            }

            // Check if compression is supported and beneficial
            var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
            if (acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            {
                var json = JsonSerializer.Serialize(optimizedResponse.Data, _jsonOptions);
                if (json.Length > 1024) // Only compress if larger than 1KB
                {
                    optimizedResponse.CompressedData = await CompressResponseAsync(json);
                    optimizedResponse.IsCompressed = true;
                }
            }

            _logger.LogDebug("Response optimized [{CorrelationId}]", correlationId);
            return optimizedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response optimization failed [{CorrelationId}]", correlationId);
            optimizedResponse.Data = data;
            return optimizedResponse;
        }
    }

    /// <summary>
    /// Creates partial response with only requested fields
    /// </summary>
    public object CreatePartialResponse(object data, string[]? fields)
    {
        if (fields == null || fields.Length == 0)
            return data;

        try
        {
            // Convert to JSON and back to filter fields
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            using var document = JsonDocument.Parse(json);
            
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return FilterArrayFields(document.RootElement, fields);
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return FilterObjectFields(document.RootElement, fields);
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create partial response, returning full data");
            return data;
        }
    }

    /// <summary>
    /// Gets response optimization performance metrics
    /// </summary>
    public ResponseOptimizationMetrics GetMetrics()
    {
        return _metrics.Clone();
    }

    /// <summary>
    /// Filters object fields based on requested field list
    /// </summary>
    private Dictionary<string, object?> FilterObjectFields(JsonElement element, string[] fields)
    {
        var result = new Dictionary<string, object?>();
        var fieldSet = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            if (fieldSet.Contains(property.Name))
            {
                result[property.Name] = ExtractJsonValue(property.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Filters array fields based on requested field list
    /// </summary>
    private List<Dictionary<string, object?>> FilterArrayFields(JsonElement element, string[] fields)
    {
        var result = new List<Dictionary<string, object?>>();

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                result.Add(FilterObjectFields(item, fields));
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts value from JsonElement
    /// </summary>
    private object? ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value)),
            _ => element.ToString()
        };
    }
}

/// <summary>
/// Optimized response container
/// </summary>
public class OptimizedResponse
{
    public object? OriginalData { get; set; }
    public object? Data { get; set; }
    public string ETag { get; set; } = string.Empty;
    public bool IsNotModified { get; set; }
    public bool IsCompressed { get; set; }
    public byte[]? CompressedData { get; set; }
}

/// <summary>
/// Streaming JSON result for large datasets
/// </summary>
public class StreamingJsonResult<T> : IActionResult
{
    private readonly IAsyncEnumerable<T> _data;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger _logger;
    private readonly string _correlationId;

    public StreamingJsonResult(IAsyncEnumerable<T> data, JsonSerializerOptions jsonOptions, ILogger logger, string correlationId)
    {
        _data = data;
        _jsonOptions = jsonOptions;
        _logger = logger;
        _correlationId = correlationId;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";

        try
        {
            await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync('[');

            var isFirst = true;
            await foreach (var item in _data)
            {
                if (!isFirst)
                    await writer.WriteAsync(',');

                var json = JsonSerializer.Serialize(item, _jsonOptions);
                await writer.WriteAsync(json);
                await writer.FlushAsync();

                isFirst = false;
            }

            await writer.WriteAsync(']');
            await writer.FlushAsync();

            _logger.LogDebug("Streaming response completed [{CorrelationId}]", _correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming response failed [{CorrelationId}]", _correlationId);
            throw;
        }
    }
}

/// <summary>
/// Response optimization metrics
/// </summary>
public class ResponseOptimizationMetrics
{
    private long _etagGenerations;
    private long _cacheHits;
    private long _cacheMisses;
    private long _compressions;
    private long _totalOriginalBytes;
    private long _totalCompressedBytes;
    private long _partialResponses;

    public long ETagGenerations => _etagGenerations;
    public long CacheHits => _cacheHits;
    public long CacheMisses => _cacheMisses;
    public long Compressions => _compressions;
    public long TotalOriginalBytes => _totalOriginalBytes;
    public long TotalCompressedBytes => _totalCompressedBytes;
    public long PartialResponses => _partialResponses;
    
    public double CacheHitRatio => (_cacheHits + _cacheMisses) > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0;
    public double CompressionRatio => _totalOriginalBytes > 0 ? (double)_totalCompressedBytes / _totalOriginalBytes : 0;
    public long BytesSaved => _totalOriginalBytes - _totalCompressedBytes;

    public void RecordETagGeneration() => Interlocked.Increment(ref _etagGenerations);
    public void RecordCacheHit() => Interlocked.Increment(ref _cacheHits);
    public void RecordCacheMiss() => Interlocked.Increment(ref _cacheMisses);
    public void RecordPartialResponse() => Interlocked.Increment(ref _partialResponses);

    public void RecordCompression(long originalBytes, long compressedBytes)
    {
        Interlocked.Increment(ref _compressions);
        Interlocked.Add(ref _totalOriginalBytes, originalBytes);
        Interlocked.Add(ref _totalCompressedBytes, compressedBytes);
    }

    public ResponseOptimizationMetrics Clone()
    {
        return new ResponseOptimizationMetrics
        {
            _etagGenerations = _etagGenerations,
            _cacheHits = _cacheHits,
            _cacheMisses = _cacheMisses,
            _compressions = _compressions,
            _totalOriginalBytes = _totalOriginalBytes,
            _totalCompressedBytes = _totalCompressedBytes,
            _partialResponses = _partialResponses
        };
    }
}
