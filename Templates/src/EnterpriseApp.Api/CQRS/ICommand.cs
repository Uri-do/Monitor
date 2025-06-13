using MediatR;
using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Api.CQRS;

/// <summary>
/// Marker interface for commands that don't return a value
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Marker interface for commands that return a value
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler interface for commands that don't return a value
/// </summary>
/// <typeparam name="TCommand">The type of the command</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Handler interface for commands that return a value
/// </summary>
/// <typeparam name="TCommand">The type of the command</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Base command class with common properties
/// </summary>
public abstract class BaseCommand : ICommand
{
    /// <summary>
    /// Correlation ID for tracking the request
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID of the user executing the command
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Base command class with response and common properties
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public abstract class BaseCommand<TResponse> : ICommand<TResponse>
{
    /// <summary>
    /// Correlation ID for tracking the request
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID of the user executing the command
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Command validation interface
/// </summary>
/// <typeparam name="TCommand">The type of the command</typeparam>
public interface ICommandValidator<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Validates the command
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<Result> ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command validation interface for commands with response
/// </summary>
/// <typeparam name="TCommand">The type of the command</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommandValidator<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Validates the command
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<Result> ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command authorization interface
/// </summary>
/// <typeparam name="TCommand">The type of the command</typeparam>
public interface ICommandAuthorizer<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Authorizes the command execution
    /// </summary>
    /// <param name="command">The command to authorize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<Result> AuthorizeAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command authorization interface for commands with response
/// </summary>
/// <typeparam name="TCommand">The type of the command</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommandAuthorizer<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Authorizes the command execution
    /// </summary>
    /// <param name="command">The command to authorize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<Result> AuthorizeAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command execution context
/// </summary>
public class CommandContext
{
    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User ID executing the command
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> UserRoles { get; set; } = new();

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Command execution result
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public class CommandResult<TResponse>
{
    /// <summary>
    /// Indicates if the command was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The response data
    /// </summary>
    public TResponse? Data { get; set; }

    /// <summary>
    /// Error information if the command failed
    /// </summary>
    public Error? Error { get; set; }

    /// <summary>
    /// Execution metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static CommandResult<TResponse> Success(TResponse data, TimeSpan duration = default)
    {
        return new CommandResult<TResponse>
        {
            IsSuccess = true,
            Data = data,
            Duration = duration
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static CommandResult<TResponse> Failure(Error error, TimeSpan duration = default)
    {
        return new CommandResult<TResponse>
        {
            IsSuccess = false,
            Error = error,
            Duration = duration
        };
    }
}
