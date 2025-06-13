namespace EnterpriseApp.Core.Common;

/// <summary>
/// Represents an error that occurred during an operation
/// </summary>
public sealed class Error : IEquatable<Error>
{
    /// <summary>
    /// Represents no error (success state)
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>
    /// Initializes a new instance of the Error class
    /// </summary>
    public Error(string code, string message, ErrorType type = ErrorType.Failure)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>
    /// Gets the error code
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error type
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Creates a validation error
    /// </summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates a not found error
    /// </summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates a conflict error
    /// </summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error
    /// </summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error
    /// </summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>
    /// Creates a business rule error
    /// </summary>
    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);

    /// <summary>
    /// Creates an external service error
    /// </summary>
    public static Error External(string code, string message) => new(code, message, ErrorType.External);

    /// <summary>
    /// Creates a general failure error
    /// </summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>
    /// Creates a critical error
    /// </summary>
    public static Error Critical(string code, string message) => new(code, message, ErrorType.Critical);

    /// <summary>
    /// Creates a validation error for a specific field
    /// </summary>
    public static Error Validation(string field, string message) => 
        Validation($"Validation.{field}", $"{field}: {message}");

    /// <summary>
    /// Creates a not found error for a specific entity
    /// </summary>
    public static Error NotFound(string entity, object id) => 
        NotFound($"{entity}.NotFound", $"{entity} with ID '{id}' was not found");

    /// <summary>
    /// Creates a conflict error for a specific entity
    /// </summary>
    public static Error Conflict(string entity, string reason) => 
        Conflict($"{entity}.Conflict", $"{entity} conflict: {reason}");

    /// <summary>
    /// Creates a business rule error for a specific rule
    /// </summary>
    public static Error BusinessRule(string rule, string message) => 
        BusinessRule($"BusinessRule.{rule}", message);

    /// <summary>
    /// Creates an external service error
    /// </summary>
    public static Error External(string service, string message) => 
        External($"External.{service}", $"External service '{service}' error: {message}");

    /// <summary>
    /// Combines multiple errors into a single error
    /// </summary>
    public static Error Combine(params Error[] errors)
    {
        if (errors.Length == 0)
            return None;

        if (errors.Length == 1)
            return errors[0];

        var codes = string.Join(", ", errors.Select(e => e.Code));
        var messages = string.Join("; ", errors.Select(e => e.Message));
        var highestType = errors.Max(e => e.Type);

        return new Error($"Combined({codes})", messages, highestType);
    }

    /// <summary>
    /// Combines multiple errors into a single error
    /// </summary>
    public static Error Combine(IEnumerable<Error> errors) => Combine(errors.ToArray());

    /// <summary>
    /// Determines whether the specified error is equal to the current error
    /// </summary>
    public bool Equals(Error? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Code == other.Code && Type == other.Type;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current error
    /// </summary>
    public override bool Equals(object? obj) => obj is Error error && Equals(error);

    /// <summary>
    /// Returns the hash code for this error
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Code, Type);

    /// <summary>
    /// Returns a string representation of the error
    /// </summary>
    public override string ToString() => $"[{Type}] {Code}: {Message}";

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(Error? left, Error? right) => 
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(Error? left, Error? right) => !(left == right);

    /// <summary>
    /// Implicit conversion from string to Error
    /// </summary>
    public static implicit operator Error(string message) => Failure("General.Failure", message);
}

/// <summary>
/// Represents the type of error
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error (success state)
    /// </summary>
    None = 0,

    /// <summary>
    /// General failure
    /// </summary>
    Failure = 1,

    /// <summary>
    /// Validation error
    /// </summary>
    Validation = 2,

    /// <summary>
    /// Resource not found
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// Conflict with current state
    /// </summary>
    Conflict = 4,

    /// <summary>
    /// Unauthorized access
    /// </summary>
    Unauthorized = 5,

    /// <summary>
    /// Forbidden operation
    /// </summary>
    Forbidden = 6,

    /// <summary>
    /// Business rule violation
    /// </summary>
    BusinessRule = 7,

    /// <summary>
    /// External service error
    /// </summary>
    External = 8,

    /// <summary>
    /// Critical system error
    /// </summary>
    Critical = 9
}

/// <summary>
/// Extension methods for Error
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Converts an error to an HTTP status code
    /// </summary>
    public static int ToHttpStatusCode(this Error error)
    {
        return error.Type switch
        {
            ErrorType.None => 200,
            ErrorType.Validation => 400,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            ErrorType.BusinessRule => 422,
            ErrorType.External => 502,
            ErrorType.Critical => 500,
            ErrorType.Failure => 500,
            _ => 500
        };
    }

    /// <summary>
    /// Checks if the error is of a specific type
    /// </summary>
    public static bool IsOfType(this Error error, ErrorType type) => error.Type == type;

    /// <summary>
    /// Checks if the error is a validation error
    /// </summary>
    public static bool IsValidationError(this Error error) => error.IsOfType(ErrorType.Validation);

    /// <summary>
    /// Checks if the error is a not found error
    /// </summary>
    public static bool IsNotFoundError(this Error error) => error.IsOfType(ErrorType.NotFound);

    /// <summary>
    /// Checks if the error is a conflict error
    /// </summary>
    public static bool IsConflictError(this Error error) => error.IsOfType(ErrorType.Conflict);

    /// <summary>
    /// Checks if the error is an authorization error
    /// </summary>
    public static bool IsAuthorizationError(this Error error) => 
        error.IsOfType(ErrorType.Unauthorized) || error.IsOfType(ErrorType.Forbidden);

    /// <summary>
    /// Checks if the error is a business rule error
    /// </summary>
    public static bool IsBusinessRuleError(this Error error) => error.IsOfType(ErrorType.BusinessRule);

    /// <summary>
    /// Checks if the error is a critical error
    /// </summary>
    public static bool IsCriticalError(this Error error) => error.IsOfType(ErrorType.Critical);
}
