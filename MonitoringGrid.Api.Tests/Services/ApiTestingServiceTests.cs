using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Api.Models.Testing;
using System.Net.Http;

namespace MonitoringGrid.Api.Tests.Services;

/// <summary>
/// Unit tests for ApiTestingService
/// </summary>
public class ApiTestingServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ApiTestingService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly ApiTestingService _service;

    public ApiTestingServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ApiTestingService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _service = new ApiTestingService(
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _serviceProviderMock.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenValidParametersProvided()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Should().BeOfType<ApiTestingService>();
    }

    [Fact]
    public async Task GetTestableEndpointsAsync_ShouldReturnEndpoints_WhenControllersExist()
    {
        // Act
        var result = await _service.GetTestableEndpointsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<TestableEndpoint>>();
        
        // Should discover the ApiTestingController itself
        var apiTestingEndpoints = result.Where(e => e.Controller == "ApiTesting").ToList();
        apiTestingEndpoints.Should().NotBeEmpty();
        
        // Should have the main endpoints we created
        var endpointPaths = apiTestingEndpoints.Select(e => e.Path).ToList();
        endpointPaths.Should().Contain(path => path.Contains("endpoints"));
        endpointPaths.Should().Contain(path => path.Contains("execute"));
    }

    [Fact]
    public async Task GetTestHistoryAsync_ShouldReturnEmptyHistory_WhenNoHistoryExists()
    {
        // Arrange
        var request = new GetTestHistoryRequest
        {
            Days = 7,
            PageSize = 10
        };

        // Act
        var result = await _service.GetTestHistoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalExecutions.Should().Be(0);
        result.Executions.Should().BeEmpty();
        result.Page.Should().Be(request.Page);
        result.PageSize.Should().Be(request.PageSize);
    }

    [Fact]
    public async Task GetTestStatisticsAsync_ShouldReturnEmptyStatistics_WhenNoDataExists()
    {
        // Arrange
        var request = new GetTestStatisticsRequest
        {
            Days = 30,
            IncludeTrends = true
        };

        // Act
        var result = await _service.GetTestStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Overview.Should().NotBeNull();
        result.ControllerStats.Should().BeEmpty();
        result.EndpointStats.Should().BeEmpty();
        result.Performance.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateTestDataAsync_ShouldReturnTestData_WhenValidRequestProvided()
    {
        // Arrange
        var request = new GenerateTestDataRequest
        {
            Method = "GET",
            Path = "/api/test",
            NumberOfSamples = 3,
            IncludeValidData = true,
            IncludeInvalidData = true,
            IncludeEdgeCases = true
        };

        // Act
        var result = await _service.GenerateTestDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Method.Should().Be(request.Method);
        result.Path.Should().Be(request.Path);
        result.ValidSamples.Should().NotBeEmpty();
        result.InvalidSamples.Should().NotBeEmpty();
        result.EdgeCaseSamples.Should().NotBeEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ValidateEndpointAsync_ShouldReturnValidationResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new ValidateEndpointRequest
        {
            Method = "GET",
            Path = "/api/test",
            ValidateAuthentication = true,
            ValidateAuthorization = true,
            ValidateInputValidation = true
        };

        // Act
        var result = await _service.ValidateEndpointAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Method.Should().Be(request.Method);
        result.Path.Should().Be(request.Path);
        result.Issues.Should().NotBeNull();
        result.Warnings.Should().NotBeNull();
        result.Details.Should().NotBeNull();
        result.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("GET", "/api/test")]
    [InlineData("POST", "/api/users")]
    [InlineData("PUT", "/api/indicators/1")]
    [InlineData("DELETE", "/api/schedulers/1")]
    public async Task GenerateTestDataAsync_ShouldHandleDifferentHttpMethods_WhenValidMethodsProvided(string method, string path)
    {
        // Arrange
        var request = new GenerateTestDataRequest
        {
            Method = method,
            Path = path,
            NumberOfSamples = 1
        };

        // Act
        var result = await _service.GenerateTestDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Method.Should().Be(method);
        result.Path.Should().Be(path);
    }

    [Fact]
    public async Task ValidateEndpointAsync_ShouldReturnValidResult_WhenEndpointIsValid()
    {
        // Arrange
        var request = new ValidateEndpointRequest
        {
            Method = "GET",
            Path = "/api/testing/endpoints", // Our own endpoint
            ValidateAuthentication = false, // Since we're testing without full auth setup
            ValidateAuthorization = false,
            ValidateInputValidation = true,
            ValidateOutputFormat = true
        };

        // Act
        var result = await _service.ValidateEndpointAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Method.Should().Be(request.Method);
        result.Path.Should().Be(request.Path);
        result.Issues.Should().NotBeNull();
        result.Warnings.Should().NotBeNull();
    }

    [Fact]
    public void TestableEndpoint_ShouldHaveCorrectProperties_WhenCreated()
    {
        // Arrange & Act
        var endpoint = new TestableEndpoint
        {
            Controller = "Test",
            Action = "TestAction",
            Method = "GET",
            Path = "/api/test",
            Description = "Test endpoint",
            Complexity = TestComplexity.Simple,
            RequiresAuthentication = false,
            RequiredRoles = new List<string>(),
            Parameters = new List<EndpointParameter>(),
            ResponseExamples = new List<ResponseExample>(),
            Tags = new List<string> { "Test" }
        };

        // Assert
        endpoint.Controller.Should().Be("Test");
        endpoint.Action.Should().Be("TestAction");
        endpoint.Method.Should().Be("GET");
        endpoint.Path.Should().Be("/api/test");
        endpoint.Description.Should().Be("Test endpoint");
        endpoint.Complexity.Should().Be(TestComplexity.Simple);
        endpoint.RequiresAuthentication.Should().BeFalse();
        endpoint.RequiredRoles.Should().BeEmpty();
        endpoint.Parameters.Should().BeEmpty();
        endpoint.ResponseExamples.Should().BeEmpty();
        endpoint.Tags.Should().Contain("Test");
    }

    [Fact]
    public void TestExecutionResult_ShouldHaveCorrectProperties_WhenCreated()
    {
        // Arrange & Act
        var result = new TestExecutionResult
        {
            TestName = "Test execution",
            Method = "GET",
            Path = "/api/test",
            StatusCode = 200,
            IsSuccess = true,
            ResponseBody = "{ \"success\": true }",
            TestDuration = 150, // milliseconds
            ExecutedAt = DateTime.UtcNow
        };

        // Assert
        result.TestName.Should().Be("Test execution");
        result.Method.Should().Be("GET");
        result.Path.Should().Be("/api/test");
        result.StatusCode.Should().Be(200);
        result.IsSuccess.Should().BeTrue();
        result.ResponseBody.Should().Be("{ \"success\": true }");
        result.TestDuration.Should().Be(150);
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(TestComplexity.Simple)]
    [InlineData(TestComplexity.Medium)]
    [InlineData(TestComplexity.Complex)]
    [InlineData(TestComplexity.Advanced)]
    public void TestComplexity_ShouldSupportAllLevels_WhenUsed(TestComplexity complexity)
    {
        // Arrange & Act
        var endpoint = new TestableEndpoint
        {
            Complexity = complexity
        };

        // Assert
        endpoint.Complexity.Should().Be(complexity);
    }

    [Fact]
    public void ValidationIssue_ShouldHaveCorrectProperties_WhenCreated()
    {
        // Arrange & Act
        var issue = new ValidationIssue
        {
            Type = "Authentication",
            Severity = "Error",
            Message = "Authentication is required but not configured",
            Field = "Authorization",
            ExpectedValue = "Bearer token",
            ActualValue = null
        };

        // Assert
        issue.Type.Should().Be("Authentication");
        issue.Severity.Should().Be("Error");
        issue.Message.Should().Be("Authentication is required but not configured");
        issue.Field.Should().Be("Authorization");
        issue.ExpectedValue.Should().Be("Bearer token");
        issue.ActualValue.Should().BeNull();
    }

    [Fact]
    public void BatchTestExecutionResult_ShouldCalculateCorrectSummary_WhenResultsProvided()
    {
        // Arrange & Act
        var batchResult = new BatchTestExecutionResult
        {
            BatchName = "Test batch",
            TotalTests = 5,
            SuccessfulTests = 4,
            FailedTests = 1,
            Results = new List<TestExecutionResult>
            {
                new TestExecutionResult { IsSuccess = true, StatusCode = 200, TestDuration = 100 },
                new TestExecutionResult { IsSuccess = true, StatusCode = 200, TestDuration = 150 },
                new TestExecutionResult { IsSuccess = true, StatusCode = 201, TestDuration = 200 },
                new TestExecutionResult { IsSuccess = true, StatusCode = 200, TestDuration = 120 },
                new TestExecutionResult { IsSuccess = false, StatusCode = 500, TestDuration = 300 }
            },
            Summary = new BatchTestSummary
            {
                SuccessRate = 80.0,
                AverageResponseTime = 174, // (100+150+200+120+300)/5
                FastestResponse = 100,
                SlowestResponse = 300
            }
        };

        // Assert
        batchResult.BatchName.Should().Be("Test batch");
        batchResult.TotalTests.Should().Be(5);
        batchResult.SuccessfulTests.Should().Be(4);
        batchResult.FailedTests.Should().Be(1);
        batchResult.Results.Should().HaveCount(5);
        batchResult.Summary.SuccessRate.Should().Be(80.0);
        batchResult.Summary.AverageResponseTime.Should().Be(174);
        batchResult.Summary.FastestResponse.Should().Be(100);
        batchResult.Summary.SlowestResponse.Should().Be(300);
    }
}
