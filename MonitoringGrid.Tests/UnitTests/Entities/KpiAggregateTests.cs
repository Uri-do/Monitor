using FluentAssertions;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Exceptions;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.Entities;

public class KpiAggregateTests
{
    [Fact]
    public void MarkAsExecuted_WithSuccessfulExecution_ShouldRaiseDomainEvent()
    {
        // Arrange
        var kpi = CreateTestKpi();
        const decimal currentValue = 100;
        const decimal historicalValue = 80;

        // Act
        kpi.MarkAsExecuted(true, currentValue, historicalValue);

        // Assert
        kpi.DomainEvents.Should().HaveCount(1);
        var executedEvent = kpi.DomainEvents.First() as KpiExecutedEvent;
        executedEvent.Should().NotBeNull();
        executedEvent!.KpiId.Should().Be(kpi.KpiId);
        executedEvent.WasSuccessful.Should().BeTrue();
        executedEvent.CurrentValue.Should().Be(currentValue);
        executedEvent.HistoricalValue.Should().Be(historicalValue);
    }

    [Fact]
    public void MarkAsExecuted_WithThresholdBreach_ShouldRaiseThresholdEvent()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Deviation = 10; // 10% threshold
        const decimal currentValue = 100;
        const decimal historicalValue = 80; // 25% deviation

        // Act
        kpi.MarkAsExecuted(true, currentValue, historicalValue);

