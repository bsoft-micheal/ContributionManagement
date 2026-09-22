namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Standard unified API response wrapper returned by all controller endpoints.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message, int statusCode = CommonStatusCodes.Status200OK)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> FailureResult(string message, int statusCode = CommonStatusCodes.Status400BadRequest)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = default
        };
    }
}

/// <summary>
/// Non-generic API response wrapper for endpoints that do not return a data payload (e.g. Delete, Logout).
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResult(string message, int statusCode = CommonStatusCodes.Status200OK)
    {
        return new ApiResponse
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = null
        };
    }

    public static new ApiResponse FailureResult(string message, int statusCode = CommonStatusCodes.Status400BadRequest)
    {
        return new ApiResponse
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = null
        };
    }
}
