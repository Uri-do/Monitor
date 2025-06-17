using System.Text.RegularExpressions;

namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a validated indicator code
/// </summary>
public record IndicatorCode
{
    private static readonly Regex CodePattern = new(@"^[A-Z][A-Z0-9_]{2,49}$", RegexOptions.Compiled);
    
    public string Value { get; }

    public IndicatorCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Indicator code cannot be null or empty", nameof(code));

        var trimmedCode = code.Trim().ToUpperInvariant();

        if (!IsValidCode(trimmedCode))
            throw new ArgumentException($"Invalid indicator code: {code}. Must start with a letter, contain only letters, numbers, and underscores, and be 3-50 characters long.", nameof(code));

        Value = trimmedCode;
    }

    public static implicit operator string(IndicatorCode code) => code.Value;

    public static explicit operator IndicatorCode(string code) => new(code);

    public bool IsSystemCode()
    {
        return Value.StartsWith("SYS_") || Value.StartsWith("SYSTEM_");
    }

    public bool IsUserCode()
    {
        return Value.StartsWith("USER_") || Value.StartsWith("CUSTOM_");
    }

    public string GetPrefix()
    {
        var underscoreIndex = Value.IndexOf('_');
        return underscoreIndex > 0 ? Value.Substring(0, underscoreIndex) : Value;
    }

    private static bool IsValidCode(string code)
    {
        return CodePattern.IsMatch(code);
    }

    public override string ToString() => Value;
}
