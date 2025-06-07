using FluentValidation.TestHelper;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Validation;
using MonitoringGrid.Api.Validators;
using MonitoringGrid.Core.Interfaces;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MonitoringGrid.Tests.UnitTests.Validation;

/// <summary>
/// Unit tests for enhanced validation system
/// </summary>
public class EnhancedValidationTests
{
    private readonly CreateKpiRequestValidator _validator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ITestOutputHelper _output;

    public EnhancedValidationTests(ITestOutputHelper output)
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _validator = new CreateKpiRequestValidator(_mockUnitOfWork.Object);
        _output = output;
    }

    [Fact]
    public void ValidKpiRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateKpiRequest
        {
            Indicator = "Valid_KPI_Test",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_ValidProcedure",
            SubjectTemplate = "Alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1, 2 }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
        _output.WriteLine("Valid KPI request passed all validation rules");
    }

    [Theory]
    [InlineData(1, 2, "High priority KPIs (SMS alerts) should not run more frequently than every 5 minutes to avoid spam")]
    [InlineData(1, 1441, "High priority KPIs should run at least once per day")]
    [InlineData(2, 0, "Email-only KPIs should not run more frequently than every minute")]
    [InlineData(2, 10081, "Email-only KPIs should run at least once per week")]
    public void FrequencyValidation_ShouldFailForInvalidFrequencyPriorityCombo(byte priority, int frequency, string expectedError)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Priority = priority;
        request.Frequency = frequency;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Frequency)
              .WithErrorMessage(expectedError);
        _output.WriteLine($"Frequency validation correctly failed for Priority {priority}, Frequency {frequency}");
    }

    [Theory]
    [InlineData("DROP TABLE users; --", "Stored procedure name must be in 'monitoring' or 'stats' schema and contain no dangerous patterns")]
    [InlineData("xp_cmdshell", "Stored procedure name must be in 'monitoring' or 'stats' schema and contain no dangerous patterns")]
    [InlineData("invalid.procedure", "Stored procedure name must be in 'monitoring' or 'stats' schema and contain no dangerous patterns")]
    [InlineData("DELETE FROM table", "Stored procedure name must be in 'monitoring' or 'stats' schema and contain no dangerous patterns")]
    public void StoredProcedureValidation_ShouldFailForDangerousNames(string spName, string expectedError)
    {
        // Arrange
        var request = CreateValidRequest();
        request.SpName = spName;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SpName)
              .WithErrorMessage(expectedError);
        _output.WriteLine($"Stored procedure validation correctly failed for: {spName}");
    }

    [Theory]
    [InlineData("monitoring.usp_ValidProcedure")]
    [InlineData("stats.usp_ValidProcedure")]
    public void StoredProcedureValidation_ShouldPassForValidNames(string spName)
    {
        // Arrange
        var request = CreateValidRequest();
        request.SpName = spName;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SpName);
        _output.WriteLine($"Stored procedure validation correctly passed for: {spName}");
    }

    [Theory]
    [InlineData("Simple alert message", "Subject template must contain {current}, {historical}, and {deviation} placeholders and be safe")]
    [InlineData("Alert: {current} vs {historical}", "Subject template must contain {current}, {historical}, and {deviation} placeholders and be safe")]
    [InlineData("Alert with <script>alert('xss')</script>", "Subject template must contain {current}, {historical}, and {deviation} placeholders and be safe")]
    [InlineData("Alert with javascript:void(0)", "Subject template must contain {current}, {historical}, and {deviation} placeholders and be safe")]
    public void TemplateValidation_ShouldFailForInvalidTemplates(string template, string expectedError)
    {
        // Arrange
        var request = CreateValidRequest();
        request.SubjectTemplate = template;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubjectTemplate)
              .WithErrorMessage(expectedError);
        _output.WriteLine($"Template validation correctly failed for: {template}");
    }

    [Theory]
    [InlineData("Alert: {current} vs {historical} (deviation: {deviation}%)")]
    [InlineData("🚨 Critical: {current}% vs {historical}% (deviation: {deviation}%)")]
    [InlineData("System Alert: Current value {current}, Historical {historical}, Deviation {deviation}%")]
    public void TemplateValidation_ShouldPassForValidTemplates(string template)
    {
        // Arrange
        var request = CreateValidRequest();
        request.SubjectTemplate = template;
        request.DescriptionTemplate = template; // Use same for description

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SubjectTemplate);
        result.ShouldNotHaveValidationErrorFor(x => x.DescriptionTemplate);
        _output.WriteLine($"Template validation correctly passed for: {template}");
    }

    [Theory]
    [InlineData(5, 2, "High-frequency KPIs should have cooldown periods at least equal to their frequency")]
    [InlineData(30, 301, "Cooldown period should not exceed 10 times the execution frequency")]
    [InlineData(60, -1, "Cooldown minutes cannot be negative")]
    public void CooldownValidation_ShouldFailForInvalidCooldowns(int frequency, int cooldown, string expectedError)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Frequency = frequency;
        request.CooldownMinutes = cooldown;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CooldownMinutes)
              .WithErrorMessage(expectedError);
        _output.WriteLine($"Cooldown validation correctly failed for Frequency {frequency}, Cooldown {cooldown}");
    }

    [Theory]
    [InlineData(5, 30, "High-frequency KPIs (≤5 min) should use data windows ≤60 minutes")]
    [InlineData(30, 15, "Data window should be at least equal to the execution frequency")]
    [InlineData(1440, 720, "Daily KPIs should use data windows of at least 24 hours")]
    [InlineData(10, 1001, "Data window should not exceed 100 times the execution frequency")]
    public void DataWindowValidation_ShouldFailForInvalidDataWindows(int frequency, int lastMinutes, string expectedError)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Frequency = frequency;
        request.LastMinutes = lastMinutes;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastMinutes)
              .WithErrorMessage(expectedError);
        _output.WriteLine($"Data window validation correctly failed for Frequency {frequency}, LastMinutes {lastMinutes}");
    }

    [Theory]
    [InlineData(30, 60)] // 30 min frequency, 60 min data window
    [InlineData(60, 120)] // 1 hour frequency, 2 hour data window
    [InlineData(1440, 1440)] // Daily frequency, daily data window
    [InlineData(5, 30)] // 5 min frequency, 30 min data window
    public void DataWindowValidation_ShouldPassForValidDataWindows(int frequency, int lastMinutes)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Frequency = frequency;
        request.LastMinutes = lastMinutes;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastMinutes);
        _output.WriteLine($"Data window validation correctly passed for Frequency {frequency}, LastMinutes {lastMinutes}");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void RequiredFields_ShouldFailForEmptyValues(string? value)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Indicator = value!;
        request.Owner = value!;
        request.SpName = value!;
        request.SubjectTemplate = value!;
        request.DescriptionTemplate = value!;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Indicator);
        result.ShouldHaveValidationErrorFor(x => x.Owner);
        result.ShouldHaveValidationErrorFor(x => x.SpName);
        result.ShouldHaveValidationErrorFor(x => x.SubjectTemplate);
        result.ShouldHaveValidationErrorFor(x => x.DescriptionTemplate);
        _output.WriteLine($"Required field validation correctly failed for empty/null values");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public void DeviationValidation_ShouldFailForInvalidValues(decimal deviation)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Deviation = deviation;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Deviation);
        _output.WriteLine($"Deviation validation correctly failed for value: {deviation}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10081)] // More than 7 days
    public void FrequencyValidation_ShouldFailForInvalidRanges(int frequency)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Frequency = frequency;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Frequency);
        _output.WriteLine($"Frequency validation correctly failed for value: {frequency}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(255)]
    public void PriorityValidation_ShouldFailForInvalidValues(byte priority)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Priority = priority;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (priority < 1 || priority > 2)
        {
            result.ShouldHaveValidationErrorFor(x => x.Priority);
            _output.WriteLine($"Priority validation correctly failed for value: {priority}");
        }
        else
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Priority);
            _output.WriteLine($"Priority validation correctly passed for value: {priority}");
        }
    }

    [Fact]
    public void ContactIds_ShouldFailForEmptyList()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ContactIds = new List<int>();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContactIds)
              .WithErrorMessage("At least one contact must be assigned");
        _output.WriteLine("Contact IDs validation correctly failed for empty list");
    }

    private static CreateKpiRequest CreateValidRequest()
    {
        return new CreateKpiRequest
        {
            Indicator = "Valid_Test_KPI",
            Owner = "test@example.com",
            Priority = 2,
            Frequency = 30,
            LastMinutes = 60,
            Deviation = 5.0m,
            SpName = "monitoring.usp_ValidProcedure",
            SubjectTemplate = "Alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1, 2 }
        };
    }
}
