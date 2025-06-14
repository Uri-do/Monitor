using FluentAssertions;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Specifications;
using Xunit;

namespace MonitoringGrid.Core.Tests.Specifications;

/// <summary>
/// Tests for Indicator specifications
/// Updated from legacy KPI specifications to use current Indicator terminology
/// </summary>
public class IndicatorSpecificationsTests
{
    [Fact]
    public void IndicatorsDueForExecutionSpecification_ShouldIncludeActiveIndicatorsNotRunning()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = true;
        indicator.IsCurrentlyRunning = false;
        indicator.LastRun = null;
        
        var specification = new IndicatorsDueForExecutionSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IndicatorsDueForExecutionSpecification_ShouldExcludeInactiveIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = false;
        indicator.IsCurrentlyRunning = false;
        indicator.LastRun = null;
        
        var specification = new IndicatorsDueForExecutionSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IndicatorsDueForExecutionSpecification_ShouldExcludeCurrentlyRunningIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = true;
        indicator.IsCurrentlyRunning = true;
        indicator.LastRun = null;
        
        var specification = new IndicatorsDueForExecutionSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IndicatorsByOwnerSpecification_ShouldIncludeIndicatorsWithMatchingOwner()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.OwnerContactId = 123;
        indicator.IsActive = true;
        
        var specification = new IndicatorsByOwnerSpecification(123);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IndicatorsByOwnerSpecification_ShouldExcludeIndicatorsWithDifferentOwner()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.OwnerContactId = 456;
        indicator.IsActive = true;
        
        var specification = new IndicatorsByOwnerSpecification(123);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IndicatorsByOwnerSpecification_ShouldExcludeInactiveIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.OwnerContactId = 123;
        indicator.IsActive = false;
        
        var specification = new IndicatorsByOwnerSpecification(123);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IndicatorsByPrioritySpecification_ShouldIncludeIndicatorsWithMatchingPriority()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.Priority = "High";
        indicator.IsActive = true;
        
        var specification = new IndicatorsByPrioritySpecification("High");

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IndicatorsByPrioritySpecification_ShouldExcludeIndicatorsWithDifferentPriority()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.Priority = "Low";
        indicator.IsActive = true;
        
        var specification = new IndicatorsByPrioritySpecification("High");

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IndicatorsByCollectorSpecification_ShouldIncludeIndicatorsWithMatchingCollector()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.CollectorID = 789;
        indicator.IsActive = true;
        
        var specification = new IndicatorsByCollectorSpecification(789);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ActiveIndicatorsSpecification_ShouldIncludeActiveIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = true;
        
        var specification = new ActiveIndicatorsSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ActiveIndicatorsSpecification_ShouldExcludeInactiveIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = false;
        
        var specification = new ActiveIndicatorsSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RunningIndicatorsSpecification_ShouldIncludeRunningIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsCurrentlyRunning = true;
        indicator.ExecutionStartTime = DateTime.UtcNow;
        
        var specification = new RunningIndicatorsSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RunningIndicatorsSpecification_ShouldExcludeNotRunningIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsCurrentlyRunning = false;
        
        var specification = new RunningIndicatorsSpecification();

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void StaleIndicatorsSpecification_ShouldIncludeStaleIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = true;
        indicator.LastRun = DateTime.UtcNow.AddHours(-25); // 25 hours ago
        
        var specification = new StaleIndicatorsSpecification(24);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void StaleIndicatorsSpecification_ShouldIncludeIndicatorsNeverRun()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = true;
        indicator.LastRun = null;
        
        var specification = new StaleIndicatorsSpecification(24);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void StaleIndicatorsSpecification_ShouldExcludeRecentIndicators()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.IsActive = true;
        indicator.LastRun = DateTime.UtcNow.AddHours(-12); // 12 hours ago
        
        var specification = new StaleIndicatorsSpecification(24);

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IndicatorsByThresholdTypeSpecification_ShouldIncludeMatchingThresholdType()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.ThresholdType = "Percentage";
        indicator.IsActive = true;
        
        var specification = new IndicatorsByThresholdTypeSpecification("Percentage");

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IndicatorsByCollectorAndItemSpecification_ShouldIncludeMatchingCollectorAndItem()
    {
        // Arrange
        var indicator = CreateTestIndicator();
        indicator.CollectorID = 789;
        indicator.CollectorItemName = "TestItem";
        indicator.IsActive = true;
        
        var specification = new IndicatorsByCollectorAndItemSpecification(789, "TestItem");

        // Act
        var result = specification.Criteria.Compile()(indicator);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Creates a test indicator with default values
    /// </summary>
    private static Indicator CreateTestIndicator()
    {
        return new Indicator
        {
            IndicatorID = 1,
            IndicatorName = "Test Indicator",
            IndicatorCode = "TEST_IND",
            IndicatorDesc = "Test Indicator Description",
            OwnerContactId = 1,
            Priority = "medium",
            CollectorID = 1,
            CollectorItemName = "DefaultItem",
            ThresholdType = "threshold_value",
            ThresholdField = "Total",
            ThresholdComparison = "gt",
            ThresholdValue = 100,
            IsActive = true,
            IsCurrentlyRunning = false,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
    }
}
