# MonitoringGrid.Tests

Working test suite for the MonitoringGrid Worker Controller, providing a foundation for testing the worker control functionality.

## 🏗️ Test Architecture

The test suite provides a simple, working foundation that can be extended:

```
MonitoringGrid.Tests/
├── SimpleWorkerTests.cs        # Basic working tests for Worker Controller
├── README.md                   # This documentation
└── MonitoringGrid.Tests.csproj # Test project configuration
```

## ✅ Current Status

**WORKING**: The test project successfully compiles and runs with 12 passing tests covering:
- Basic test framework functionality
- Worker Controller instantiation
- Test data creation
- Mock framework usage
- Performance measurement
- Async operations
- Exception handling
- Memory management
- Concurrency testing

## 🎯 Test Coverage

### Current Test Coverage

| Test Category | Status | Count | Description |
|---------------|--------|-------|-------------|
| **Basic Framework Tests** | ✅ Working | 12 tests | Foundation tests that verify the testing infrastructure works correctly |
| **Worker Controller Tests** | 🚧 Foundation | 1 test | Basic instantiation test for WorkerController |
| **Performance Tests** | ✅ Working | 3 tests | Basic performance measurement and memory testing |
| **Async Tests** | ✅ Working | 1 test | Async operation testing |
| **Exception Tests** | ✅ Working | 1 test | Exception handling verification |
| **Concurrency Tests** | ✅ Working | 1 test | Basic parallel execution testing |

### Test Categories

#### 🔧 Foundation Tests (SimpleWorkerTests.cs)
- **Framework Validation**: Ensures xUnit, Moq, and FluentAssertions work correctly
- **Controller Instantiation**: Verifies WorkerController can be created with mocked dependencies
- **Test Data Creation**: Validates test data builders for Worker DTOs
- **Mock Framework**: Confirms mocking infrastructure is functional
- **Performance Measurement**: Basic timing and memory usage validation
- **Async Operations**: Async/await pattern testing
- **Exception Handling**: Exception throwing and catching verification
- **Concurrency**: Basic parallel execution testing

**Coverage**: 12 working test methods covering core testing infrastructure

## 🚀 Running Tests

### Prerequisites

```bash
# Ensure .NET 8 SDK is installed
dotnet --version

# Restore packages
dotnet restore MonitoringGrid.Tests
```

### Running All Tests

```bash
# Run all tests
dotnet test MonitoringGrid.Tests

# Run with detailed output
dotnet test MonitoringGrid.Tests --verbosity normal

# Run with code coverage
dotnet test MonitoringGrid.Tests --collect:"XPlat Code Coverage"
```

### Running Specific Test Categories

```bash
# Unit tests only
dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~Controllers"

# Integration tests only
dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~Integration"

# Performance tests only
dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~Performance"
```

### Running Individual Test Methods

```bash
# Run specific test method
dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~SimpleWorkerTests.BasicTest_ShouldPass"

# Run all performance tests
dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~PerformanceTest"

# Run all async tests
dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~AsyncTest"
```

## 📊 Test Reports

### Code Coverage Reports

```bash
# Generate coverage report
dotnet test MonitoringGrid.Tests --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Install report generator (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML coverage report
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
```

### Performance Test Reports

Performance tests using NBomber automatically generate reports in:
- `load_test_reports/` - Load test results
- `stress_test_reports/` - Stress test results

Reports include:
- HTML dashboard with charts and metrics
- CSV data for further analysis
- Response time percentiles
- Throughput statistics

## 🛠️ Test Configuration

### Test Settings

Key configuration options for tests:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "InMemory database for testing"
  },
  "WorkerServices": {
    "EnableWorkerServices": true  // Integrated mode for integration tests
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "MonitoringGrid": "Information"
    }
  }
}
```

### Test Data

Tests use a combination of:
- **Bogus**: For generating realistic fake data
- **AutoFixture**: For creating test objects with random values
- **TestDataBuilder**: Custom builder for domain-specific test data
- **In-Memory Database**: For integration tests requiring data persistence

## 🔍 Test Utilities

### TestBase Class

Provides common functionality for all tests:
- Action result assertions
- Mock verification helpers
- Performance measurement utilities
- Exception testing helpers
- Test output formatting

### TestDataBuilder Class

Generates test data for:
- Worker DTOs (requests/responses)
- Core entities (Indicators, Contacts)
- Test scenarios (valid/invalid/edge cases)
- Random data generation

### WorkerControllerTestFixture

Comprehensive test fixture providing:
- Mocked dependencies (ILogger, IConfiguration, etc.)
- Service setup and configuration
- Verification helpers
- Test data seeding

## 📈 Performance Benchmarks

### Expected Performance Metrics

| Operation | Average Response Time | 95th Percentile | Throughput |
|-----------|----------------------|-----------------|------------|
| GetStatus | < 50ms | < 100ms | > 100 req/sec |
| GetTrackedProcesses | < 30ms | < 80ms | > 150 req/sec |
| ExecuteIndicator | < 200ms | < 500ms | > 50 req/sec |

### Load Test Scenarios

1. **Normal Load**: 10 req/sec for 30 seconds
2. **High Load**: 50 req/sec ramping over 60 seconds
3. **Stress Test**: Ramping to 100+ req/sec
4. **Concurrency**: 20 concurrent threads

## 🐛 Debugging Tests

### Common Issues

1. **Test Database Issues**
   ```bash
   # Clear test databases
   dotnet ef database drop --project MonitoringGrid.Infrastructure --startup-project MonitoringGrid.Api
   ```

2. **Port Conflicts in Integration Tests**
   - Tests use random ports to avoid conflicts
   - Check for hanging test processes

3. **Performance Test Failures**
   - Performance tests may fail on slower machines
   - Adjust timeout values in test configuration

### Test Debugging

```bash
# Run tests with debugging
dotnet test MonitoringGrid.Tests --logger "console;verbosity=detailed"

# Run specific failing test
dotnet test MonitoringGrid.Tests --filter "MethodName~SpecificTestName"
```

## 🔄 Continuous Integration

### GitHub Actions Integration

Tests are automatically run in CI/CD pipeline:

```yaml
- name: Run Unit Tests
  run: dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~Controllers" --logger trx

- name: Run Integration Tests  
  run: dotnet test MonitoringGrid.Tests --filter "FullyQualifiedName~Integration" --logger trx

- name: Generate Coverage Report
  run: dotnet test MonitoringGrid.Tests --collect:"XPlat Code Coverage"
```

### Quality Gates

- **Unit Test Coverage**: > 80%
- **Integration Test Coverage**: > 60%
- **Performance Tests**: Must pass on CI environment
- **Zero Test Failures**: All tests must pass for deployment

## 📝 Contributing to Tests

### Adding New Tests

1. **Unit Tests**: Add to `Controllers/WorkerControllerTests.cs`
2. **Integration Tests**: Add to `Integration/WorkerControllerIntegrationTests.cs`
3. **Performance Tests**: Add to `Performance/WorkerControllerPerformanceTests.cs`

### Test Naming Convention

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act  
    // Assert
}
```

### Best Practices

- Use descriptive test names
- Follow Arrange-Act-Assert pattern
- Include test output for debugging
- Mock external dependencies
- Test both success and failure scenarios
- Include performance assertions where appropriate

## 📚 Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [NBomber Documentation](https://nbomber.com/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
