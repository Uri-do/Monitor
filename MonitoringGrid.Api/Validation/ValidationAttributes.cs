using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.Validation;

/// <summary>
/// Validation attribute for positive integers
/// </summary>
public class PositiveIntegerAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Let Required handle null values
        
        if (value is int intValue)
        {
            return intValue > 0;
        }
        
        if (value is long longValue)
        {
            return longValue > 0;
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a positive integer greater than 0.";
    }
}

/// <summary>
/// Validation attribute for page size with configurable limits
/// </summary>
public class PageSizeAttribute : ValidationAttribute
{
    private readonly int _minValue;
    private readonly int _maxValue;

    public PageSizeAttribute(int minValue = 1, int maxValue = 100)
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Let Required handle null values
        
        if (value is int intValue)
        {
            return intValue >= _minValue && intValue <= _maxValue;
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between {_minValue} and {_maxValue}.";
    }
}

/// <summary>
/// Validation attribute for indicator IDs
/// </summary>
public class IndicatorIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return false;
        
        if (value is long longValue)
        {
            return longValue > 0;
        }
        
        if (value is int intValue)
        {
            return intValue > 0;
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a valid indicator ID (positive integer).";
    }
}

/// <summary>
/// Validation attribute for process IDs
/// </summary>
public class ProcessIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Process ID can be null in some contexts
        
        if (value is int intValue)
        {
            return intValue > 0;
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a valid process ID (positive integer).";
    }
}

/// <summary>
/// Validation attribute for configuration keys
/// </summary>
public class ConfigurationKeyAttribute : ValidationAttribute
{
    private readonly string[] _allowedKeys;

    public ConfigurationKeyAttribute(params string[] allowedKeys)
    {
        _allowedKeys = allowedKeys;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is string stringValue)
        {
            return _allowedKeys.Contains(stringValue, StringComparer.OrdinalIgnoreCase);
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", _allowedKeys)}.";
    }
}

/// <summary>
/// Validation attribute for worker modes
/// </summary>
public class WorkerModeAttribute : ValidationAttribute
{
    private static readonly string[] AllowedModes = { "Manual", "Integrated" };

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is string stringValue)
        {
            return AllowedModes.Contains(stringValue, StringComparer.OrdinalIgnoreCase);
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", AllowedModes)}.";
    }
}

/// <summary>
/// Validation attribute for execution context
/// </summary>
public class ExecutionContextAttribute : ValidationAttribute
{
    private static readonly string[] AllowedContexts = { "Manual", "Scheduled", "DirectTest", "System" };

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is string stringValue)
        {
            return AllowedContexts.Contains(stringValue, StringComparer.OrdinalIgnoreCase);
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", AllowedContexts)}.";
    }
}

/// <summary>
/// Validation attribute for timeout values in milliseconds
/// </summary>
public class TimeoutAttribute : ValidationAttribute
{
    private readonly int _minValue;
    private readonly int _maxValue;

    public TimeoutAttribute(int minValue = 1000, int maxValue = 300000) // 1 second to 5 minutes
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is int intValue)
        {
            return intValue >= _minValue && intValue <= _maxValue;
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between {_minValue} and {_maxValue} milliseconds.";
    }
}

/// <summary>
/// Validation attribute for search terms
/// </summary>
public class SearchTermAttribute : ValidationAttribute
{
    private readonly int _maxLength;
    private readonly int _minLength;

    public SearchTermAttribute(int minLength = 0, int maxLength = 100)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Search term can be null
        
        if (value is string stringValue)
        {
            return stringValue.Length >= _minLength && stringValue.Length <= _maxLength;
        }
        
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between {_minLength} and {_maxLength} characters.";
    }
}

/// <summary>
/// Validation attribute for boolean flags
/// </summary>
public class BooleanFlagAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Boolean can be null in some contexts
        
        return value is bool;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a boolean value (true or false).";
    }
}