        // Assert
        kpi.DomainEvents.Should().HaveCount(2);
        var thresholdEvent = kpi.DomainEvents.OfType<KpiThresholdBreachedEvent>().FirstOrDefault();
        thresholdEvent.Should().NotBeNull();
        thresholdEvent!.KpiId.Should().Be(kpi.KpiId);
        thresholdEvent.CurrentValue.Should().Be(currentValue);
        thresholdEvent.HistoricalValue.Should().Be(historicalValue);
        thresholdEvent.Severity.Should().Be("High");
    }

    [Fact]
    public void MarkAsExecuted_WithFailedExecution_ShouldRaiseEventWithError()
    {
        // Arrange
        var kpi = CreateTestKpi();
        const string errorMessage = "Database connection failed";

        // Act
        kpi.MarkAsExecuted(false, errorMessage: errorMessage);

        // Assert
        kpi.DomainEvents.Should().HaveCount(1);
        var executedEvent = kpi.DomainEvents.First() as KpiExecutedEvent;
        executedEvent.Should().NotBeNull();
        executedEvent!.WasSuccessful.Should().BeFalse();
        executedEvent.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void MarkAsExecuted_ShouldUpdateLastRunTime()
    {
        // Arrange
        var kpi = CreateTestKpi();
        var beforeExecution = DateTime.UtcNow;

        // Act
        kpi.MarkAsExecuted(true);

        // Assert
        kpi.LastRun.Should().NotBeNull();
        kpi.LastRun.Should().BeOnOrAfter(beforeExecution);
        kpi.ModifiedDate.Should().BeOnOrAfter(beforeExecution);
    }

    [Fact]
    public void Deactivate_WithActiveKpi_ShouldDeactivateAndRaiseEvent()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        const string reason = "No longer needed";
        const string deactivatedBy = "Admin";

        // Act
        kpi.Deactivate(reason, deactivatedBy);

        // Assert
        kpi.IsActive.Should().BeFalse();
        kpi.DomainEvents.Should().HaveCount(1);
        var deactivatedEvent = kpi.DomainEvents.First() as KpiDeactivatedEvent;
        deactivatedEvent.Should().NotBeNull();
        deactivatedEvent!.KpiId.Should().Be(kpi.KpiId);
        deactivatedEvent.Reason.Should().Be(reason);
        deactivatedEvent.DeactivatedBy.Should().Be(deactivatedBy);
    }

    [Fact]
    public void Deactivate_WithInactiveKpi_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = false;

        // Act & Assert
        Assert.Throws<BusinessRuleViolationException>(() => kpi.Deactivate("reason", "user"));
    }

    [Fact]
    public void UpdateConfiguration_ShouldRaiseUpdateEvent()
    {
        // Arrange
        var kpi = CreateTestKpi();
        const string updatedBy = "Admin";

        // Act
        kpi.UpdateConfiguration(updatedBy);

        // Assert
        kpi.DomainEvents.Should().HaveCount(1);
        var updatedEvent = kpi.DomainEvents.First() as KpiUpdatedEvent;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.KpiId.Should().Be(kpi.KpiId);
        updatedEvent.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void ValidateConfiguration_WithValidKpi_ShouldNotThrow()
    {
        // Arrange
        var kpi = CreateTestKpi();

        // Act & Assert
        kpi.Invoking(k => k.ValidateConfiguration()).Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidIndicator_ShouldThrowKpiValidationException()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Indicator = "";

        // Act & Assert
        var exception = Assert.Throws<KpiValidationException>(() => kpi.ValidateConfiguration());
        exception.ValidationErrors.Should().Contain("Indicator is required");
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidPriority_ShouldThrowKpiValidationException()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Priority = 3; // Invalid priority

        // Act & Assert
        var exception = Assert.Throws<KpiValidationException>(() => kpi.ValidateConfiguration());
        exception.ValidationErrors.Should().Contain("Priority must be 1 (SMS + Email) or 2 (Email Only)");
    }

    [Fact]
    public void ValidateConfiguration_WithMultipleErrors_ShouldIncludeAllErrors()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Indicator = "";
        kpi.Owner = "";
        kpi.Frequency = -1;

        // Act & Assert
        var exception = Assert.Throws<KpiValidationException>(() => kpi.ValidateConfiguration());
        exception.ValidationErrors.Should().HaveCount(3);
        exception.ValidationErrors.Should().Contain("Indicator is required");
        exception.ValidationErrors.Should().Contain("Owner is required");
        exception.ValidationErrors.Should().Contain("Frequency must be greater than 0");
    }

    [Fact]
    public void IsDue_WithNullLastRun_ShouldReturnTrue()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.LastRun = null;

        // Act
        var result = kpi.IsDue();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDue_WithInactiveKpi_ShouldReturnTrue()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = false;

        // Act
        var result = kpi.IsDue();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDue_WithRecentExecution_ShouldReturnFalse()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.IsActive = true;
        kpi.Frequency = 60; // 60 minutes
        kpi.LastRun = DateTime.UtcNow.AddMinutes(-30); // 30 minutes ago

        // Act
        var result = kpi.IsDue();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInCooldown_WithRecentExecution_ShouldReturnTrue()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.CooldownMinutes = 30;
        kpi.LastRun = DateTime.UtcNow.AddMinutes(-15); // 15 minutes ago

        // Act
        var result = kpi.IsInCooldown();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInCooldown_WithOldExecution_ShouldReturnFalse()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.CooldownMinutes = 30;
        kpi.LastRun = DateTime.UtcNow.AddMinutes(-45); // 45 minutes ago

        // Act
        var result = kpi.IsInCooldown();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "SMS + Email")]
    [InlineData(2, "Email Only")]
    [InlineData(3, "Unknown")]
    public void GetPriorityName_ShouldReturnCorrectName(byte priority, string expectedName)
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.Priority = priority;

        // Act
        var result = kpi.GetPriorityName();

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var kpi = CreateTestKpi();
        kpi.MarkAsExecuted(true);
        kpi.UpdateConfiguration("Admin");

        // Act
        kpi.ClearDomainEvents();

        // Assert
        kpi.DomainEvents.Should().BeEmpty();
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
            SubjectTemplate = "Test Subject: {Indicator}",
            DescriptionTemplate = "Test Description: {Indicator} deviated by {Deviation}%",
            IsActive = true,
            CooldownMinutes = 30,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }
}
