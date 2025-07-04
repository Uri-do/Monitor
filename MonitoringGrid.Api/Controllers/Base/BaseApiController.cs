using MediatR;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.Common;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers.Base;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected readonly ILogger Logger;
    // Performance metrics removed for simplification

    protected BaseApiController(
        IMediator mediator,
        ILogger logger)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a command with standardized error handling and performance tracking
    /// </summary>
    protected async Task<IActionResult> ExecuteCommandAsync<TCommand, TResult>(
        TCommand command,
        string operationName,
        Func<TResult, IActionResult>? successAction = null) where TCommand : IRequest<Result<TResult>>
    {
        var stopwatch = Stopwatch.StartNew();
        var metricPrefix = $"{GetControllerName()}.{operationName}";

        try
        {
            Logger.LogDebug("Executing {OperationName} with command {CommandType}", operationName, typeof(TCommand).Name);

            var result = await Mediator.Send(command);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                return successAction?.Invoke(result.Value) ?? Ok(result.Value);
            }
            Logger.LogWarning("Command {CommandType} failed: {Error}", typeof(TCommand).Name, result.Error);
            return BadRequest(CreateErrorResponse(result.Error));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex, "Error executing {OperationName} with command {CommandType}", operationName, typeof(TCommand).Name);
            return StatusCode(500, CreateErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Executes a query with standardized error handling and performance tracking
    /// </summary>
    protected async Task<IActionResult> ExecuteQueryAsync<TQuery, TResult>(
        TQuery query,
        string operationName,
        Func<TResult, IActionResult>? successAction = null) where TQuery : IRequest<Result<TResult>>
    {
        var stopwatch = Stopwatch.StartNew();
        var metricPrefix = $"{GetControllerName()}.{operationName}";

        try
        {
            Logger.LogDebug("Executing {OperationName} with query {QueryType}", operationName, typeof(TQuery).Name);

            var result = await Mediator.Send(query);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                return successAction?.Invoke(result.Value) ?? Ok(result.Value);
            }
            Logger.LogWarning("Query {QueryType} failed: {Error}", typeof(TQuery).Name, result.Error);
            return BadRequest(CreateErrorResponse(result.Error));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex, "Error executing {OperationName} with query {QueryType}", operationName, typeof(TQuery).Name);
            return StatusCode(500, CreateErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Executes a command that returns no content with standardized error handling
    /// </summary>
    protected async Task<IActionResult> ExecuteCommandAsync<TCommand>(
        TCommand command,
        string operationName) where TCommand : IRequest<Result>
    {
        var stopwatch = Stopwatch.StartNew();
        var metricPrefix = $"{GetControllerName()}.{operationName}";

        try
        {
            Logger.LogDebug("Executing {OperationName} with command {CommandType}", operationName, typeof(TCommand).Name);

            var result = await Mediator.Send(command);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                return NoContent();
            }
            Logger.LogWarning("Command {CommandType} failed: {Error}", typeof(TCommand).Name, result.Error);
            return BadRequest(CreateErrorResponse(result.Error));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex, "Error executing {OperationName} with command {CommandType}", operationName, typeof(TCommand).Name);
            return StatusCode(500, CreateErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    protected virtual object CreateErrorResponse(string error)
    {
        return ApiResponse.ErrorResponse(error, new Dictionary<string, object>
        {
            ["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ["path"] = HttpContext.Request.Path.Value ?? "",
            ["method"] = HttpContext.Request.Method,
            ["timestamp"] = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a standardized error response with error code
    /// </summary>
    protected virtual object CreateErrorResponse(string error, string errorCode)
    {
        return ApiResponse.ErrorResponse(error, new Dictionary<string, object>
        {
            ["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ["path"] = HttpContext.Request.Path.Value ?? "",
            ["method"] = HttpContext.Request.Method,
            ["timestamp"] = DateTime.UtcNow,
            ["errorCode"] = errorCode
        });
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    protected virtual object CreateSuccessResponse<T>(T data, string? message = null)
    {
        return ApiResponse<T>.Success(data, message, new Dictionary<string, object>
        {
            ["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ["timestamp"] = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a standardized error response from Error object
    /// </summary>
    protected virtual object CreateErrorResponse(Error error)
    {
        return ApiResponse.ErrorResponse(error.Message, new Dictionary<string, object>
        {
            ["code"] = error.Code,
            ["type"] = error.Type.ToString(),
            ["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ["path"] = HttpContext.Request.Path.Value ?? "",
            ["method"] = HttpContext.Request.Method
        });
    }

    /// <summary>
    /// Gets the controller name for metrics
    /// </summary>
    protected virtual string GetControllerName()
    {
        var controllerName = GetType().Name;
        return controllerName.EndsWith("Controller") 
            ? controllerName[..^10].ToLowerInvariant() 
            : controllerName.ToLowerInvariant();
    }

    /// <summary>
    /// Creates a Created response with location header
    /// </summary>
    protected IActionResult CreatedWithLocation<T>(string actionName, object routeValues, T value)
    {
        return CreatedAtAction(actionName, routeValues, value);
    }

    /// <summary>
    /// Creates a standardized validation error response
    /// </summary>
    protected IActionResult ValidationError(string field, string message)
    {
        return BadRequest(new
        {
            error = "Validation failed",
            details = new Dictionary<string, string[]>
            {
                [field] = new[] { message }
            },
            timestamp = DateTime.UtcNow,
            traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Creates a standardized not found response
    /// </summary>
    protected IActionResult NotFoundWithMessage(string resourceType, object id)
    {
        return NotFound(new
        {
            error = $"{resourceType} not found",
            resourceId = id,
            timestamp = DateTime.UtcNow,
            traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Validates model state and returns appropriate response
    /// </summary>
    protected IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            return BadRequest(CreateValidationErrorResponse(errors));
        }

        return null;
    }

    /// <summary>
    /// Creates a standardized validation error response
    /// </summary>
    protected virtual object CreateValidationErrorResponse(Dictionary<string, string[]> errors)
    {
        return new
        {
            error = "Validation failed",
            details = errors,
            timestamp = DateTime.UtcNow,
            traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            path = HttpContext.Request.Path.Value ?? "",
            method = HttpContext.Request.Method
        };
    }

    /// <summary>
    /// Validates a single parameter and returns error response if invalid
    /// </summary>
    protected IActionResult? ValidateParameter<T>(T value, string parameterName, Func<T, bool> validator, string errorMessage)
    {
        if (!validator(value))
        {
            var errors = new Dictionary<string, string[]>
            {
                [parameterName] = new[] { errorMessage }
            };
            return BadRequest(CreateValidationErrorResponse(errors));
        }
        return null;
    }

    /// <summary>
    /// Validates multiple parameters and returns combined error response if any are invalid
    /// </summary>
    protected IActionResult? ValidateParameters(params (bool isValid, string parameterName, string errorMessage)[] validations)
    {
        var errors = validations
            .Where(v => !v.isValid)
            .ToDictionary(
                v => v.parameterName,
                v => new[] { v.errorMessage }
            );

        if (errors.Any())
        {
            return BadRequest(CreateValidationErrorResponse(errors));
        }

        return null;
    }
}
