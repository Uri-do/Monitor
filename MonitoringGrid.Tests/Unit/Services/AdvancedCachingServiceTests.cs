using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Services;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.Unit.Services;

/// <summary>
/// Unit tests for AdvancedCachingService
/// </summary>
public class AdvancedCachingServiceTests : IDisposable
{
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly Mock<ILogger<AdvancedCachingService>> _loggerMock;
    private readonly Mock<ICorrelationIdService> _correlationIdServiceMock;
    private readonly AdvancedCachingService _cachingService;
    private readonly ITestOutputHelper _output;

    public AdvancedCachingServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _memoryCacheMock = new Mock<IMemoryCache>();
        _distributedCacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<AdvancedCachingService>>();
        _correlationIdServiceMock = new Mock<ICorrelationIdService>();

        _correlationIdServiceMock.Setup(x => x.GetCorrelationId())
            .Returns("test-correlation-id");

        _cachingService = new AdvancedCachingService(
            _memoryCacheMock.Object,
            _distributedCacheMock.Object,
            _loggerMock.Object,
            _correlationIdServiceMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenMemoryCacheHit_ShouldReturnFromMemoryCache()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new TestData { Id = 1, Name = "Test" };
        
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = expectedValue;
                return true;
            });

        // Act
        var result = await _cachingService.GetAsync<TestData>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue.Id, result.Id);
        Assert.Equal(expectedValue.Name, result.Name);
        
        // Verify memory cache was checked but distributed cache was not
        _memoryCacheMock.Verify(x => x.TryGetValue(key, out It.Ref<object>.IsAny), Times.Once);
        _distributedCacheMock.Verify(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        
        _output.WriteLine("Memory cache hit test passed");
    }

    [Fact]
    public async Task GetAsync_WhenMemoryCacheMissButDistributedCacheHit_ShouldReturnFromDistributedCache()
    {
        // Arrange
        var key = "test-key";
        var testData = new TestData { Id = 2, Name = "Distributed Test" };
        var serializedData = JsonSerializer.Serialize(testData);
        
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out It.Ref<object>.IsAny))
            .Returns(false);
        
        _distributedCacheMock.Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedData);

        // Act
        var result = await _cachingService.GetAsync<TestData>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
        
        // Verify both caches were checked
        _memoryCacheMock.Verify(x => x.TryGetValue(key, out It.Ref<object>.IsAny), Times.Once);
        _distributedCacheMock.Verify(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify item was stored in memory cache for faster access
        _memoryCacheMock.Verify(x => x.Set(key, result, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        
        _output.WriteLine("Distributed cache hit test passed");
    }

    [Fact]
    public async Task GetAsync_WhenBothCachesMiss_ShouldReturnNull()
    {
        // Arrange
        var key = "nonexistent-key";
        
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out It.Ref<object>.IsAny))
            .Returns(false);
        
        _distributedCacheMock.Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _cachingService.GetAsync<TestData>(key);

        // Assert
        Assert.Null(result);
        
        _output.WriteLine("Cache miss test passed");
    }

    [Fact]
    public async Task SetAsync_ShouldStoreInBothCaches()
    {
        // Arrange
        var key = "test-key";
        var value = new TestData { Id = 3, Name = "Set Test" };
        var expiration = TimeSpan.FromMinutes(10);

        // Act
        await _cachingService.SetAsync(key, value, expiration);

        // Assert
        // Verify memory cache was set
        _memoryCacheMock.Verify(x => x.Set(key, value, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        
        // Verify distributed cache was set
        _distributedCacheMock.Verify(x => x.SetStringAsync(
            key, 
            It.IsAny<string>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        _output.WriteLine("Set cache test passed");
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test-key";
        var cachedValue = new TestData { Id = 4, Name = "Cached" };
        var factoryCallCount = 0;
        
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = cachedValue;
                return true;
            });

        // Act
        var result = await _cachingService.GetOrSetAsync(key, () =>
        {
            factoryCallCount++;
            return Task.FromResult(new TestData { Id = 999, Name = "Factory" });
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedValue.Id, result.Id);
        Assert.Equal(0, factoryCallCount); // Factory should not be called
        
        _output.WriteLine("GetOrSet cache hit test passed");
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_ShouldCallFactoryAndCache()
    {
        // Arrange
        var key = "test-key";
        var factoryValue = new TestData { Id = 5, Name = "Factory Value" };
        var factoryCallCount = 0;
        
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out It.Ref<object>.IsAny))
            .Returns(false);
        
        _distributedCacheMock.Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _cachingService.GetOrSetAsync(key, () =>
        {
            factoryCallCount++;
            return Task.FromResult(factoryValue);
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(factoryValue.Id, result.Id);
        Assert.Equal(1, factoryCallCount); // Factory should be called once
        
        // Verify value was cached
        _memoryCacheMock.Verify(x => x.Set(key, factoryValue, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        
        _output.WriteLine("GetOrSet cache miss test passed");
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromBothCaches()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _cachingService.RemoveAsync(key);

        // Assert
        _memoryCacheMock.Verify(x => x.Remove(key), Times.Once);
        _distributedCacheMock.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        
        _output.WriteLine("Remove cache test passed");
    }

    [Fact]
    public void GenerateCacheKey_WithParameters_ShouldCreateConsistentKey()
    {
        // Arrange
        var prefix = "test";
        var param1 = "value1";
        var param2 = 123;
        var param3 = true;

        // Act
        var key1 = _cachingService.GenerateCacheKey(prefix, param1, param2, param3);
        var key2 = _cachingService.GenerateCacheKey(prefix, param1, param2, param3);

        // Assert
        Assert.Equal(key1, key2);
        Assert.StartsWith(prefix, key1);
        Assert.Contains(param1, key1);
        Assert.Contains(param2.ToString(), key1);
        Assert.Contains(param3.ToString(), key1);
        
        _output.WriteLine($"Generated cache key: {key1}");
    }

    [Fact]
    public void GenerateCacheKey_WithLongKey_ShouldHashKey()
    {
        // Arrange
        var prefix = "test";
        var longParam = new string('x', 200); // Create a long parameter

        // Act
        var key = _cachingService.GenerateCacheKey(prefix, longParam);

        // Assert
        Assert.StartsWith(prefix, key);
        Assert.Contains("hash", key);
        Assert.True(key.Length < 250); // Should be shorter than the original
        
        _output.WriteLine($"Hashed cache key: {key}");
    }

    [Fact]
    public void GetStatistics_ShouldReturnCacheStatistics()
    {
        // Act
        var statistics = _cachingService.GetStatistics();

        // Assert
        Assert.NotNull(statistics);
        Assert.True(statistics.TotalOperations >= 0);
        Assert.True(statistics.HitRatio >= 0 && statistics.HitRatio <= 1);
        
        _output.WriteLine($"Cache statistics: Hits={statistics.TotalHits}, Misses={statistics.Misses}, Ratio={statistics.HitRatio:P2}");
    }

    [Fact]
    public async Task CachingService_UnderException_ShouldHandleGracefully()
    {
        // Arrange
        var key = "test-key";
        
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out It.Ref<object>.IsAny))
            .Throws(new InvalidOperationException("Memory cache error"));

        // Act & Assert - Should not throw
        var result = await _cachingService.GetAsync<TestData>(key);
        Assert.Null(result);
        
        _output.WriteLine("Exception handling test passed");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    /// <summary>
    /// Test data class for caching tests
    /// </summary>
    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
