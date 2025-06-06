using FluentAssertions;
using MonitoringGrid.Core.Events;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.Events;

public class DomainEventsTests
{
    [Fact]
    public void KpiExecutedEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int kpiId = 1;
        const string indicator = "Test KPI";
        const string owner = "TestOwner";
        const bool wasSuccessful = true;
        const decimal currentValue = 100;
        const decimal historicalValue = 80;

        // Act
        var domainEvent = new KpiExecutedEvent(kpiId, indicator, owner, wasSuccessful, currentValue, historicalValue);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.Version.Should().Be(1);
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.Indicator.Should().Be(indicator);
        domainEvent.Owner.Should().Be(owner);
        domainEvent.WasSuccessful.Should().Be(wasSuccessful);
        domainEvent.CurrentValue.Should().Be(currentValue);
        domainEvent.HistoricalValue.Should().Be(historicalValue);
        domainEvent.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void KpiExecutedEvent_WithError_ShouldIncludeErrorMessage()
    {
        // Arrange
        const int kpiId = 1;
        const string indicator = "Test KPI";
        const string owner = "TestOwner";
        const bool wasSuccessful = false;
        const string errorMessage = "Database connection failed";

        // Act
        var domainEvent = new KpiExecutedEvent(kpiId, indicator, owner, wasSuccessful, errorMessage: errorMessage);

        // Assert
        domainEvent.WasSuccessful.Should().BeFalse();
        domainEvent.ErrorMessage.Should().Be(errorMessage);
        domainEvent.CurrentValue.Should().BeNull();
        domainEvent.HistoricalValue.Should().BeNull();
    }

    [Fact]
    public void KpiThresholdBreachedEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int kpiId = 1;
        const string indicator = "Test KPI";
        const decimal currentValue = 100;
        const decimal historicalValue = 80;
        const decimal deviation = 25;
        const string severity = "High";

        // Act
        var domainEvent = new KpiThresholdBreachedEvent(kpiId, indicator, currentValue, historicalValue, deviation, severity);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.Indicator.Should().Be(indicator);
        domainEvent.CurrentValue.Should().Be(currentValue);
        domainEvent.HistoricalValue.Should().Be(historicalValue);
        domainEvent.Deviation.Should().Be(deviation);
        domainEvent.Severity.Should().Be(severity);
    }

    [Fact]
    public void KpiDeactivatedEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int kpiId = 1;
        const string indicator = "Test KPI";
        const string reason = "No longer needed";
        const string deactivatedBy = "Admin";

        // Act
        var domainEvent = new KpiDeactivatedEvent(kpiId, indicator, reason, deactivatedBy);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.Indicator.Should().Be(indicator);
        domainEvent.Reason.Should().Be(reason);
        domainEvent.DeactivatedBy.Should().Be(deactivatedBy);
    }

    [Fact]
    public void AlertTriggeredEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int alertId = 1;
        const int kpiId = 2;
        const string severity = "High";
        const string subject = "Alert Subject";
        const string message = "Alert Message";
        const decimal currentValue = 100;
        const decimal historicalValue = 80;
        const decimal deviation = 25;

        // Act
        var domainEvent = new AlertTriggeredEvent(alertId, kpiId, severity, subject, message, currentValue, historicalValue, deviation);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.AlertId.Should().Be(alertId);
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.Severity.Should().Be(severity);
        domainEvent.Subject.Should().Be(subject);
        domainEvent.Message.Should().Be(message);
        domainEvent.CurrentValue.Should().Be(currentValue);
        domainEvent.HistoricalValue.Should().Be(historicalValue);
        domainEvent.Deviation.Should().Be(deviation);
    }

    [Fact]
    public void AlertResolvedEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int alertId = 1;
        const int kpiId = 2;
        const string resolvedBy = "Admin";
        const string resolution = "Issue fixed";

        // Act
        var domainEvent = new AlertResolvedEvent(alertId, kpiId, resolvedBy, resolution);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.AlertId.Should().Be(alertId);
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.ResolvedBy.Should().Be(resolvedBy);
        domainEvent.Resolution.Should().Be(resolution);
    }

    [Fact]
    public void AlertAcknowledgedEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int alertId = 1;
        const int kpiId = 2;
        const string acknowledgedBy = "Operator";
        const string notes = "Investigating the issue";

        // Act
        var domainEvent = new AlertAcknowledgedEvent(alertId, kpiId, acknowledgedBy, notes);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.AlertId.Should().Be(alertId);
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.AcknowledgedBy.Should().Be(acknowledgedBy);
        domainEvent.Notes.Should().Be(notes);
    }

    [Fact]
    public void AlertEscalationTriggeredEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int alertId = 1;
        const int kpiId = 2;
        const int escalationLevel = 2;
        const string reason = "No response after 30 minutes";

        // Act
        var domainEvent = new AlertEscalationTriggeredEvent(alertId, kpiId, escalationLevel, reason);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.AlertId.Should().Be(alertId);
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.EscalationLevel.Should().Be(escalationLevel);
        domainEvent.Reason.Should().Be(reason);
    }

    [Fact]
    public void AlertNotificationSentEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        const int alertId = 1;
        const int kpiId = 2;
        const string channel = "Email";
        var recipients = new List<string> { "admin@example.com", "operator@example.com" };
        const bool wasSuccessful = true;

        // Act
        var domainEvent = new AlertNotificationSentEvent(alertId, kpiId, channel, recipients, wasSuccessful);

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.AlertId.Should().Be(alertId);
        domainEvent.KpiId.Should().Be(kpiId);
        domainEvent.Channel.Should().Be(channel);
        domainEvent.Recipients.Should().BeEquivalentTo(recipients);
        domainEvent.WasSuccessful.Should().Be(wasSuccessful);
        domainEvent.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void AlertNotificationSentEvent_WithError_ShouldIncludeErrorMessage()
    {
        // Arrange
        const int alertId = 1;
        const int kpiId = 2;
        const string channel = "SMS";
        var recipients = new List<string> { "+1234567890" };
        const bool wasSuccessful = false;
        const string errorMessage = "SMS gateway unavailable";

        // Act
        var domainEvent = new AlertNotificationSentEvent(alertId, kpiId, channel, recipients, wasSuccessful, errorMessage);

        // Assert
        domainEvent.WasSuccessful.Should().BeFalse();
        domainEvent.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventIds()
    {
        // Arrange & Act
        var event1 = new KpiExecutedEvent(1, "KPI1", "Owner1", true);
        var event2 = new KpiExecutedEvent(2, "KPI2", "Owner2", true);

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_ShouldHaveRecentTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new KpiExecutedEvent(1, "KPI", "Owner", true);
        var afterCreation = DateTime.UtcNow;

        // Assert
        domainEvent.OccurredOn.Should().BeOnOrAfter(beforeCreation);
        domainEvent.OccurredOn.Should().BeOnOrBefore(afterCreation);
    }
}
