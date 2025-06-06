using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a validated email address
/// </summary>
public record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address cannot be null or empty", nameof(email));

        var trimmedEmail = email.Trim();

        if (!IsValidEmail(trimmedEmail))
            throw new ArgumentException($"Invalid email address: {email}", nameof(email));

        Value = trimmedEmail.ToLowerInvariant();
    }

    public static implicit operator string(EmailAddress email) => email.Value;

    public static explicit operator EmailAddress(string email) => new(email);

    public string GetDomain()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex > 0 ? Value.Substring(atIndex + 1) : string.Empty;
    }

    public string GetLocalPart()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex > 0 ? Value.Substring(0, atIndex) : Value;
    }

    public bool IsFromDomain(string domain)
    {
        return GetDomain().Equals(domain, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;
}
