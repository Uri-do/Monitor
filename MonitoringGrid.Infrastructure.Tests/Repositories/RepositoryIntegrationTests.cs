using FluentAssertions;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using Xunit;

namespace MonitoringGrid.Infrastructure.Tests.Repositories;

/// <summary>
/// Integration tests for Repository pattern implementation
/// </summary>
public class RepositoryIntegrationTests : TestBase
{
    [Fact]
    public async Task Repository_Should_Add_And_Retrieve_Entity()
    {
        // Arrange
        var repository = GetService<IRepository<Indicator>>();
        var indicator = new Indicator
        {
            IndicatorName = "Test Indicator",
            IndicatorCode = "TEST_IND",
            IndicatorDesc = "Test Description",
            CollectorID = 1,
            CollectorItemName = "TestItem",
            IsActive = true,
            Priority = "medium",
            ThresholdType = "threshold_value",
            ThresholdField = "Total",
            ThresholdValue = 100,
            ThresholdComparison = "gt",
            LastMinutes = 60,
            OwnerContactId = 1
        };

        // Act
        var addedIndicator = await repository.AddAsync(indicator);
        await repository.SaveChangesAsync();

        // Assert
        addedIndicator.Should().NotBeNull();
        addedIndicator.IndicatorID.Should().BeGreaterThan(0);
        addedIndicator.IndicatorName.Should().Be("Test Indicator");

        // Verify retrieval
        var retrievedIndicator = await repository.GetByIdAsync(addedIndicator.IndicatorID);
        retrievedIndicator.Should().NotBeNull();
        retrievedIndicator!.IndicatorName.Should().Be("Test Indicator");
    }

    [Fact]
    public async Task Repository_Should_Update_Entity()
    {
        // Arrange
        var repository = GetService<IRepository<Indicator>>();
        var indicator = new Indicator
        {
            IndicatorName = "Original Name",
            IndicatorCode = "ORIG_IND",
            IndicatorDesc = "Original Description",
            CollectorID = 1,
            CollectorItemName = "OriginalItem",
            IsActive = true,
            Priority = "low",
            ThresholdType = "threshold_value",
            ThresholdField = "Total",
            ThresholdValue = 50,
            ThresholdComparison = "lt",
            LastMinutes = 30,
            OwnerContactId = 1
        };

        // Act
        await repository.AddAsync(indicator);
        await repository.SaveChangesAsync();

        indicator.IndicatorName = "Updated Name";
        indicator.Priority = "high";
        await repository.UpdateAsync(indicator);
        await repository.SaveChangesAsync();

        // Assert
        var updatedIndicator = await repository.GetByIdAsync(indicator.IndicatorID);
        updatedIndicator.Should().NotBeNull();
        updatedIndicator!.IndicatorName.Should().Be("Updated Name");
        updatedIndicator.Priority.Should().Be("high");
    }

    [Fact]
    public async Task Repository_Should_Delete_Entity()
    {
        // Arrange
        var repository = GetService<IRepository<Indicator>>();
        var indicator = new Indicator
        {
            IndicatorName = "To Be Deleted",
            IndicatorCode = "DEL_IND",
            IndicatorDesc = "Will be deleted",
            CollectorID = 1,
            CollectorItemName = "DeleteItem",
            IsActive = true,
            Priority = "medium",
            ThresholdType = "threshold_value",
            ThresholdField = "Total",
            ThresholdValue = 75,
            ThresholdComparison = "eq",
            LastMinutes = 45,
            OwnerContactId = 1
        };

        // Act
        await repository.AddAsync(indicator);
        await repository.SaveChangesAsync();

        await repository.DeleteAsync(indicator);
        await repository.SaveChangesAsync();

        // Assert
        var deletedIndicator = await repository.GetByIdAsync(indicator.IndicatorID);
        deletedIndicator.Should().BeNull();
    }

