using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MonitoringGrid.Api.Common;

namespace MonitoringGrid.Api.Attributes;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Action filter that automatically validates model state and returns standardized error responses
/// </summary>
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var response = ValidationErrorResponse.Create(errors);
            context.Result = new BadRequestObjectResult(response);
        }

        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Validates that a string is not null, empty, or whitespace
/// </summary>
public class RequiredStringAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return !string.IsNullOrWhiteSpace(value?.ToString());
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} is required and cannot be empty or whitespace.";
    }
}

/// <summary>
/// Validates that a numeric value is within a specified range
/// </summary>
public class NumericRangeAttribute : ValidationAttribute
{
    public double Minimum { get; }
    public double Maximum { get; }
    public bool AllowNull { get; set; } = false;

    public NumericRangeAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return AllowNull;

        if (double.TryParse(value.ToString(), out var numericValue))
        {
            return numericValue >= Minimum && numericValue <= Maximum;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between {Minimum} and {Maximum}.";
    }
}

/// <summary>
/// Validates that a collection has a minimum and maximum number of items
/// </summary>
public class CollectionSizeAttribute : ValidationAttribute
{
    public int MinimumCount { get; }
    public int MaximumCount { get; }

    public CollectionSizeAttribute(int minimumCount, int maximumCount)
    {
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
    }

    public override bool IsValid(object? value)
    {
        if (value is IEnumerable<object> collection)
        {
            var count = collection.Count();
            return count >= MinimumCount && count <= MaximumCount;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must contain between {MinimumCount} and {MaximumCount} items.";
    }
}

/// <summary>
/// Validates that a string matches one of the allowed values (case-insensitive)
/// </summary>
public class AllowedValuesAttribute : ValidationAttribute
{
    public string[] AllowedValues { get; }
    public bool IgnoreCase { get; set; } = true;

    public AllowedValuesAttribute(params string[] allowedValues)
    {
        AllowedValues = allowedValues ?? Array.Empty<string>();
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Use [Required] for null validation

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return true;

        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return AllowedValues.Any(allowed => string.Equals(allowed, stringValue, comparison));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of the following values: {string.Join(", ", AllowedValues)}.";
    }
}

/// <summary>
/// Validates that a date is within a specified range
/// </summary>
public class DateRangeAttribute : ValidationAttribute
{
    public DateTime? MinimumDate { get; }
    public DateTime? MaximumDate { get; }
    public bool AllowFutureDates { get; set; } = true;
    public bool AllowPastDates { get; set; } = true;

    public DateRangeAttribute(string? minimumDate = null, string? maximumDate = null)
    {
        if (!string.IsNullOrEmpty(minimumDate) && DateTime.TryParse(minimumDate, out var min))
            MinimumDate = min;

        if (!string.IsNullOrEmpty(maximumDate) && DateTime.TryParse(maximumDate, out var max))
            MaximumDate = max;
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Use [Required] for null validation

        if (value is DateTime dateValue)
        {
            var now = DateTime.UtcNow;

            // Check future/past date restrictions
            if (!AllowFutureDates && dateValue > now)
                return false;

            if (!AllowPastDates && dateValue < now)
                return false;

            // Check explicit date range
            if (MinimumDate.HasValue && dateValue < MinimumDate.Value)
                return false;

            if (MaximumDate.HasValue && dateValue > MaximumDate.Value)
                return false;

            return true;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        var constraints = new List<string>();

        if (MinimumDate.HasValue)
            constraints.Add($"after {MinimumDate.Value:yyyy-MM-dd}");

        if (MaximumDate.HasValue)
            constraints.Add($"before {MaximumDate.Value:yyyy-MM-dd}");

        if (!AllowFutureDates)
            constraints.Add("not in the future");

        if (!AllowPastDates)
            constraints.Add("not in the past");

        var constraintText = constraints.Any() ? $" ({string.Join(", ", constraints)})" : "";
        return $"{name} must be a valid date{constraintText}.";
    }
}

/// <summary>
/// Validates that an ID value is positive
/// </summary>
public class PositiveIdAttribute : ValidationAttribute
{
    public bool AllowZero { get; set; } = false;

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Use [Required] for null validation

        if (long.TryParse(value.ToString(), out var longValue))
        {
            return AllowZero ? longValue >= 0 : longValue > 0;
        }

        if (int.TryParse(value.ToString(), out var intValue))
        {
            return AllowZero ? intValue >= 0 : intValue > 0;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        var constraint = AllowZero ? "greater than or equal to 0" : "greater than 0";
        return $"{name} must be {constraint}.";
    }
}
