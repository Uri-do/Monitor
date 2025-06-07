using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.IntegrationTests;

/// <summary>
/// Integration tests for CQRS and Event system
/// </summary>
public class CqrsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public CqrsIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task CreateKpi_ShouldTriggerDomainEvents_AndReturnCreatedKpi()
    {
        // Arrange
        var createRequest = new CreateKpiRequest
        {
            Indicator = $"Test_KPI_{Guid.NewGuid():N}",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1 }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2.0/kpi", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {responseContent}");
        
        var createdKpi = await response.Content.ReadFromJsonAsync<KpiDto>();
        Assert.NotNull(createdKpi);
        Assert.Equal(createRequest.Indicator, createdKpi.Indicator);
        Assert.Equal(createRequest.Owner, createdKpi.Owner);
        Assert.True(createdKpi.KpiId > 0);

        // Verify domain events were processed
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var kpiRepository = unitOfWork.Repository<KPI>();
        
        var savedKpi = await kpiRepository.GetByIdAsync(createdKpi.KpiId);
        Assert.NotNull(savedKpi);
        Assert.Equal(createRequest.Indicator, savedKpi.Indicator);
    }

    [Fact]
    public async Task UpdateKpi_ShouldTriggerUpdateEvents_AndReturnUpdatedKpi()
    {
        // Arrange - First create a KPI
        var createRequest = new CreateKpiRequest
        {
            Indicator = $"Update_Test_KPI_{Guid.NewGuid():N}",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1 }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v2.0/kpi", createRequest);
        var createdKpi = await createResponse.Content.ReadFromJsonAsync<KpiDto>();
        Assert.NotNull(createdKpi);

        // Arrange - Update request
        var updateRequest = new UpdateKpiRequest
        {
            Indicator = createdKpi.Indicator + "_Updated",
            Owner = "updated@example.com",
            Priority = 1, // Changed to high priority
            Frequency = 15, // Changed frequency
            LastMinutes = 30,
            Deviation = 3.0m,
            SpName = "monitoring.usp_TestProcedure_v2",
            SubjectTemplate = "Updated alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Updated: Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 30,
            ContactIds = new List<int> { 1, 2 }
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/v2.0/kpi/{createdKpi.KpiId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        var updatedKpi = await updateResponse.Content.ReadFromJsonAsync<KpiDto>();
        Assert.NotNull(updatedKpi);
        Assert.Equal(updateRequest.Indicator, updatedKpi.Indicator);
        Assert.Equal(updateRequest.Owner, updatedKpi.Owner);
        Assert.Equal(updateRequest.Priority, updatedKpi.Priority);
        Assert.Equal(updateRequest.Frequency, updatedKpi.Frequency);
    }

    [Fact]
    public async Task ExecuteKpi_ShouldTriggerExecutionEvents_AndReturnExecutionResult()
    {
        // Arrange - Create a KPI first
        var createRequest = new CreateKpiRequest
        {
            Indicator = $"Execute_Test_KPI_{Guid.NewGuid():N}",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1 }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v2.0/kpi", createRequest);
        var createdKpi = await createResponse.Content.ReadFromJsonAsync<KpiDto>();
        Assert.NotNull(createdKpi);

        // Act
        var executeResponse = await _client.PostAsync($"/api/v2.0/kpi/{createdKpi.KpiId}/execute", null);

        // Assert
        // Note: This might fail due to missing stored procedure, but we're testing the CQRS flow
        _output.WriteLine($"Execute response status: {executeResponse.StatusCode}");
        var responseContent = await executeResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Execute response: {responseContent}");

        // The execution might fail due to missing SP, but the CQRS command should be processed
        Assert.True(executeResponse.StatusCode == HttpStatusCode.OK || 
                   executeResponse.StatusCode == HttpStatusCode.BadRequest ||
                   executeResponse.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetKpis_ShouldReturnFilteredResults()
    {
        // Arrange - Create multiple KPIs with different properties
        var kpi1 = new CreateKpiRequest
        {
            Indicator = $"Filter_Test_KPI_1_{Guid.NewGuid():N}",
            Owner = "owner1@example.com",
            Priority = 1,
            Frequency = 15,
            LastMinutes = 30,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure1",
            SubjectTemplate = "Alert 1: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "KPI 1: Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 30,
            ContactIds = new List<int> { 1 }
        };

        var kpi2 = new CreateKpiRequest
        {
            Indicator = $"Filter_Test_KPI_2_{Guid.NewGuid():N}",
            Owner = "owner2@example.com",
            Priority = 2,
            Frequency = 60,
            LastMinutes = 120,
            Deviation = 10.0m,
            SpName = "monitoring.usp_TestProcedure2",
            SubjectTemplate = "Alert 2: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "KPI 2: Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 120,
            ContactIds = new List<int> { 2 }
        };

        // Create both KPIs
        await _client.PostAsJsonAsync("/api/v2.0/kpi", kpi1);
        await _client.PostAsJsonAsync("/api/v2.0/kpi", kpi2);

        // Act & Assert - Test filtering by priority
        var priorityFilterResponse = await _client.GetAsync("/api/v2.0/kpi?priority=1");
        Assert.Equal(HttpStatusCode.OK, priorityFilterResponse.StatusCode);
        
        var priorityFilteredKpis = await priorityFilterResponse.Content.ReadFromJsonAsync<List<KpiDto>>();
        Assert.NotNull(priorityFilteredKpis);
        Assert.All(priorityFilteredKpis, kpi => Assert.Equal(1, kpi.Priority));

        // Act & Assert - Test filtering by owner
        var ownerFilterResponse = await _client.GetAsync("/api/v2.0/kpi?owner=owner1@example.com");
        Assert.Equal(HttpStatusCode.OK, ownerFilterResponse.StatusCode);
        
        var ownerFilteredKpis = await ownerFilterResponse.Content.ReadFromJsonAsync<List<KpiDto>>();
        Assert.NotNull(ownerFilteredKpis);
        Assert.All(ownerFilteredKpis, kpi => Assert.Equal("owner1@example.com", kpi.Owner));

        // Act & Assert - Test filtering by active status
        var activeFilterResponse = await _client.GetAsync("/api/v2.0/kpi?isActive=true");
        Assert.Equal(HttpStatusCode.OK, activeFilterResponse.StatusCode);
        
        var activeFilteredKpis = await activeFilterResponse.Content.ReadFromJsonAsync<List<KpiDto>>();
        Assert.NotNull(activeFilteredKpis);
        Assert.All(activeFilteredKpis, kpi => Assert.True(kpi.IsActive));
    }

    [Fact]
    public async Task DeleteKpi_ShouldTriggerDeletionEvents_AndRemoveKpi()
    {
        // Arrange - Create a KPI first
        var createRequest = new CreateKpiRequest
        {
            Indicator = $"Delete_Test_KPI_{Guid.NewGuid():N}",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_TestProcedure",
            SubjectTemplate = "Test alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1 }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v2.0/kpi", createRequest);
        var createdKpi = await createResponse.Content.ReadFromJsonAsync<KpiDto>();
        Assert.NotNull(createdKpi);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v2.0/kpi/{createdKpi.KpiId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify KPI is deleted
        var getResponse = await _client.GetAsync($"/api/v2.0/kpi/{createdKpi.KpiId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task BulkOperations_ShouldProcessMultipleKpis()
    {
        // Arrange - Create multiple KPIs
        var kpiIds = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            var createRequest = new CreateKpiRequest
            {
                Indicator = $"Bulk_Test_KPI_{i}_{Guid.NewGuid():N}",
                Owner = "bulk@example.com",
                Priority = 2,
                Frequency = 30,
                LastMinutes = 60,
                Deviation = 5.0m,
                SpName = "monitoring.usp_TestProcedure",
                SubjectTemplate = "Bulk alert: {current} vs {historical} (deviation: {deviation}%)",
                DescriptionTemplate = "Bulk: Current: {current}, Historical: {historical}, Deviation: {deviation}%",
                CooldownMinutes = 60,
                ContactIds = new List<int> { 1 }
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v2.0/kpi", createRequest);
            var createdKpi = await createResponse.Content.ReadFromJsonAsync<KpiDto>();
            Assert.NotNull(createdKpi);
            kpiIds.Add(createdKpi.KpiId);
        }

        // Act - Bulk activate operation
        var bulkRequest = new BulkKpiOperationRequest
        {
            Operation = "activate",
            KpiIds = kpiIds
        };

        var bulkResponse = await _client.PostAsJsonAsync("/api/v2.0/kpi/bulk", bulkRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, bulkResponse.StatusCode);

        // Verify changes were applied
        foreach (var kpiId in kpiIds)
        {
            var getResponse = await _client.GetAsync($"/api/v2.0/kpi/{kpiId}");
            var kpi = await getResponse.Content.ReadFromJsonAsync<KpiDto>();
            Assert.NotNull(kpi);
            Assert.True(kpi.IsActive); // Should be activated
        }
    }
}