    [Fact]
    public async Task Repository_Should_Support_Bulk_Operations()
    {
        // Arrange
        var repository = GetService<IRepository<Indicator>>();
        var indicators = new List<Indicator>
        {
            new() { IndicatorName = "Bulk 1", IndicatorCode = "BULK1", IndicatorDesc = "Bulk Test 1", CollectorID = 1, CollectorItemName = "BulkItem1", IsActive = true, Priority = "low", ThresholdType = "threshold_value", ThresholdField = "Total", ThresholdValue = 10, ThresholdComparison = "gt", LastMinutes = 15, OwnerContactId = 1 },
            new() { IndicatorName = "Bulk 2", IndicatorCode = "BULK2", IndicatorDesc = "Bulk Test 2", CollectorID = 1, CollectorItemName = "BulkItem2", IsActive = true, Priority = "medium", ThresholdType = "threshold_value", ThresholdField = "Total", ThresholdValue = 20, ThresholdComparison = "lt", LastMinutes = 30, OwnerContactId = 1 },
            new() { IndicatorName = "Bulk 3", IndicatorCode = "BULK3", IndicatorDesc = "Bulk Test 3", CollectorID = 1, CollectorItemName = "BulkItem3", IsActive = false, Priority = "high", ThresholdType = "threshold_value", ThresholdField = "Total", ThresholdValue = 30, ThresholdComparison = "gte", LastMinutes = 45, OwnerContactId = 1 }
        };

        // Act
        var insertCount = await repository.BulkInsertAsync(indicators);
        await repository.SaveChangesAsync();

        // Assert
        insertCount.Should().Be(3);
        
        var allIndicators = await repository.GetAllAsync();
        allIndicators.Should().HaveCount(3);
        allIndicators.Should().Contain(i => i.IndicatorName == "Bulk 1");
        allIndicators.Should().Contain(i => i.IndicatorName == "Bulk 2");
        allIndicators.Should().Contain(i => i.IndicatorName == "Bulk 3");
    }

    [Fact]
    public async Task Repository_Should_Support_Filtering()
    {
        // Arrange
        var repository = GetService<IRepository<Indicator>>();
        var indicators = new List<Indicator>
        {
            new() { IndicatorName = "Active 1", IndicatorCode = "ACT1", IndicatorDesc = "Active Test", CollectorID = 1, CollectorItemName = "ActiveItem1", IsActive = true, Priority = "high", ThresholdType = "threshold_value", ThresholdField = "Total", ThresholdValue = 100, ThresholdComparison = "gt", LastMinutes = 60, OwnerContactId = 1 },
            new() { IndicatorName = "Inactive 1", IndicatorCode = "INACT1", IndicatorDesc = "Inactive Test", CollectorID = 1, CollectorItemName = "InactiveItem1", IsActive = false, Priority = "low", ThresholdType = "threshold_value", ThresholdField = "Total", ThresholdValue = 50, ThresholdComparison = "lt", LastMinutes = 30, OwnerContactId = 1 },
            new() { IndicatorName = "Active 2", IndicatorCode = "ACT2", IndicatorDesc = "Another Active", CollectorID = 1, CollectorItemName = "ActiveItem2", IsActive = true, Priority = "medium", ThresholdType = "threshold_value", ThresholdField = "Total", ThresholdValue = 75, ThresholdComparison = "eq", LastMinutes = 45, OwnerContactId = 1 }
        };

        await repository.BulkInsertAsync(indicators);
        await repository.SaveChangesAsync();

        // Act
        var activeIndicators = await repository.GetAsync(i => i.IsActive);
        var highPriorityIndicators = await repository.GetAsync(i => i.Priority == "high");

        // Assert
        activeIndicators.Should().HaveCount(2);
        activeIndicators.Should().OnlyContain(i => i.IsActive);

        highPriorityIndicators.Should().HaveCount(1);
        highPriorityIndicators.First().IndicatorName.Should().Be("Active 1");
    }
}
