using System.Text.Json.Serialization;

namespace MonitoringGrid.Api.Common;

/// <summary>
/// Standardized API response wrapper for consistent response format across all endpoints
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The actual data payload (null if operation failed)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of error messages (empty if operation succeeded)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Unique correlation ID for request tracking and debugging
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata about the response
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponse<T> Success(T data, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message ?? "Operation completed successfully",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse<T> Success(string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message ?? "Operation completed successfully",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failure response with error messages
    /// </summary>
    public static ApiResponse<T> Failure(string error, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = new List<string> { error },
            Message = "Operation failed",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failure response with multiple error messages
    /// </summary>
    public static ApiResponse<T> Failure(IEnumerable<string> errors, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errors.ToList(),
            Message = message ?? "Operation failed",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failure response from an exception
    /// </summary>
    public static ApiResponse<T> Failure(Exception exception, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = new List<string> { exception.Message },
            Message = message ?? "An error occurred during operation",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a validation failure response
    /// </summary>
    public static ApiResponse<T> ValidationFailure(Dictionary<string, string[]> validationErrors, Dictionary<string, object>? metadata = null)
    {
        var errors = validationErrors
            .SelectMany(kvp => kvp.Value.Select(error => $"{kvp.Key}: {error}"))
            .ToList();

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errors,
            Message = "Validation failed",
            Metadata = metadata
        };
    }
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static new ApiResponse Success(string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse
        {
            IsSuccess = true,
            Message = message ?? "Operation completed successfully",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failure response with error message
    /// </summary>
    public static new ApiResponse Failure(string error, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Errors = new List<string> { error },
            Message = "Operation failed",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failure response with multiple error messages
    /// </summary>
    public static new ApiResponse Failure(IEnumerable<string> errors, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Errors = errors.ToList(),
            Message = message ?? "Operation failed",
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failure response from an exception
    /// </summary>
    public static new ApiResponse Failure(Exception exception, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Errors = new List<string> { exception.Message },
            Message = message ?? "An error occurred during operation",
            Metadata = metadata
        };
    }
}

/// <summary>
/// Paginated API response for list operations
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates a successful paginated response
    /// </summary>
    public static PaginatedApiResponse<T> Success(
        IEnumerable<T> data, 
        int pageNumber, 
        int pageSize, 
        int totalCount, 
        string? message = null,
        Dictionary<string, object>? metadata = null)
    {
        return new PaginatedApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Message = message ?? "Data retrieved successfully",
            Metadata = metadata
        };
    }
}
