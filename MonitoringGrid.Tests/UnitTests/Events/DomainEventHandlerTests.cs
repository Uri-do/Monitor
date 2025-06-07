using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Events.Handlers;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.UnitTests.Events;

/// <summary>
/// Unit tests for domain event handlers
/// </summary>
public class DomainEventHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<MetricsService> _mockMetricsService;
    private readonly Mock<ILogger<KpiCreatedEventHandler>> _mockLogger;
    private readonly ITestOutputHelper _output;

    public DomainEventHandlerTests(ITestOutputHelper output)
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMetricsService = new Mock<MetricsService>();
        _mockLogger = new Mock<ILogger<KpiCreatedEventHandler>>();
        _output = output;
    }

    [Fact]
    public async Task KpiCreatedEventHandler_ShouldProcessEvent_AndUpdateMetrics()
    {
        // Arrange
        var kpi = new KPI
        {
            KpiId = 1,
            Indicator = "Test_KPI",
            Owner = "test@example.com",
            Priority = 1,
            Frequency = 15,
            LastMinutes = 30,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test: {current} vs {historical}",
            DescriptionTemplate = "Test description",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var domainEvent = new KpiCreatedEvent(kpi);
        var handler = new KpiCreatedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, _mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.IncrementKpiCreated(), Times.Once);
        _output.WriteLine($"KpiCreatedEventHandler processed event for KPI: {kpi.Indicator}");
    }

    [Fact]
    public async Task KpiUpdatedEventHandler_ShouldProcessEvent_AndLogChanges()
    {
        // Arrange
        var originalKpi = new KPI
        {
            KpiId = 1,
            Indicator = "Test_KPI",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test: {current} vs {historical}",
            DescriptionTemplate = "Test description",
            IsActive = true,
            ModifiedDate = DateTime.UtcNow.AddHours(-1)
        };

        var updatedKpi = new KPI
        {
            KpiId = 1,
            Indicator = "Test_KPI_Updated",
            Owner = "updated@example.com",
            Priority = 1, // Changed
            Frequency = 15, // Changed
            LastMinutes = 30, // Changed
            Deviation = 3.0m, // Changed
            SpName = "monitoring.usp_TestProcedure_v2",
            SubjectTemplate = "Updated: {current} vs {historical}",
            DescriptionTemplate = "Updated description",
            IsActive = true,
            ModifiedDate = DateTime.UtcNow
        };

        var domainEvent = new KpiUpdatedEvent(updatedKpi, originalKpi);
        var mockLogger = new Mock<ILogger<KpiUpdatedEventHandler>>();
        var handler = new KpiUpdatedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.IncrementKpiUpdated(), Times.Once);
        _output.WriteLine($"KpiUpdatedEventHandler processed event for KPI: {updatedKpi.Indicator}");
    }

    [Fact]
    public async Task KpiExecutedEventHandler_ShouldProcessEvent_AndUpdateExecutionMetrics()
    {
        // Arrange
        var kpi = new KPI
        {
            KpiId = 1,
            Indicator = "Test_KPI",
            Owner = "test@example.com",
            Priority = 1,
            Frequency = 15,
            LastMinutes = 30,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test: {current} vs {historical}",
            DescriptionTemplate = "Test description",
            IsActive = true,
            LastRun = DateTime.UtcNow
        };

        var executionResult = new
        {
            Success = true,
            CurrentValue = 95.5m,
            HistoricalValue = 98.2m,
            DeviationPercent = -2.75m,
            ExecutionDurationMs = 1250,
            ThresholdBreached = false
        };

        var domainEvent = new KpiExecutedEvent(kpi, executionResult.Success, executionResult.ExecutionDurationMs, 
            executionResult.CurrentValue, executionResult.HistoricalValue, executionResult.DeviationPercent, 
            executionResult.ThresholdBreached);

        var mockLogger = new Mock<ILogger<KpiExecutedEventHandler>>();
        var handler = new KpiExecutedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.RecordKpiExecution(kpi.Indicator, executionResult.Success, executionResult.ExecutionDurationMs), Times.Once);
        _output.WriteLine($"KpiExecutedEventHandler processed execution event for KPI: {kpi.Indicator}");
    }

    [Fact]
    public async Task KpiThresholdBreachedEventHandler_ShouldProcessEvent_AndTriggerAlert()
    {
        // Arrange
        var kpi = new KPI
        {
            KpiId = 1,
            Indicator = "Test_KPI",
            Owner = "test@example.com",
            Priority = 1,
            Frequency = 15,
            LastMinutes = 30,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Threshold breached: Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            IsActive = true
        };

        var alertData = new
        {
            CurrentValue = 85.2m,
            HistoricalValue = 98.5m,
            DeviationPercent = -13.5m,
            ThresholdBreached = true
        };

        var domainEvent = new KpiThresholdBreachedEvent(kpi, alertData.CurrentValue, alertData.HistoricalValue, 
            alertData.DeviationPercent);

        var mockAlertRepository = new Mock<IRepository<AlertLog>>();
        _mockUnitOfWork.Setup(u => u.Repository<AlertLog>()).Returns(mockAlertRepository.Object);

        var mockLogger = new Mock<ILogger<KpiThresholdBreachedEventHandler>>();
        var handler = new KpiThresholdBreachedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        mockAlertRepository.Verify(r => r.AddAsync(It.IsAny<AlertLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMetricsService.Verify(m => m.IncrementAlertTriggered(kpi.Priority), Times.Once);
        _output.WriteLine($"KpiThresholdBreachedEventHandler processed threshold breach for KPI: {kpi.Indicator}");
    }

    [Fact]
    public async Task KpiDeactivatedEventHandler_ShouldProcessEvent_AndCleanupResources()
    {
        // Arrange
        var kpi = new KPI
        {
            KpiId = 1,
            Indicator = "Test_KPI",
            Owner = "test@example.com",
            Priority = 1,
            Frequency = 15,
            LastMinutes = 30,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test: {current} vs {historical}",
            DescriptionTemplate = "Test description",
            IsActive = false, // Deactivated
            ModifiedDate = DateTime.UtcNow
        };

        var domainEvent = new KpiDeactivatedEvent(kpi);
        var mockLogger = new Mock<ILogger<KpiDeactivatedEventHandler>>();
        var handler = new KpiDeactivatedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.IncrementKpiDeactivated(), Times.Once);
        _output.WriteLine($"KpiDeactivatedEventHandler processed deactivation for KPI: {kpi.Indicator}");
    }

    [Fact]
    public async Task AlertTriggeredEventHandler_ShouldProcessEvent_AndSendNotifications()
    {
        // Arrange
        var alert = new AlertLog
        {
            AlertId = 1,
            KpiId = 1,
            Indicator = "Test_KPI",
            TriggerTime = DateTime.UtcNow,
            CurrentValue = 85.2m,
            HistoricalValue = 98.5m,
            DeviationPercent = -13.5m,
            Subject = "Alert: 85.2% vs 98.5% (deviation: -13.5%)",
            Description = "Threshold breached: Current: 85.2%, Historical: 98.5%, Deviation: -13.5%",
            Priority = 1,
            IsResolved = false
        };

        var domainEvent = new AlertTriggeredEvent(alert);
        var mockLogger = new Mock<ILogger<AlertTriggeredEventHandler>>();
        var handler = new AlertTriggeredEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.IncrementAlertTriggered(alert.Priority), Times.Once);
        _output.WriteLine($"AlertTriggeredEventHandler processed alert for KPI: {alert.Indicator}");
    }

    [Fact]
    public async Task AlertResolvedEventHandler_ShouldProcessEvent_AndUpdateMetrics()
    {
        // Arrange
        var alert = new AlertLog
        {
            AlertId = 1,
            KpiId = 1,
            Indicator = "Test_KPI",
            TriggerTime = DateTime.UtcNow.AddHours(-2),
            CurrentValue = 85.2m,
            HistoricalValue = 98.5m,
            DeviationPercent = -13.5m,
            Subject = "Alert: 85.2% vs 98.5% (deviation: -13.5%)",
            Description = "Threshold breached: Current: 85.2%, Historical: 98.5%, Deviation: -13.5%",
            Priority = 1,
            IsResolved = true,
            ResolvedTime = DateTime.UtcNow,
            ResolvedBy = "admin@example.com"
        };

        var domainEvent = new AlertResolvedEvent(alert);
        var mockLogger = new Mock<ILogger<AlertResolvedEventHandler>>();
        var handler = new AlertResolvedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.IncrementAlertResolved(), Times.Once);
        var resolutionTime = (alert.ResolvedTime!.Value - alert.TriggerTime).TotalMinutes;
        _mockMetricsService.Verify(m => m.RecordAlertResolutionTime(resolutionTime), Times.Once);
        _output.WriteLine($"AlertResolvedEventHandler processed resolution for alert: {alert.AlertId}");
    }

    [Theory]
    [InlineData(true, 1500)]
    [InlineData(false, 2500)]
    public async Task KpiExecutedEventHandler_ShouldHandleDifferentExecutionOutcomes(bool success, int durationMs)
    {
        // Arrange
        var kpi = new KPI
        {
            KpiId = 1,
            Indicator = "Theory_Test_KPI",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test: {current} vs {historical}",
            DescriptionTemplate = "Test description",
            IsActive = true,
            LastRun = DateTime.UtcNow
        };

        var domainEvent = new KpiExecutedEvent(kpi, success, durationMs, 95.5m, 98.2m, -2.75m, false);
        var mockLogger = new Mock<ILogger<KpiExecutedEventHandler>>();
        var handler = new KpiExecutedEventHandler(_mockUnitOfWork.Object, _mockMetricsService.Object, mockLogger.Object);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMetricsService.Verify(m => m.RecordKpiExecution(kpi.Indicator, success, durationMs), Times.Once);
        _output.WriteLine($"KpiExecutedEventHandler processed {(success ? "successful" : "failed")} execution in {durationMs}ms");
    }
}
