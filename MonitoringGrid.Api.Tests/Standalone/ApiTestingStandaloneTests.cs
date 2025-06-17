using Xunit;
using FluentAssertions;
using MonitoringGrid.Api.Models.Testing;
using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.Tests.Standalone;

/// <summary>
/// Standalone tests for API Testing models and functionality that don't require Infrastructure dependencies
/// </summary>
public class ApiTestingStandaloneTests
{
    [Fact]
    public void TestableEndpoint_ShouldHaveCorrectDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var endpoint = new TestableEndpoint();

        // Assert
        endpoint.Controller.Should().Be(string.Empty);
        endpoint.Action.Should().Be(string.Empty);
        endpoint.Method.Should().Be(string.Empty);
        endpoint.Path.Should().Be(string.Empty);
        endpoint.Description.Should().Be(string.Empty);
        endpoint.Parameters.Should().NotBeNull().And.BeEmpty();
        endpoint.RequiredRoles.Should().NotBeNull().And.BeEmpty();
        endpoint.ResponseExamples.Should().NotBeNull().And.BeEmpty();
        endpoint.Tags.Should().NotBeNull().And.BeEmpty();
        endpoint.RequiresAuthentication.Should().BeFalse();
        endpoint.Complexity.Should().Be(default(TestComplexity));
    }

    [Fact]
    public void TestableEndpoint_ShouldSetPropertiesCorrectly_WhenAssigned()
    {
        // Arrange & Act
        var endpoint = new TestableEndpoint
        {
            Controller = "ApiTesting",
            Action = "GetTestableEndpoints",
            Method = "GET",
            Path = "/api/testing/endpoints",
            Description = "Get all testable endpoints",
            Complexity = TestComplexity.Simple,
            RequiresAuthentication = true,
            RequiredRoles = new List<string> { "Admin" },
            Tags = new List<string> { "Testing", "API" }
        };

        // Assert
        endpoint.Controller.Should().Be("ApiTesting");
        endpoint.Action.Should().Be("GetTestableEndpoints");
        endpoint.Method.Should().Be("GET");
        endpoint.Path.Should().Be("/api/testing/endpoints");
        endpoint.Description.Should().Be("Get all testable endpoints");
        endpoint.Complexity.Should().Be(TestComplexity.Simple);
        endpoint.RequiresAuthentication.Should().BeTrue();
        endpoint.RequiredRoles.Should().Contain("Admin");
        endpoint.Tags.Should().Contain("Testing").And.Contain("API");
    }

    [Theory]
    [InlineData(TestComplexity.Simple)]
    [InlineData(TestComplexity.Medium)]
    [InlineData(TestComplexity.Complex)]
    [InlineData(TestComplexity.Advanced)]
    public void TestComplexity_ShouldSupportAllValues_WhenUsed(TestComplexity complexity)
    {
        // Arrange & Act
        var endpoint = new TestableEndpoint { Complexity = complexity };

        // Assert
        endpoint.Complexity.Should().Be(complexity);
    }

    [Fact]
    public void ExecuteTestRequest_ShouldValidateRequiredFields_WhenValidated()
    {
        // Arrange
        var request = new ExecuteTestRequest
        {
            Method = "GET",
            Path = "/api/test"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(request);
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void ExecuteTestRequest_ShouldFailValidation_WhenRequiredFieldsMissing()
    {
        // Arrange
        var request = new ExecuteTestRequest(); // Missing required Method and Path

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(request);
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("Method"));
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("Path"));
    }

    [Fact]
    public void TestExecutionResult_ShouldHaveCorrectDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var result = new TestExecutionResult();

        // Assert
        result.TestName.Should().Be(string.Empty);
        result.Method.Should().Be(string.Empty);
        result.Path.Should().Be(string.Empty);
        result.StatusCode.Should().Be(0);
        result.IsSuccess.Should().BeFalse();
        result.ResponseBody.Should().BeNull();
        result.ResponseHeaders.Should().BeNull();
        result.TestDuration.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
        result.PerformanceMetrics.Should().BeNull();
        result.ValidationResult.Should().BeNull();
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.ExecutedBy.Should().BeNull();
    }

    [Fact]
    public void TestExecutionResult_ShouldSetPropertiesCorrectly_WhenAssigned()
    {
        // Arrange
        var executedAt = DateTime.UtcNow;
        
        // Act
        var result = new TestExecutionResult
        {
            TestName = "Test API endpoint",
            Method = "GET",
            Path = "/api/test",
            StatusCode = 200,
            IsSuccess = true,
            ResponseBody = "{ \"success\": true }",
            TestDuration = 150,
            ExecutedAt = executedAt,
            ExecutedBy = "TestUser"
        };

        // Assert
        result.TestName.Should().Be("Test API endpoint");
        result.Method.Should().Be("GET");
        result.Path.Should().Be("/api/test");
        result.StatusCode.Should().Be(200);
        result.IsSuccess.Should().BeTrue();
        result.ResponseBody.Should().Be("{ \"success\": true }");
        result.TestDuration.Should().Be(150);
        result.ExecutedAt.Should().Be(executedAt);
        result.ExecutedBy.Should().Be("TestUser");
    }

    [Fact]
    public void BatchTestExecutionResult_ShouldCalculateCorrectMetrics_WhenResultsProvided()
    {
        // Arrange & Act
        var batchResult = new BatchTestExecutionResult
        {
            BatchName = "API Test Batch",
            TotalTests = 10,
            SuccessfulTests = 8,
            FailedTests = 2,
            SkippedTests = 0,
            TotalDuration = TimeSpan.FromSeconds(5),
            ExecutedAt = DateTime.UtcNow.ToString(),
            ExecutedBy = "TestUser"
        };

        // Assert
        batchResult.BatchName.Should().Be("API Test Batch");
        batchResult.TotalTests.Should().Be(10);
        batchResult.SuccessfulTests.Should().Be(8);
        batchResult.FailedTests.Should().Be(2);
        batchResult.SkippedTests.Should().Be(0);
        batchResult.TotalDuration.Should().Be(TimeSpan.FromSeconds(5));
        batchResult.ExecutedBy.Should().Be("TestUser");
    }

    [Fact]
    public void ValidationIssue_ShouldStoreCorrectInformation_WhenCreated()
    {
        // Arrange & Act
        var issue = new ValidationIssue
        {
            Type = "Authentication",
            Severity = "Error",
            Message = "Missing authentication header",
            Field = "Authorization",
            ExpectedValue = "Bearer token",
            ActualValue = null
        };

        // Assert
        issue.Type.Should().Be("Authentication");
        issue.Severity.Should().Be("Error");
        issue.Message.Should().Be("Missing authentication header");
        issue.Field.Should().Be("Authorization");
        issue.ExpectedValue.Should().Be("Bearer token");
        issue.ActualValue.Should().BeNull();
    }

    [Fact]
    public void EndpointParameter_ShouldHaveCorrectProperties_WhenCreated()
    {
        // Arrange & Act
        var parameter = new EndpointParameter
        {
            Name = "id",
            Type = "long",
            Source = "Route",
            IsRequired = true,
            DefaultValue = null,
            Description = "The unique identifier",
            ValidationRules = "Must be positive integer"
        };

        // Assert
        parameter.Name.Should().Be("id");
        parameter.Type.Should().Be("long");
        parameter.Source.Should().Be("Route");
        parameter.IsRequired.Should().BeTrue();
        parameter.DefaultValue.Should().BeNull();
        parameter.Description.Should().Be("The unique identifier");
        parameter.ValidationRules.Should().Be("Must be positive integer");
    }

    [Fact]
    public void TestPerformanceMetrics_ShouldStoreCorrectMetrics_WhenCreated()
    {
        // Arrange & Act
        var metrics = new TestPerformanceMetrics
        {
            ResponseTime = TimeSpan.FromMilliseconds(150),
            ResponseSizeBytes = 1024,
            DnsLookupTime = TimeSpan.FromMilliseconds(5),
            ConnectionTime = TimeSpan.FromMilliseconds(10),
            SslHandshakeTime = TimeSpan.FromMilliseconds(20),
            TimeToFirstByte = TimeSpan.FromMilliseconds(100),
            ContentDownloadTime = TimeSpan.FromMilliseconds(50),
            RequestsPerSecond = 100.5,
            MemoryUsage = 2048,
            CpuUsage = 15.5
        };

        // Assert
        metrics.ResponseTime.Should().Be(TimeSpan.FromMilliseconds(150));
        metrics.ResponseSizeBytes.Should().Be(1024);
        metrics.DnsLookupTime.Should().Be(TimeSpan.FromMilliseconds(5));
        metrics.ConnectionTime.Should().Be(TimeSpan.FromMilliseconds(10));
        metrics.SslHandshakeTime.Should().Be(TimeSpan.FromMilliseconds(20));
        metrics.TimeToFirstByte.Should().Be(TimeSpan.FromMilliseconds(100));
        metrics.ContentDownloadTime.Should().Be(TimeSpan.FromMilliseconds(50));
        metrics.RequestsPerSecond.Should().Be(100.5);
        metrics.MemoryUsage.Should().Be(2048);
        metrics.CpuUsage.Should().Be(15.5);
    }

    [Fact]
    public void GenerateTestDataRequest_ShouldValidateRequiredFields_WhenValidated()
    {
        // Arrange
        var request = new GenerateTestDataRequest
        {
            Method = "POST",
            Path = "/api/users",
            NumberOfSamples = 5,
            IncludeValidData = true,
            IncludeInvalidData = true,
            IncludeEdgeCases = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(request);
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void TestStatistics_ShouldHaveCorrectStructure_WhenCreated()
    {
        // Arrange & Act
        var statistics = new TestStatistics
        {
            Overview = new TestOverviewStatistics
            {
                TotalTests = 100,
                SuccessfulTests = 95,
                FailedTests = 5,
                SuccessRate = 95.0,
                AverageResponseTime = 150,
                TotalEndpoints = 20,
                TestedEndpoints = 18,
                EndpointCoverage = 90.0
            },
            ControllerStats = new List<ControllerStatistics>(),
            EndpointStats = new List<EndpointStatistics>(),
            Performance = new PerformanceStatistics
            {
                AverageResponseTime = 150,
                MedianResponseTime = 120,
                P95ResponseTime = 300,
                P99ResponseTime = 500,
                FastestResponse = 50,
                SlowestResponse = 1000,
                RequestsPerSecond = 50.5,
                AverageResponseSize = 2048
            },
            GeneratedAt = DateTime.UtcNow.ToString()
        };

        // Assert
        statistics.Overview.Should().NotBeNull();
        statistics.Overview.TotalTests.Should().Be(100);
        statistics.Overview.SuccessRate.Should().Be(95.0);
        statistics.ControllerStats.Should().NotBeNull();
        statistics.EndpointStats.Should().NotBeNull();
        statistics.Performance.Should().NotBeNull();
        statistics.Performance.AverageResponseTime.Should().Be(150);
        statistics.Performance.RequestsPerSecond.Should().Be(50.5);
    }

    [Theory]
    [InlineData("GET", "/api/indicators")]
    [InlineData("POST", "/api/indicators")]
    [InlineData("PUT", "/api/indicators/1")]
    [InlineData("DELETE", "/api/indicators/1")]
    [InlineData("GET", "/api/testing/endpoints")]
    [InlineData("POST", "/api/testing/execute")]
    public void ExecuteTestRequest_ShouldSupportDifferentHttpMethods_WhenCreated(string method, string path)
    {
        // Arrange & Act
        var request = new ExecuteTestRequest
        {
            Method = method,
            Path = path,
            TestName = $"Test {method} {path}",
            IncludePerformanceMetrics = true,
            IncludeResponseValidation = true,
            TimeoutSeconds = 30
        };

        // Assert
        request.Method.Should().Be(method);
        request.Path.Should().Be(path);
        request.TestName.Should().Be($"Test {method} {path}");
        request.IncludePerformanceMetrics.Should().BeTrue();
        request.IncludeResponseValidation.Should().BeTrue();
        request.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void EndpointValidationResult_ShouldTrackValidationStatus_WhenCreated()
    {
        // Arrange & Act
        var validationResult = new EndpointValidationResult
        {
            Method = "GET",
            Path = "/api/test",
            IsValid = false,
            Issues = new List<ValidationIssue>
            {
                new ValidationIssue
                {
                    Type = "Security",
                    Severity = "Error",
                    Message = "Missing authentication"
                }
            },
            Warnings = new List<ValidationIssue>
            {
                new ValidationIssue
                {
                    Type = "Performance",
                    Severity = "Warning",
                    Message = "Response time exceeds recommended threshold"
                }
            },
            Details = new EndpointValidationDetails
            {
                HasAuthentication = false,
                HasAuthorization = false,
                HasInputValidation = true,
                HasOutputValidation = true,
                HasErrorHandling = true,
                HasDocumentation = true
            },
            ValidatedAt = DateTime.UtcNow.ToString()
        };

        // Assert
        validationResult.Method.Should().Be("GET");
        validationResult.Path.Should().Be("/api/test");
        validationResult.IsValid.Should().BeFalse();
        validationResult.Issues.Should().HaveCount(1);
        validationResult.Issues.First().Type.Should().Be("Security");
        validationResult.Warnings.Should().HaveCount(1);
        validationResult.Warnings.First().Type.Should().Be("Performance");
        validationResult.Details.HasAuthentication.Should().BeFalse();
        validationResult.Details.HasInputValidation.Should().BeTrue();
    }
}
