using FluentAssertions;
using MonitoringGrid.Core.ValueObjects;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.ValueObjects;

public class DeviationPercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5.5)]
    [InlineData(25.75)]
    [InlineData(100)]
    public void Constructor_WithValidPercentage_ShouldCreateDeviationPercentage(decimal percentage)
    {
        // Act
        var deviation = new DeviationPercentage(percentage);

        // Assert
        deviation.Value.Should().Be(Math.Round(percentage, 2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Constructor_WithNegativePercentage_ShouldThrowArgumentException(decimal percentage)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DeviationPercentage(percentage));
    }

    [Theory]
    [InlineData(0, "Minimal")]
    [InlineData(4.9, "Minimal")]
    [InlineData(5, "Low")]
    [InlineData(9.9, "Low")]
    [InlineData(10, "Medium")]
    [InlineData(24.9, "Medium")]
    [InlineData(25, "High")]
    [InlineData(49.9, "High")]
    [InlineData(50, "Critical")]
    [InlineData(100, "Critical")]
    public void GetSeverityLevel_ShouldReturnCorrectSeverity(decimal percentage, string expectedSeverity)
    {
        // Arrange
        var deviation = new DeviationPercentage(percentage);

        // Act
        var severity = deviation.GetSeverityLevel();

        // Assert
        severity.Should().Be(expectedSeverity);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(24.9, false)]
    [InlineData(25, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    public void RequiresImmediateAttention_ShouldReturnCorrectResult(decimal percentage, bool expected)
    {
        // Arrange
        var deviation = new DeviationPercentage(percentage);

        // Act
        var result = deviation.RequiresImmediateAttention();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(49.9, false)]
    [InlineData(50, true)]
    [InlineData(75, true)]
    [InlineData(100, true)]
    public void RequiresSmsAlert_ShouldReturnCorrectResult(decimal percentage, bool expected)
    {
        // Arrange
        var deviation = new DeviationPercentage(percentage);

        // Act
        var result = deviation.RequiresSmsAlert();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "#90EE90")]
    [InlineData(4.9, "#90EE90")]
    [InlineData(5, "#32CD32")]
    [InlineData(10, "#FFD700")]
    [InlineData(25, "#FF8C00")]
    [InlineData(50, "#FF0000")]
    public void GetColorCode_ShouldReturnCorrectColor(decimal percentage, string expectedColor)
    {
        // Arrange
        var deviation = new DeviationPercentage(percentage);

        // Act
        var color = deviation.GetColorCode();

        // Assert
        color.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData(100, 80, 25)]
    [InlineData(80, 100, 20)]
    [InlineData(50, 0, 0)] // Division by zero should return 0
    [InlineData(100, 100, 0)]
    public void Calculate_ShouldReturnCorrectDeviation(decimal current, decimal historical, decimal expected)
    {
        // Act
        var deviation = DeviationPercentage.Calculate(current, historical);

        // Assert
        deviation.Value.Should().Be(expected);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ShouldReturnValue()
    {
        // Arrange
        var deviation = new DeviationPercentage(25.5m);

        // Act
        decimal result = deviation;

        // Assert
        result.Should().Be(25.5m);
    }

    [Fact]
    public void ExplicitConversion_FromDecimal_ShouldCreateDeviationPercentage()
    {
        // Arrange
        const decimal percentage = 25.5m;

        // Act
        var deviation = (DeviationPercentage)percentage;

        // Assert
        deviation.Value.Should().Be(25.5m);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedPercentage()
    {
        // Arrange
        var deviation = new DeviationPercentage(25.567m);

        // Act
        var result = deviation.ToString();

        // Assert
        result.Should().Be("25.57%");
    }

    [Fact]
    public void Constructor_ShouldRoundToTwoDecimals()
    {
        // Arrange
        const decimal percentage = 25.6789m;

        // Act
        var deviation = new DeviationPercentage(percentage);

        // Assert
        deviation.Value.Should().Be(25.68m);
    }
}
