using FluentAssertions;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Specifications;
using Xunit;

namespace MonitoringGrid.Core.Tests.Specifications;

public class KpiSpecificationsTests
{
    [Fact]
    public void KpisDueForExecutionSpecification_ShouldIncludeActiveKpisWithoutLastRun()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.LastRun = null;
        
        var specification = new KpisDueForExecutionSpecification();

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KpisDueForExecutionSpecification_ShouldIncludeKpisDueForExecution()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Frequency = 60; // 60 minutes
        kpi.LastRun = DateTime.UtcNow.AddMinutes(-65); // 65 minutes ago
        
        var specification = new KpisDueForExecutionSpecification();

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KpisDueForExecutionSpecification_ShouldExcludeInactiveKpis()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = false;
        kpi.LastRun = null;
        
        var specification = new KpisDueForExecutionSpecification();

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void KpisDueForExecutionSpecification_ShouldExcludeKpisNotDue()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Frequency = 60; // 60 minutes
        kpi.LastRun = DateTime.UtcNow.AddMinutes(-30); // 30 minutes ago
        
        var specification = new KpisDueForExecutionSpecification();

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void KpisByOwnerSpecification_ShouldIncludeKpisWithMatchingOwner()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Owner = "TestOwner";
        kpi.IsActive = true;
        
        var specification = new KpisByOwnerSpecification("TestOwner");

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KpisByOwnerSpecification_ShouldExcludeKpisWithDifferentOwner()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Owner = "DifferentOwner";
        kpi.IsActive = true;
        
        var specification = new KpisByOwnerSpecification("TestOwner");

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HighPriorityKpisSpecification_ShouldIncludeHighPriorityKpis()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Priority = 1; // High priority (SMS)
        kpi.IsActive = true;
        
        var specification = new HighPriorityKpisSpecification();

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HighPriorityKpisSpecification_ShouldExcludeLowPriorityKpis()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Priority = 2; // Low priority (Email only)
        kpi.IsActive = true;
        
        var specification = new HighPriorityKpisSpecification();

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void StaleKpisSpecification_ShouldIncludeStaleKpis()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.LastRun = DateTime.UtcNow.AddHours(-25); // 25 hours ago
        
        var specification = new StaleKpisSpecification(24);

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void StaleKpisSpecification_ShouldExcludeRecentKpis()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.LastRun = DateTime.UtcNow.AddHours(-12); // 12 hours ago
        
        var specification = new StaleKpisSpecification(24);

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void KpisByFrequencyRangeSpecification_ShouldIncludeKpisInRange()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Frequency = 30;
        
        var specification = new KpisByFrequencyRangeSpecification(15, 60);

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KpisByFrequencyRangeSpecification_ShouldExcludeKpisOutsideRange()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Frequency = 120;
        
        var specification = new KpisByFrequencyRangeSpecification(15, 60);

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void KpiSearchSpecification_ShouldFindKpisByIndicator()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Indicator = "Test Performance Indicator";
        
        var specification = new KpiSearchSpecification("Performance");

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KpiSearchSpecification_ShouldFindKpisByOwner()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Owner = "John Smith";
        
        var specification = new KpiSearchSpecification("Smith");

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KpiSearchSpecification_ShouldNotFindNonMatchingKpis()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Indicator = "Sales Report";
        kpi.Owner = "Jane Doe";
        
        var specification = new KpiSearchSpecification("Performance");

        // Act
        var result = specification.IsSatisfiedBy(kpi);

        // Assert
        result.Should().BeFalse();
    }

    private static KPI CreateTestKpi()
    {
        return new KPI
        {
            KpiId = 1,
            Indicator = "Test KPI",
            Owner = "TestOwner",
            Priority = 1,
            Frequency = 60,
            Deviation = 10,
            SpName = "TestSP",
            SubjectTemplate = "Test Subject",
            DescriptionTemplate = "Test Description",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }
}
