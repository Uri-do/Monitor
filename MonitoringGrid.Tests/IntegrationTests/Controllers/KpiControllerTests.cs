using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Security;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MonitoringGrid.Tests.IntegrationTests.Controllers;

public class KpiControllerTests : IntegrationTestBase
{
    public KpiControllerTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetKpis_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetKpis_WithAuthentication_ShouldReturnKpis()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/api/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var kpis = await response.Content.ReadFromJsonAsync<List<KpiDto>>();
        kpis.Should().NotBeNull();
        kpis!.Should().HaveCountGreaterThan(0);
        kpis.Should().Contain(k => k.Indicator == "Test KPI 1");
        kpis.Should().Contain(k => k.Indicator == "Test KPI 2");
    }

    [Fact]
    public async Task GetKpi_WithValidId_ShouldReturnKpi()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var kpi = await ExecuteInTransactionAsync(async context =>
        {
            return await context.KPIs.FirstAsync();
        });

        // Act
        var response = await authenticatedClient.GetAsync($"/api/kpis/{kpi.KpiId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var returnedKpi = await response.Content.ReadFromJsonAsync<KpiDto>();
        returnedKpi.Should().NotBeNull();
        returnedKpi!.KpiId.Should().Be(kpi.KpiId);
        returnedKpi.Indicator.Should().Be(kpi.Indicator);
        returnedKpi.Owner.Should().Be(kpi.Owner);
    }

    [Fact]
    public async Task GetKpi_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidId = 99999;

        // Act
        var response = await authenticatedClient.GetAsync($"/api/kpis/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateKpi_WithValidData_ShouldCreateKpi()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var createRequest = new CreateKpiRequest
        {
            Indicator = "New Test KPI",
            Owner = "Test Owner",
            Query = "SELECT COUNT(*) FROM NewTable",
            HistoricalQuery = "SELECT COUNT(*) FROM NewTable WHERE Date = DATEADD(day, -1, GETDATE())",
            Threshold = 15.0m,
            Priority = 1,
            SubjectTemplate = "Alert: {{indicator}}",
            DescriptionTemplate = "Current: {{current}}, Historical: {{historical}}"
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/kpis", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdKpi = await response.Content.ReadFromJsonAsync<KpiDto>();
        createdKpi.Should().NotBeNull();
        createdKpi!.Indicator.Should().Be(createRequest.Indicator);
        createdKpi.Owner.Should().Be(createRequest.Owner);
        createdKpi.Query.Should().Be(createRequest.Query);
        createdKpi.Threshold.Should().Be(createRequest.Threshold);

        // Verify in database
        var dbKpi = await ExecuteInTransactionAsync(async context =>
        {
            return await context.KPIs.FirstOrDefaultAsync(k => k.KpiId == createdKpi.KpiId);
        });

        dbKpi.Should().NotBeNull();
        dbKpi!.Indicator.Should().Be(createRequest.Indicator);
    }

    [Fact]
    public async Task CreateKpi_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var createRequest = new CreateKpiRequest
        {
            // Missing required fields
            Indicator = "",
            Owner = "",
            Query = "",
            HistoricalQuery = "",
            Threshold = -1, // Invalid threshold
            Priority = 0
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/kpis", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateKpi_WithValidData_ShouldUpdateKpi()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var existingKpi = await ExecuteInTransactionAsync(async context =>
        {
            return await context.KPIs.FirstAsync();
        });

        var updateRequest = new UpdateKpiRequest
        {
            Indicator = "Updated Test KPI",
            Owner = "Updated Owner",
            Query = "SELECT COUNT(*) FROM UpdatedTable",
            HistoricalQuery = "SELECT COUNT(*) FROM UpdatedTable WHERE Date = DATEADD(day, -1, GETDATE())",
            Threshold = 20.0m,
            Priority = 2,
            SubjectTemplate = "Updated Alert: {{indicator}}",
            DescriptionTemplate = "Updated: Current: {{current}}, Historical: {{historical}}"
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/kpis/{existingKpi.KpiId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedKpi = await response.Content.ReadFromJsonAsync<KpiDto>();
        updatedKpi.Should().NotBeNull();
        updatedKpi!.KpiId.Should().Be(existingKpi.KpiId);
        updatedKpi.Indicator.Should().Be(updateRequest.Indicator);
        updatedKpi.Owner.Should().Be(updateRequest.Owner);
        updatedKpi.Threshold.Should().Be(updateRequest.Threshold);

        // Verify in database
        var dbKpi = await ExecuteInTransactionAsync(async context =>
        {
            return await context.KPIs.FirstOrDefaultAsync(k => k.KpiId == existingKpi.KpiId);
        });

        dbKpi.Should().NotBeNull();
        dbKpi!.Indicator.Should().Be(updateRequest.Indicator);
        dbKpi.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task UpdateKpi_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidId = 99999;
        
        var updateRequest = new UpdateKpiRequest
        {
            Indicator = "Updated Test KPI",
            Owner = "Updated Owner",
            Query = "SELECT COUNT(*) FROM UpdatedTable",
            HistoricalQuery = "SELECT COUNT(*) FROM UpdatedTable WHERE Date = DATEADD(day, -1, GETDATE())",
            Threshold = 20.0m,
            Priority = 2
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/kpis/{invalidId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteKpi_WithValidId_ShouldDeleteKpi()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var kpiToDelete = await ExecuteInTransactionAsync(async context =>
        {
            var kpi = new KPI
            {
                Indicator = "KPI to Delete",
                Owner = "Test Owner",
                Query = "SELECT 1",
                HistoricalQuery = "SELECT 1",
                Threshold = 10.0m,
                Priority = 3,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            
            context.KPIs.Add(kpi);
            await context.SaveChangesAsync();
            return kpi;
        });

        // Act
        var response = await authenticatedClient.DeleteAsync($"/api/kpis/{kpiToDelete.KpiId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion in database
        var deletedKpi = await ExecuteInTransactionAsync(async context =>
        {
            return await context.KPIs.FirstOrDefaultAsync(k => k.KpiId == kpiToDelete.KpiId);
        });

        deletedKpi.Should().BeNull();
    }

    [Fact]
    public async Task DeleteKpi_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidId = 99999;

        // Act
        var response = await authenticatedClient.DeleteAsync($"/api/kpis/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteKpi_WithValidId_ShouldExecuteKpi()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var kpi = await ExecuteInTransactionAsync(async context =>
        {
            return await context.KPIs.FirstAsync();
        });

        // Act
        var response = await authenticatedClient.PostAsync($"/api/kpis/{kpi.KpiId}/execute", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var executionResult = await response.Content.ReadFromJsonAsync<KpiExecutionResultDto>();
        executionResult.Should().NotBeNull();
        executionResult!.KpiId.Should().Be(kpi.KpiId);
        executionResult.ExecutionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetKpiHistory_WithValidId_ShouldReturnHistory()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var kpi = await ExecuteInTransactionAsync(async context =>
        {
            var existingKpi = await context.KPIs.FirstAsync();
            
            // Add some historical data
            var historicalData = new[]
            {
                new HistoricalData
                {
                    KpiId = existingKpi.KpiId,
                    ExecutionTime = DateTime.UtcNow.AddDays(-2),
                    CurrentValue = 100,
                    HistoricalValue = 95,
                    DeviationPercentage = 5.26m,
                    IsSuccessful = true,
                    ExecutionTimeMs = 150
                },
                new HistoricalData
                {
                    KpiId = existingKpi.KpiId,
                    ExecutionTime = DateTime.UtcNow.AddDays(-1),
                    CurrentValue = 105,
                    HistoricalValue = 100,
                    DeviationPercentage = 5.0m,
                    IsSuccessful = true,
                    ExecutionTimeMs = 120
                }
            };
            
            context.HistoricalData.AddRange(historicalData);
            await context.SaveChangesAsync();
            
            return existingKpi;
        });

        // Act
        var response = await authenticatedClient.GetAsync($"/api/kpis/{kpi.KpiId}/history?days=7");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var history = await response.Content.ReadFromJsonAsync<List<HistoricalDataDto>>();
        history.Should().NotBeNull();
        history!.Should().HaveCount(2);
        history.Should().OnlyContain(h => h.KpiId == kpi.KpiId);
        history.Should().BeInDescendingOrder(h => h.ExecutionTime);
    }

    [Fact]
    public async Task GetKpis_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/api/kpis?page=1&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var kpis = await response.Content.ReadFromJsonAsync<List<KpiDto>>();
        kpis.Should().NotBeNull();
        kpis!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetKpis_WithFiltering_ShouldReturnFilteredResults()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/api/kpis?owner=Test Owner 1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var kpis = await response.Content.ReadFromJsonAsync<List<KpiDto>>();
        kpis.Should().NotBeNull();
        kpis!.Should().OnlyContain(k => k.Owner == "Test Owner 1");
    }
}
