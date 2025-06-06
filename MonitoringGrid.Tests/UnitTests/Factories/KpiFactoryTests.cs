using FluentAssertions;
using MonitoringGrid.Core.Factories;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.Factories;

public class KpiFactoryTests
{
    private readonly KpiFactory _factory;

    public KpiFactoryTests()
    {
        _factory = new KpiFactory();
    }

    [Fact]
    public void CreateKpi_WithValidParameters_ShouldCreateKpi()
    {
        // Arrange
        const string indicator = "Test KPI";
        const string owner = "TestOwner";
        const byte priority = 1;
        const int frequency = 60;
        const decimal deviation = 10.5m;
        const string spName = "TestStoredProcedure";
        const string subjectTemplate = "Alert: {Indicator}";
        const string descriptionTemplate = "KPI {Indicator} has deviated by {Deviation}%";

        // Act
        var kpi = _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate);

        // Assert
        kpi.Should().NotBeNull();
        kpi.Indicator.Should().Be(indicator);
        kpi.Owner.Should().Be(owner);
        kpi.Priority.Should().Be(priority);
        kpi.Frequency.Should().Be(frequency);
        kpi.Deviation.Should().Be(deviation);
        kpi.SpName.Should().Be(spName);
        kpi.SubjectTemplate.Should().Be(subjectTemplate);
        kpi.DescriptionTemplate.Should().Be(descriptionTemplate);
        kpi.IsActive.Should().BeTrue();
        kpi.CooldownMinutes.Should().Be(30);
        kpi.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        kpi.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("", "Owner", 1, 60, 10, "SP", "Subject", "Description")]
    [InlineData("   ", "Owner", 1, 60, 10, "SP", "Subject", "Description")]
    [InlineData(null, "Owner", 1, 60, 10, "SP", "Subject", "Description")]
    public void CreateKpi_WithInvalidIndicator_ShouldThrowArgumentException(string indicator, string owner, byte priority, int frequency, decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate));
    }

    [Theory]
    [InlineData("Indicator", "", 1, 60, 10, "SP", "Subject", "Description")]
    [InlineData("Indicator", "   ", 1, 60, 10, "SP", "Subject", "Description")]
    [InlineData("Indicator", null, 1, 60, 10, "SP", "Subject", "Description")]
    public void CreateKpi_WithInvalidOwner_ShouldThrowArgumentException(string indicator, string owner, byte priority, int frequency, decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate));
    }

    [Theory]
    [InlineData("Indicator", "Owner", 0, 60, 10, "SP", "Subject", "Description")]
    [InlineData("Indicator", "Owner", 3, 60, 10, "SP", "Subject", "Description")]
    public void CreateKpi_WithInvalidPriority_ShouldThrowArgumentException(string indicator, string owner, byte priority, int frequency, decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate));
    }

    [Theory]
    [InlineData("Indicator", "Owner", 1, 0, 10, "SP", "Subject", "Description")]
    [InlineData("Indicator", "Owner", 1, -5, 10, "SP", "Subject", "Description")]
    public void CreateKpi_WithInvalidFrequency_ShouldThrowArgumentException(string indicator, string owner, byte priority, int frequency, decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate));
    }

    [Theory]
    [InlineData("Indicator", "Owner", 1, 60, -1, "SP", "Subject", "Description")]
    [InlineData("Indicator", "Owner", 1, 60, 101, "SP", "Subject", "Description")]
    public void CreateKpi_WithInvalidDeviation_ShouldThrowArgumentException(string indicator, string owner, byte priority, int frequency, decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate));
    }

    [Theory]
    [InlineData("Indicator", "Owner", 1, 60, 10, "", "Subject", "Description")]
    [InlineData("Indicator", "Owner", 1, 60, 10, "   ", "Subject", "Description")]
    [InlineData("Indicator", "Owner", 1, 60, 10, null, "Subject", "Description")]
    public void CreateKpi_WithInvalidSpName_ShouldThrowArgumentException(string indicator, string owner, byte priority, int frequency, decimal deviation, string spName, string subjectTemplate, string descriptionTemplate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateKpi(indicator, owner, priority, frequency, deviation, spName, subjectTemplate, descriptionTemplate));
    }

    [Fact]
    public void CreateFromTemplate_WithValidTemplate_ShouldCreateKpi()
    {
        // Arrange
        var template = new KpiTemplate
        {
            Indicator = "Template KPI",
            Priority = 2,
            Frequency = 120,
            Deviation = 15,
            SpName = "TemplateSP",
            SubjectTemplate = "Template Subject",
            DescriptionTemplate = "Template Description"
        };
        const string owner = "TemplateOwner";

        // Act
        var kpi = _factory.CreateFromTemplate(template, owner);

        // Assert
        kpi.Should().NotBeNull();
        kpi.Indicator.Should().Be(template.Indicator);
        kpi.Owner.Should().Be(owner);
        kpi.Priority.Should().Be(template.Priority);
        kpi.Frequency.Should().Be(template.Frequency);
        kpi.Deviation.Should().Be(template.Deviation);
        kpi.SpName.Should().Be(template.SpName);
        kpi.SubjectTemplate.Should().Be(template.SubjectTemplate);
        kpi.DescriptionTemplate.Should().Be(template.DescriptionTemplate);
    }

    [Fact]
    public void CreateCopy_WithValidSourceKpi_ShouldCreateCopy()
    {
        // Arrange
        var sourceKpi = _factory.CreateKpi("Source KPI", "SourceOwner", 1, 60, 10, "SourceSP", "Source Subject", "Source Description");
        const string newIndicator = "Copied KPI";
        const string newOwner = "CopyOwner";

        // Act
        var copiedKpi = _factory.CreateCopy(sourceKpi, newIndicator, newOwner);

        // Assert
        copiedKpi.Should().NotBeNull();
        copiedKpi.Indicator.Should().Be(newIndicator);
        copiedKpi.Owner.Should().Be(newOwner);
        copiedKpi.Priority.Should().Be(sourceKpi.Priority);
        copiedKpi.Frequency.Should().Be(sourceKpi.Frequency);
        copiedKpi.Deviation.Should().Be(sourceKpi.Deviation);
        copiedKpi.SpName.Should().Be(sourceKpi.SpName);
        copiedKpi.SubjectTemplate.Should().Be(sourceKpi.SubjectTemplate);
        copiedKpi.DescriptionTemplate.Should().Be(sourceKpi.DescriptionTemplate);
    }

    [Fact]
    public void CreateCopy_WithoutNewOwner_ShouldUseSourceOwner()
    {
        // Arrange
        var sourceKpi = _factory.CreateKpi("Source KPI", "SourceOwner", 1, 60, 10, "SourceSP", "Source Subject", "Source Description");
        const string newIndicator = "Copied KPI";

        // Act
        var copiedKpi = _factory.CreateCopy(sourceKpi, newIndicator);

        // Assert
        copiedKpi.Should().NotBeNull();
        copiedKpi.Indicator.Should().Be(newIndicator);
        copiedKpi.Owner.Should().Be(sourceKpi.Owner);
    }

    [Fact]
    public void CreateKpi_ShouldTrimWhitespace()
    {
        // Arrange
        const string indicator = "  Test KPI  ";
        const string owner = "  TestOwner  ";
        const string spName = "  TestSP  ";
        const string subjectTemplate = "  Subject  ";
        const string descriptionTemplate = "  Description  ";

        // Act
        var kpi = _factory.CreateKpi(indicator, owner, 1, 60, 10, spName, subjectTemplate, descriptionTemplate);

        // Assert
        kpi.Indicator.Should().Be("Test KPI");
        kpi.Owner.Should().Be("TestOwner");
        kpi.SpName.Should().Be("TestSP");
        kpi.SubjectTemplate.Should().Be("Subject");
        kpi.DescriptionTemplate.Should().Be("Description");
    }
}
