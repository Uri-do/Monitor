using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Security;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MonitoringGrid.Tests.IntegrationTests.Controllers;

public class IndicatorControllerTests : IntegrationTestBase
{
    public IndicatorControllerTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetIndicators_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/indicator");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetIndicators_WithAuthentication_ShouldReturnIndicators()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/api/indicator");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var indicators = await response.Content.ReadFromJsonAsync<List<IndicatorDto>>();
        indicators.Should().NotBeNull();
        indicators!.Should().HaveCountGreaterThan(0);
        indicators.Should().Contain(i => i.IndicatorName == "Test Indicator 1");
        indicators.Should().Contain(i => i.IndicatorName == "Test Indicator 2");
    }

    [Fact]
    public async Task GetIndicator_WithValidId_ShouldReturnIndicator()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var indicator = await ExecuteInTransactionAsync(async context =>
        {
            return await context.Indicators.FirstAsync();
        });

        // Act
        var response = await authenticatedClient.GetAsync($"/api/indicator/{indicator.IndicatorID}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var returnedIndicator = await response.Content.ReadFromJsonAsync<IndicatorDto>();
        returnedIndicator.Should().NotBeNull();
        returnedIndicator!.IndicatorID.Should().Be(indicator.IndicatorID);
        returnedIndicator.IndicatorName.Should().Be(indicator.IndicatorName);
        returnedIndicator.OwnerContactId.Should().Be(indicator.OwnerContactId);
    }

    [Fact]
    public async Task GetIndicator_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidId = 99999;

        // Act
        var response = await authenticatedClient.GetAsync($"/api/indicator/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateIndicator_WithValidData_ShouldCreateIndicator()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var createRequest = new CreateIndicatorRequest
        {
            IndicatorName = "New Test Indicator",
            IndicatorCode = "TEST_NEW",
            IndicatorDesc = "Test Description",
            CollectorItemName = "TestCollector",
            ScheduleConfiguration = "0 */5 * * * *", // Every 5 minutes
            LastMinutes = 60,
            ThresholdType = "Percentage",
            ThresholdField = "Value",
            ThresholdComparison = "gt",
            ThresholdValue = 100,
            Priority = "medium",
            OwnerContactId = 1,
            ContactIds = new List<int> { 1 }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/indicator", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdIndicator = await response.Content.ReadFromJsonAsync<IndicatorDto>();
        createdIndicator.Should().NotBeNull();
        createdIndicator!.IndicatorName.Should().Be(createRequest.IndicatorName);
        createdIndicator.IndicatorCode.Should().Be(createRequest.IndicatorCode);
        createdIndicator.ThresholdValue.Should().Be(createRequest.ThresholdValue);

        // Verify in database
        var dbIndicator = await ExecuteInTransactionAsync(async context =>
        {
            return await context.Indicators.FirstOrDefaultAsync(i => i.IndicatorID == createdIndicator.IndicatorID);
        });

        dbIndicator.Should().NotBeNull();
        dbIndicator!.IndicatorName.Should().Be(createRequest.IndicatorName);
    }

    [Fact]
    public async Task CreateIndicator_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var createRequest = new CreateIndicatorRequest
        {
            // Missing required fields
            IndicatorName = "",
            IndicatorCode = "",
            CollectorItemName = "",
            ScheduleConfiguration = "",
            ThresholdValue = -1, // Invalid threshold
            OwnerContactId = 0
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/indicator", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateIndicator_WithValidData_ShouldUpdateIndicator()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var existingIndicator = await ExecuteInTransactionAsync(async context =>
        {
            return await context.Indicators.FirstAsync();
        });

        var updateRequest = new UpdateIndicatorRequest
        {
            IndicatorID = existingIndicator.IndicatorID,
            IndicatorName = "Updated Test Indicator",
            IndicatorCode = "TEST_UPD",
            IndicatorDesc = "Updated Description",
            CollectorItemName = "UpdatedCollector",
            ScheduleConfiguration = "0 */10 * * * *", // Every 10 minutes
            LastMinutes = 120,
            ThresholdType = "Absolute",
            ThresholdField = "Count",
            ThresholdComparison = "gte",
            ThresholdValue = 200,
            Priority = "high",
            OwnerContactId = 1,
            ContactIds = new List<int> { 1 }
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/indicator/{existingIndicator.IndicatorID}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedIndicator = await response.Content.ReadFromJsonAsync<IndicatorDto>();
        updatedIndicator.Should().NotBeNull();
        updatedIndicator!.IndicatorID.Should().Be(existingIndicator.IndicatorID);
        updatedIndicator.IndicatorName.Should().Be(updateRequest.IndicatorName);
        updatedIndicator.ThresholdValue.Should().Be(updateRequest.ThresholdValue);

        // Verify in database
        var dbIndicator = await ExecuteInTransactionAsync(async context =>
        {
            return await context.Indicators.FirstOrDefaultAsync(i => i.IndicatorID == existingIndicator.IndicatorID);
        });

        dbIndicator.Should().NotBeNull();
        dbIndicator!.IndicatorName.Should().Be(updateRequest.IndicatorName);
        dbIndicator.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task UpdateIndicator_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidId = 99999;
        
        var updateRequest = new UpdateIndicatorRequest
        {
            IndicatorID = invalidId,
            IndicatorName = "Updated Test Indicator",
            IndicatorCode = "TEST_UPD",
            CollectorItemName = "UpdatedCollector",
            ScheduleConfiguration = "0 */10 * * * *",
            LastMinutes = 120,
            ThresholdType = "Absolute",
            ThresholdField = "Count",
            ThresholdComparison = "gte",
            ThresholdValue = 200,
            Priority = "high",
            OwnerContactId = 1
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/indicator/{invalidId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteIndicator_WithValidId_ShouldDeleteIndicator()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var indicatorToDelete = await ExecuteInTransactionAsync(async context =>
        {
            var indicator = new Indicator
            {
                IndicatorName = "Indicator to Delete",
                IndicatorCode = "TEST_DEL",
                CollectorItemName = "TestCollector",
                ScheduleConfiguration = "0 */5 * * * *",
                LastMinutes = 60,
                ThresholdType = "Percentage",
                ThresholdField = "Value",
                ThresholdComparison = "gt",
                ThresholdValue = 100,
                Priority = "medium",
                OwnerContactId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            
            context.Indicators.Add(indicator);
            await context.SaveChangesAsync();
            return indicator;
        });

        // Act
        var response = await authenticatedClient.DeleteAsync($"/api/indicator/{indicatorToDelete.IndicatorID}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion in database
        var deletedIndicator = await ExecuteInTransactionAsync(async context =>
        {
            return await context.Indicators.FirstOrDefaultAsync(i => i.IndicatorID == indicatorToDelete.IndicatorID);
        });

        deletedIndicator.Should().BeNull();
    }

    [Fact]
    public async Task DeleteIndicator_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidId = 99999;

        // Act
        var response = await authenticatedClient.DeleteAsync($"/api/indicator/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteIndicator_WithValidId_ShouldExecuteIndicator()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        
        var indicator = await ExecuteInTransactionAsync(async context =>
        {
            return await context.Indicators.FirstAsync();
        });

        var executeRequest = new ExecuteIndicatorRequest
        {
            IndicatorID = indicator.IndicatorID,
            ExecutionContext = "Manual Test",
            SaveResults = true
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync($"/api/indicator/{indicator.IndicatorID}/execute", executeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var executionResult = await response.Content.ReadFromJsonAsync<IndicatorExecutionResultDto>();
        executionResult.Should().NotBeNull();
        executionResult!.IndicatorID.Should().Be(indicator.IndicatorID);
        executionResult.ExecutionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}
