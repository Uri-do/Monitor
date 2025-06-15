using FluentAssertions;
using MonitoringGrid.Core.Interfaces;
using Xunit;

namespace MonitoringGrid.Infrastructure.Tests.Services;

/// <summary>
/// Integration tests for Cache Service implementation
/// </summary>
public class CacheServiceIntegrationTests : TestBase
{
    [Fact]
    public async Task CacheService_Should_Store_And_Retrieve_Values()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrievedValue = await cacheService.GetAsync<string>(key);

        // Assert
        retrievedValue.Should().Be(value);
    }

    [Fact]
    public async Task CacheService_Should_Return_Null_For_Missing_Key()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        const string key = "non-existent-key";

        // Act
        var retrievedValue = await cacheService.GetAsync<string>(key);

        // Assert
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public async Task CacheService_Should_Support_Complex_Objects()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        const string key = "complex-object";
        var complexObject = new TestComplexObject
        {
            Id = 123,
            Name = "Test Object",
            CreatedAt = DateTime.UtcNow,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Metadata = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 42,
                ["key3"] = true
            }
        };

        // Act
        await cacheService.SetAsync(key, complexObject, TimeSpan.FromMinutes(10));
        var retrievedObject = await cacheService.GetAsync<TestComplexObject>(key);

        // Assert
        retrievedObject.Should().NotBeNull();
        retrievedObject!.Id.Should().Be(complexObject.Id);
        retrievedObject.Name.Should().Be(complexObject.Name);
        retrievedObject.Tags.Should().BeEquivalentTo(complexObject.Tags);
        retrievedObject.Metadata.Should().BeEquivalentTo(complexObject.Metadata);
    }

    [Fact]
    public async Task CacheService_Should_Remove_Items()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        const string key = "removable-key";
        const string value = "removable-value";

        // Act
        await cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var beforeRemoval = await cacheService.GetAsync<string>(key);
        
        await cacheService.RemoveAsync(key);
        var afterRemoval = await cacheService.GetAsync<string>(key);

        // Assert
        beforeRemoval.Should().Be(value);
        afterRemoval.Should().BeNull();
    }

    [Fact]
    public async Task CacheService_Should_Support_GetOrSet_Pattern()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        const string key = "get-or-set-key";
        var callCount = 0;

        // Factory function that tracks calls
        Task<string> Factory()
        {
            callCount++;
            return Task.FromResult($"generated-value-{callCount}");
        }

        // Act
        var firstCall = await cacheService.GetOrSetAsync(key, Factory, TimeSpan.FromMinutes(5));
        var secondCall = await cacheService.GetOrSetAsync(key, Factory, TimeSpan.FromMinutes(5));

        // Assert
        firstCall.Should().Be("generated-value-1");
        secondCall.Should().Be("generated-value-1"); // Should be cached
        callCount.Should().Be(1); // Factory should only be called once
    }

    [Fact]
    public async Task CacheService_Should_Clear_All_Items()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        
        // Add multiple items
        await cacheService.SetAsync("key1", "value1", TimeSpan.FromMinutes(5));
        await cacheService.SetAsync("key2", "value2", TimeSpan.FromMinutes(5));
        await cacheService.SetAsync("key3", "value3", TimeSpan.FromMinutes(5));

        // Act
        await cacheService.ClearAsync();

        // Assert
        var value1 = await cacheService.GetAsync<string>("key1");
        var value2 = await cacheService.GetAsync<string>("key2");
        var value3 = await cacheService.GetAsync<string>("key3");

        value1.Should().BeNull();
        value2.Should().BeNull();
        value3.Should().BeNull();
    }

    [Fact]
    public async Task CacheService_Should_Remove_By_Prefix()
    {
        // Arrange
        var cacheService = GetService<ICacheService>();
        
        // Add items with different prefixes
        await cacheService.SetAsync("user:1", "user1", TimeSpan.FromMinutes(5));
        await cacheService.SetAsync("user:2", "user2", TimeSpan.FromMinutes(5));
        await cacheService.SetAsync("product:1", "product1", TimeSpan.FromMinutes(5));
        await cacheService.SetAsync("product:2", "product2", TimeSpan.FromMinutes(5));

        // Act
        await cacheService.RemoveByPrefixAsync("user:");

        // Assert
        var user1 = await cacheService.GetAsync<string>("user:1");
        var user2 = await cacheService.GetAsync<string>("user:2");
        var product1 = await cacheService.GetAsync<string>("product:1");
        var product2 = await cacheService.GetAsync<string>("product:2");

        user1.Should().BeNull();
        user2.Should().BeNull();
        product1.Should().Be("product1"); // Should still exist
        product2.Should().Be("product2"); // Should still exist
    }

    private class TestComplexObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
