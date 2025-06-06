using FluentAssertions;
using MonitoringGrid.Core.ValueObjects;
using Xunit;

namespace MonitoringGrid.Tests.UnitTests.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("admin@subdomain.example.org")]
    public void Constructor_WithValidEmail_ShouldCreateEmailAddress(string email)
    {
        // Act
        var emailAddress = new EmailAddress(email);

        // Assert
        emailAddress.Value.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithNullOrEmptyEmail_ShouldThrowArgumentException(string email)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EmailAddress(email));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user..name@domain.com")]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string email)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EmailAddress(email));
    }

    [Fact]
    public void GetDomain_ShouldReturnCorrectDomain()
    {
        // Arrange
        var emailAddress = new EmailAddress("test@example.com");

        // Act
        var domain = emailAddress.GetDomain();

        // Assert
        domain.Should().Be("example.com");
    }

    [Fact]
    public void GetLocalPart_ShouldReturnCorrectLocalPart()
    {
        // Arrange
        var emailAddress = new EmailAddress("test.user@example.com");

        // Act
        var localPart = emailAddress.GetLocalPart();

        // Assert
        localPart.Should().Be("test.user");
    }

    [Theory]
    [InlineData("user@example.com", "example.com", true)]
    [InlineData("user@example.com", "EXAMPLE.COM", true)]
    [InlineData("user@example.com", "other.com", false)]
    public void IsFromDomain_ShouldReturnCorrectResult(string email, string domain, bool expected)
    {
        // Arrange
        var emailAddress = new EmailAddress(email);

        // Act
        var result = emailAddress.IsFromDomain(domain);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var emailAddress = new EmailAddress("test@example.com");

        // Act
        string result = emailAddress;

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void ExplicitConversion_FromString_ShouldCreateEmailAddress()
    {
        // Arrange
        const string email = "test@example.com";

        // Act
        var emailAddress = (EmailAddress)email;

        // Assert
        emailAddress.Value.Should().Be(email);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var emailAddress = new EmailAddress("test@example.com");

        // Act
        var result = emailAddress.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void Constructor_ShouldNormalizeCase()
    {
        // Arrange
        const string email = "TEST@EXAMPLE.COM";

        // Act
        var emailAddress = new EmailAddress(email);

        // Assert
        emailAddress.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Constructor_ShouldTrimWhitespace()
    {
        // Arrange
        const string email = "  test@example.com  ";

        // Act
        var emailAddress = new EmailAddress(email);

        // Assert
        emailAddress.Value.Should().Be("test@example.com");
    }
}
