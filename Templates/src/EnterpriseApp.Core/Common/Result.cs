namespace EnterpriseApp.Core.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
/// <typeparam name="T">The type of the value returned on success</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    /// <summary>
    /// Initializes a new instance of the Result class with a success value
    /// </summary>
    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    /// <summary>
    /// Initializes a new instance of the Result class with an error
    /// </summary>
    private Result(Error error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if the operation was successful
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result");

    /// <summary>
    /// Gets the error if the operation failed
    /// </summary>
    public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access error of a successful result");

    /// <summary>
    /// Creates a successful result with the specified value
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Executes one of two functions based on the result state
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    /// <summary>
    /// Executes one of two actions based on the result state
    /// </summary>
    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value!);
        else
            onFailure(_error!);
    }

    /// <summary>
    /// Maps the value to a new type if the result is successful
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess ? Result<TNew>.Success(mapper(_value!)) : Result<TNew>.Failure(_error!);
    }

    /// <summary>
    /// Binds the result to a new result-returning function if successful
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess ? binder(_value!) : Result<TNew>.Failure(_error!);
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(_error!);
        return this;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the default value
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? _value! : defaultValue;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the result of the function
    /// </summary>
    public T GetValueOrDefault(Func<Error, T> defaultValueFactory)
    {
        return IsSuccess ? _value! : defaultValueFactory(_error!);
    }
}

/// <summary>
/// Represents the result of an operation that can succeed or fail without returning a value
/// </summary>
public class Result
{
    private readonly Error? _error;

    /// <summary>
    /// Initializes a new instance of the Result class for success
    /// </summary>
    private Result()
    {
        _error = null;
        IsSuccess = true;
    }

    /// <summary>
    /// Initializes a new instance of the Result class with an error
    /// </summary>
    private Result(Error error)
    {
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the operation failed
    /// </summary>
    public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access error of a successful result");

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new();

    /// <summary>
    /// Creates a failed result with the specified error
    /// </summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a failed result with a value type
    /// </summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>
    /// Implicitly converts an error to a failed result
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Executes one of two functions based on the result state
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(_error!);
    }

    /// <summary>
    /// Executes one of two actions based on the result state
    /// </summary>
    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(_error!);
    }

    /// <summary>
    /// Maps the result to a value type if successful
    /// </summary>
    public Result<T> Map<T>(Func<T> mapper)
    {
        return IsSuccess ? Result<T>.Success(mapper()) : Result<T>.Failure(_error!);
    }

    /// <summary>
    /// Binds the result to a new result-returning function if successful
    /// </summary>
    public Result<T> Bind<T>(Func<Result<T>> binder)
    {
        return IsSuccess ? binder() : Result<T>.Failure(_error!);
    }

    /// <summary>
    /// Binds the result to a new result-returning function if successful
    /// </summary>
    public Result Bind(Func<Result> binder)
    {
        return IsSuccess ? binder() : Failure(_error!);
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
            action();
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public Result OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(_error!);
        return this;
    }

    /// <summary>
    /// Combines multiple results into a single result
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToArray();
        
        if (failures.Length == 0)
            return Success();

        var errors = failures.Select(f => f.Error).ToArray();
        return Failure(Error.Combine(errors));
    }

    /// <summary>
    /// Combines multiple results into a single result
    /// </summary>
    public static Result Combine(IEnumerable<Result> results)
    {
        return Combine(results.ToArray());
    }
}
