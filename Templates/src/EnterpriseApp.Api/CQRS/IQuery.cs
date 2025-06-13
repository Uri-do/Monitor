using MediatR;
using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Api.CQRS;

/// <summary>
/// Marker interface for queries
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler interface for queries
/// </summary>
/// <typeparam name="TQuery">The type of the query</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Base query class with common properties
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public abstract class BaseQuery<TResponse> : IQuery<TResponse>
{
    /// <summary>
    /// Correlation ID for tracking the request
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID of the user executing the query
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Timestamp when the query was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the query should use cached data
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// Cache expiration time
    /// </summary>
    public TimeSpan? CacheExpiration { get; set; }
}

/// <summary>
/// Paged query base class
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public abstract class PagedQuery<TResponse> : BaseQuery<TResponse>
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Search term
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    public virtual Result ValidatePagination()
    {
        if (Page < 1)
            return Error.Validation("Page", "Page must be greater than 0");

        if (PageSize < 1 || PageSize > 100)
            return Error.Validation("PageSize", "PageSize must be between 1 and 100");

        return Result.Success();
    }
}

/// <summary>
/// Sort direction enumeration
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending order
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Descending order
    /// </summary>
    Descending = 1
}

/// <summary>
/// Query authorization interface
/// </summary>
/// <typeparam name="TQuery">The type of the query</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQueryAuthorizer<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Authorizes the query execution
    /// </summary>
    /// <param name="query">The query to authorize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<Result> AuthorizeAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Query validation interface
/// </summary>
/// <typeparam name="TQuery">The type of the query</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQueryValidator<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Validates the query
    /// </summary>
    /// <param name="query">The query to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<Result> ValidateAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Query caching interface
/// </summary>
/// <typeparam name="TQuery">The type of the query</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface IQueryCache<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Gets the cache key for the query
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>Cache key</returns>
    string GetCacheKey(TQuery query);

    /// <summary>
    /// Gets the cache expiration for the query
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>Cache expiration</returns>
    TimeSpan GetCacheExpiration(TQuery query);

    /// <summary>
    /// Determines if the query should be cached
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>True if should be cached</returns>
    bool ShouldCache(TQuery query);
}

/// <summary>
/// Query execution context
/// </summary>
public class QueryContext
{
    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User ID executing the query
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
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Indicates if caching is enabled
    /// </summary>
    public bool CachingEnabled { get; set; } = true;
}

/// <summary>
/// Query execution result
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public class QueryResult<TResponse>
{
    /// <summary>
    /// Indicates if the query was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The response data
    /// </summary>
    public TResponse? Data { get; set; }

    /// <summary>
    /// Error information if the query failed
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
    /// Indicates if the result came from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static QueryResult<TResponse> Success(TResponse data, TimeSpan duration = default, bool fromCache = false)
    {
        return new QueryResult<TResponse>
        {
            IsSuccess = true,
            Data = data,
            Duration = duration,
            FromCache = fromCache
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static QueryResult<TResponse> Failure(Error error, TimeSpan duration = default)
    {
        return new QueryResult<TResponse>
        {
            IsSuccess = false,
            Error = error,
            Duration = duration
        };
    }
}

/// <summary>
/// Query performance metrics
/// </summary>
public class QueryMetrics
{
    /// <summary>
    /// Query name
    /// </summary>
    public string QueryName { get; set; } = string.Empty;

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of records returned
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Indicates if result came from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// User ID who executed the query
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Timestamp when the query was executed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
