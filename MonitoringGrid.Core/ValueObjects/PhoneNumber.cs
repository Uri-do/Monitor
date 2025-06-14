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

    /// <summary>
    /// Determines if this is likely a mobile number based on country code and length
    /// </summary>
    public bool IsMobileNumber()
    {
        var countryCode = GetCountryCode();
        var nationalNumber = GetNationalNumber();

        return countryCode switch
        {
            "1" => nationalNumber.Length == 10, // US/Canada
            "44" => nationalNumber.StartsWith("7"), // UK mobile starts with 7
            "49" => nationalNumber.StartsWith("1"), // Germany mobile starts with 1
            "33" => nationalNumber.StartsWith("6") || nationalNumber.StartsWith("7"), // France mobile
            "972" => nationalNumber.StartsWith("5"), // Israel mobile starts with 5
            _ => nationalNumber.Length >= 9 // Default assumption for mobile
        };
    }

    /// <summary>
    /// Gets the national number (without country code)
    /// </summary>
    public string GetNationalNumber()
    {
        var countryCode = GetCountryCode();
        if (!string.IsNullOrEmpty(countryCode))
        {
            return DigitsOnly.Substring(countryCode.Length);
        }
        return DigitsOnly;
    }

    /// <summary>
    /// Gets the SMS capability of this phone number
    /// </summary>
    public bool CanReceiveSms()
    {
        return IsMobileNumber();
    }

    /// <summary>
    /// Gets the priority level for SMS delivery based on country code
    /// </summary>
    public int GetSmsDeliveryPriority()
    {
        var countryCode = GetCountryCode();

        return countryCode switch
        {
            "1" => 1,   // US/Canada - highest priority
            "44" => 1,  // UK - highest priority
            "49" => 2,  // Germany - high priority
            "33" => 2,  // France - high priority
            "972" => 1, // Israel - highest priority
            _ => 3      // Other countries - normal priority
        };
    }

    /// <summary>
    /// Formats the phone number for display with proper spacing
    /// </summary>
    public string GetFormattedDisplay()
    {
        var countryCode = GetCountryCode();
        var nationalNumber = GetNationalNumber();

        if (!string.IsNullOrEmpty(countryCode))
        {
            return $"+{countryCode} {FormatNationalNumber(nationalNumber)}";
        }
        return FormatNationalNumber(nationalNumber);
    }

    private static string FormatNationalNumber(string nationalNumber)
    {
        // Basic formatting for common lengths
        return nationalNumber.Length switch
        {
            10 => $"({nationalNumber[..3]}) {nationalNumber[3..6]}-{nationalNumber[6..]}",
            9 => $"{nationalNumber[..3]} {nationalNumber[3..6]} {nationalNumber[6..]}",
            8 => $"{nationalNumber[..4]} {nationalNumber[4..]}",
            _ => nationalNumber
        };
    }

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        return PhoneRegex.IsMatch(phone);
    }

    public override string ToString() => GetFormattedDisplay();
}
