using System.Text.RegularExpressions;

namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a validated phone number
/// </summary>
public record PhoneNumber
{
    private static readonly Regex PhoneRegex = new(@"^\+?[\d\s\-\(\)]{10,}$", RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRegex = new(@"\D", RegexOptions.Compiled);

    public string Value { get; }
    public string DigitsOnly { get; }

    public PhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number cannot be null or empty", nameof(phone));

        var cleanPhone = phone.Trim();
        if (!IsValidPhone(cleanPhone))
            throw new ArgumentException($"Invalid phone number: {phone}", nameof(phone));

        Value = cleanPhone;
        DigitsOnly = DigitsOnlyRegex.Replace(cleanPhone, "");
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    public static explicit operator PhoneNumber(string phone) => new(phone);

    public bool IsInternational()
    {
        return Value.StartsWith('+');
    }

    public string GetCountryCode()
    {
        if (!IsInternational())
            return string.Empty;

        // Simple country code extraction - can be enhanced
        var digits = DigitsOnly;
        if (digits.Length >= 11 && digits.StartsWith('1'))
            return "1"; // US/Canada
        if (digits.Length >= 12)
            return digits.Substring(0, 2);
        if (digits.Length >= 11)
            return digits.Substring(0, 1);

        return string.Empty;
    }

    public string GetFormattedForSms()
    {
        // Format for SMS gateway (typically email-to-SMS)
        return DigitsOnly;
    }

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        return PhoneRegex.IsMatch(phone);
    }

    public override string ToString() => Value;
}
