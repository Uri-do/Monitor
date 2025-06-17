using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using MediatR;
using MonitoringGrid.Api.Controllers;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Api.Models.Testing;
using System.Net.Http;

namespace MonitoringGrid.Api.Tests.Controllers;

/// <summary>
/// Unit tests for ApiTestingController
/// </summary>
public class ApiTestingControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ApiTestingController>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ApiTestingService> _testingServiceMock;
    private readonly ApiTestingController _controller;

    public ApiTestingControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ApiTestingController>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _testingServiceMock = new Mock<ApiTestingService>(
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            Mock.Of<ILogger<ApiTestingService>>(),
            Mock.Of<IServiceProvider>());

        _controller = new ApiTestingController(
            _mediatorMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _testingServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenValidParametersProvided()
    {
        // Act & Assert
        _controller.Should().NotBeNull();
        _controller.Should().BeOfType<ApiTestingController>();
    }

    [Fact]
    public async Task GetTestableEndpoints_ShouldReturnOkResult_WhenEndpointsExist()
    {
        // Arrange
        var expectedEndpoints = new List<TestableEndpoint>
        {
            new TestableEndpoint
            {
                Controller = "Test",
                Action = "TestAction",
                Method = "GET",
                Path = "/api/test",
                Description = "Test endpoint",
                Complexity = TestComplexity.Simple
            }
        };

        _testingServiceMock
            .Setup(x => x.GetTestableEndpointsAsync())
            .ReturnsAsync(expectedEndpoints);

        // Act
        var result = await _controller.GetTestableEndpoints();

        // Assert
        result.Should().BeOfType<ActionResult<List<TestableEndpoint>>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ExecuteTest_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new ExecuteTestRequest
        {
            Method = "GET",
            Path = "/api/test",
            TestName = "Test execution"
        };

        var expectedResult = new TestExecutionResult
        {
            TestName = "Test execution",
            Method = "GET",
            Path = "/api/test",
            StatusCode = 200,
            IsSuccess = true,
            ExecutedAt = DateTime.UtcNow
        };

        _testingServiceMock
            .Setup(x => x.ExecuteTestAsync(It.IsAny<ExecuteTestRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ExecuteTest(request);

        // Assert
        result.Should().BeOfType<ActionResult<TestExecutionResult>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ExecuteBatchTests_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new ExecuteBatchTestRequest
        {
            Tests = new List<ExecuteTestRequest>
            {
                new ExecuteTestRequest
                {
                    Method = "GET",
                    Path = "/api/test1",
                    TestName = "Test 1"
                },
                new ExecuteTestRequest
                {
                    Method = "GET",
                    Path = "/api/test2",
                    TestName = "Test 2"
                }
            },
            BatchName = "Batch test"
        };

        var expectedResult = new BatchTestExecutionResult
        {
            BatchName = "Batch test",
            TotalTests = 2,
            SuccessfulTests = 2,
            FailedTests = 0,
            Results = new List<TestExecutionResult>
            {
                new TestExecutionResult
                {
                    TestName = "Test 1",
                    Method = "GET",
                    Path = "/api/test1",
                    StatusCode = 200,
                    IsSuccess = true
                },
                new TestExecutionResult
                {
                    TestName = "Test 2",
                    Method = "GET",
                    Path = "/api/test2",
                    StatusCode = 200,
                    IsSuccess = true
                }
            }
        };

        _testingServiceMock
            .Setup(x => x.ExecuteBatchTestsAsync(It.IsAny<ExecuteBatchTestRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ExecuteBatchTests(request);

        // Assert
        result.Should().BeOfType<ActionResult<BatchTestExecutionResult>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ExecuteTestSuite_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new ExecuteTestSuiteRequest
        {
            SuiteName = "Full test suite",
            IncludePerformanceTests = true,
            IncludeSecurityTests = true
        };

        var expectedResult = new TestSuiteExecutionResult
        {
            SuiteName = "Full test suite",
            TotalTests = 5,
            SuccessfulTests = 4,
            FailedTests = 1,
            ControllerResults = new List<ControllerTestResult>
            {
                new ControllerTestResult
                {
                    ControllerName = "Test",
                    TotalEndpoints = 5,
                    SuccessfulEndpoints = 4,
                    FailedEndpoints = 1
                }
            }
        };

        _testingServiceMock
            .Setup(x => x.ExecuteTestSuiteAsync(It.IsAny<ExecuteTestSuiteRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ExecuteTestSuite(request);

        // Assert
        result.Should().BeOfType<ActionResult<TestSuiteExecutionResult>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTestHistory_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new GetTestHistoryRequest
        {
            Days = 7,
            PageSize = 10
        };

        var expectedResult = new TestExecutionHistory
        {
            TotalExecutions = 10,
            Page = 1,
            PageSize = 10,
            TotalPages = 1,
            Executions = new List<TestExecutionSummary>
            {
                new TestExecutionSummary
                {
                    Id = "1",
                    TestName = "Test 1",
                    Controller = "Test",
                    Method = "GET",
                    IsSuccess = true,
                    StatusCode = 200
                }
            }
        };

        _testingServiceMock
            .Setup(x => x.GetTestHistoryAsync(It.IsAny<GetTestHistoryRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTestHistory(request);

        // Assert
        result.Should().BeOfType<ActionResult<TestExecutionHistory>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTestStatistics_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new GetTestStatisticsRequest
        {
            Days = 30,
            IncludeTrends = true
        };

        var expectedResult = new TestStatistics
        {
            Overview = new TestOverviewStatistics
            {
                TotalTests = 100,
                SuccessfulTests = 95,
                FailedTests = 5,
                SuccessRate = 95.0
            },
            GeneratedAt = DateTime.UtcNow
        };

        _testingServiceMock
            .Setup(x => x.GetTestStatisticsAsync(It.IsAny<GetTestStatisticsRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTestStatistics(request);

        // Assert
        result.Should().BeOfType<ActionResult<TestStatistics>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GenerateTestData_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new GenerateTestDataRequest
        {
            Method = "POST",
            Path = "/api/test",
            NumberOfSamples = 3
        };

        var expectedResult = new GeneratedTestData
        {
            Method = "POST",
            Path = "/api/test",
            ValidSamples = new List<TestDataSample>
            {
                new TestDataSample
                {
                    Name = "Valid sample",
                    Description = "Valid test data",
                    ExpectedStatusCode = 200
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _testingServiceMock
            .Setup(x => x.GenerateTestDataAsync(It.IsAny<GenerateTestDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GenerateTestData(request);

        // Assert
        result.Should().BeOfType<ActionResult<GeneratedTestData>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ValidateEndpoint_ShouldReturnOkResult_WhenValidRequestProvided()
    {
        // Arrange
        var request = new ValidateEndpointRequest
        {
            Method = "GET",
            Path = "/api/test",
            ValidateAuthentication = true
        };

        var expectedResult = new EndpointValidationResult
        {
            Method = "GET",
            Path = "/api/test",
            IsValid = true,
            Issues = new List<ValidationIssue>(),
            ValidatedAt = DateTime.UtcNow
        };

        _testingServiceMock
            .Setup(x => x.ValidateEndpointAsync(It.IsAny<ValidateEndpointRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ValidateEndpoint(request);

        // Assert
        result.Should().BeOfType<ActionResult<EndpointValidationResult>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }
}
