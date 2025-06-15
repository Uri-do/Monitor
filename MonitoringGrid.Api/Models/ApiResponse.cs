using System.Diagnostics;

namespace MonitoringGrid.Api.Models;

/// <summary>
/// Standardized API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object>? ErrorDetails { get; set; }

    /// <summary>
    /// Response metadata
    /// </summary>
    public ResponseMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Metadata = new ResponseMetadata
            {
                Message = message,
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id
            }
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string error, Dictionary<string, object>? errorDetails = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            ErrorDetails = errorDetails,
            Metadata = new ResponseMetadata
            {
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id
            }
        };
    }
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error details if operation failed
    /// </summary>
    public Dictionary<string, object>? ErrorDetails { get; set; }

    /// <summary>
    /// Response metadata
    /// </summary>
    public ResponseMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Metadata = new ResponseMetadata
            {
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id
            }
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse ErrorResponse(string message, Dictionary<string, object>? errorDetails = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails,
            Metadata = new ResponseMetadata
            {
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id
            }
        };
    }
}

/// <summary>
/// Response metadata
/// </summary>
public class ResponseMetadata
{
    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Trace ID for request correlation
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Optional message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Response duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public string? Version { get; set; } = "1.0";
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public class PaginatedResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Pagination information
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();

    /// <summary>
    /// Creates a successful paginated response
    /// </summary>
    public static PaginatedResponse<T> SuccessResponse(
        IEnumerable<T> data,
        int page,
        int pageSize,
        int totalCount,
        string? message = null)
    {
        return new PaginatedResponse<T>
        {
            Success = true,
            Data = data,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            },
            Metadata = new ResponseMetadata
            {
                Message = message,
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id
            }
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Validation error response
/// </summary>
public class ValidationErrorResponse : ApiResponse
{
    /// <summary>
    /// Field-specific validation errors
    /// </summary>
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ValidationErrorResponse Create(Dictionary<string, string[]> validationErrors)
    {
        return new ValidationErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            ValidationErrors = validationErrors,
            Metadata = new ResponseMetadata
            {
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id
            }
        };
    }
}
