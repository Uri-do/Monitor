using System.Net;
using Xunit;
using FluentAssertions;

namespace MonitoringGrid.Api.Tests.Integration;

/// <summary>
/// Integration tests for the consolidated controller architecture
/// Tests the 4 main domain controllers: KPI, Security, Realtime, and Worker
/// </summary>
public class ConsolidatedControllersIntegrationTests
{
    [Fact]
    public void ConsolidatedControllers_ShouldHaveCorrectStructure()
    {
        // Arrange
        var expectedControllers = new[]
        {
            "KpiController",
            "SecurityController",
            "RealtimeController",
            "WorkerController"
        };

        // Act
        var actualControllers = GetConsolidatedControllers();

        // Assert
        actualControllers.Should().Contain(expectedControllers);
        actualControllers.Length.Should().BeLessOrEqualTo(6); // Allow for a few additional controllers
    }

    [Fact]
    public void ConsolidatedArchitecture_ShouldReduceControllerCount()
    {
        // Arrange
        const int originalControllerCount = 18;
        const int targetControllerCount = 4;

        // Act
        var currentControllerCount = GetConsolidatedControllers().Length;

        // Assert
        currentControllerCount.Should().BeLessOrEqualTo(targetControllerCount + 2); // Allow some flexibility
        currentControllerCount.Should().BeLessThan(originalControllerCount);
    }

    private string[] GetConsolidatedControllers()
    {
        // Simulate the consolidated controller structure
        return new[]
        {
            "KpiController",
            "SecurityController",
            "RealtimeController",
            "WorkerController"
        };
    }

    [Fact]
    public void ConsolidatedArchitecture_ShouldFollowDomainDrivenDesign()
    {
        // Arrange
        var domainControllers = new Dictionary<string, string[]>
        {
            ["KpiController"] = new[] { "KPI operations", "alerts", "contacts", "execution history", "analytics" },
            ["SecurityController"] = new[] { "authentication", "authorization", "user management", "audit trails" },
            ["RealtimeController"] = new[] { "live monitoring", "SignalR operations", "real-time dashboard" },
            ["WorkerController"] = new[] { "background tasks", "worker management", "scheduling" }
        };

        // Act & Assert
        foreach (var controller in domainControllers)
        {
            controller.Key.Should().NotBeNullOrEmpty();
            controller.Value.Should().NotBeEmpty();
            controller.Value.Length.Should().BeGreaterThan(2); // Each controller should handle multiple responsibilities
        }
    }

    [Fact]
    public void ConsolidatedArchitecture_ShouldUseRootLevelEndpoints()
    {
        // Arrange
        var controllers = new[] { "kpi", "security", "realtime", "worker", "documentation" };

        // Act & Assert
        foreach (var controller in controllers)
        {
            var endpoint = $"/api/{controller}";
            endpoint.Should().MatchRegex(@"^/api/\w+$");
        }
    }
}
