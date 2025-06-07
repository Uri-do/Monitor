namespace MonitoringGrid.Api.Common;

/// <summary>
/// Represents the result of an operation without a return value
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error = null)
    {
        IsSuccess = isSuccess;
        Error = error ?? Error.None;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with an error
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result with error details
    /// </summary>
    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true);

    /// <summary>
    /// Creates a failed result with an error
    /// </summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    /// <summary>
    /// Creates a failed result with error details
    /// </summary>
    public static Result<T> Failure<T>(string code, string message) => new(default, false, new Error(code, message));
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
/// <typeparam name="T">The type of the return value</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error? error = null) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value if the operation was successful
    /// </summary>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException($"Cannot access value of a failed result: {Error}");
            return _value!;
        }
    }

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure<T>(error);

    /// <summary>
    /// Matches the result and executes the appropriate function
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}

/// <summary>
/// Extension methods for Result types
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
            action();
        return result;
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public static Result OnFailure(this Result result, Action<Error> action)
    {
        if (result.IsFailure)
            action(result.Error);
        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<Error> action)
    {
        if (result.IsFailure)
            action(result.Error);
        return result;
    }

    /// <summary>
    /// Maps a successful result to a new type
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        return result.IsSuccess
            ? Result.Success(mapper(result.Value))
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Binds a result to another operation that returns a result
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Matches the result and executes the appropriate function
    /// </summary>
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }
}
