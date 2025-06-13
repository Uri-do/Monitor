namespace MonitoringGrid.Core.Common;

/// <summary>
/// Represents an error with type and details
/// </summary>
public class Error
{
    public Error(string code, string message, ErrorType type = ErrorType.Failure)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    public static Error None => new(string.Empty, string.Empty);
    public static Error NullValue => new("Error.NullValue", "The specified result value is null.");

    public static Error NotFound(string entity, object id) => 
        new($"{entity}.NotFound", $"{entity} with ID '{id}' was not found.", ErrorType.NotFound);

    public static Error Validation(string field, string message) => 
        new($"Validation.{field}", message, ErrorType.Validation);

    public static Error Conflict(string message) => 
        new("Error.Conflict", message, ErrorType.Conflict);

    public static Error Unauthorized(string message) => 
        new("Error.Unauthorized", message, ErrorType.Unauthorized);

    public static Error Forbidden(string message) => 
        new("Error.Forbidden", message, ErrorType.Forbidden);

    public static Error BusinessRule(string rule, string message) => 
        new($"BusinessRule.{rule}", message, ErrorType.BusinessRule);

    public static Error External(string service, string message) => 
        new($"External.{service}", message, ErrorType.External);

    public static Error Timeout(string operation) => 
        new($"Timeout.{operation}", $"Operation '{operation}' timed out.", ErrorType.Timeout);

    public static Error Failure(string code, string message) => 
        new(code, message, ErrorType.Failure);

    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>
/// Types of errors for categorization
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    BusinessRule = 6,
    External = 7,
    Timeout = 8
}
