using System.Net;
using System.Text.Json;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, CommonLogMessages.General.UnhandledExceptionPath, context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        object payload;

        if (exception is FluentValidation.ValidationException valEx)
        {
            statusCode = HttpStatusCode.BadRequest;
            payload = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = CommonMessages.Validation.ValidationFailed,
                data = valEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            };
        }
        else
        {
            statusCode = exception switch
            {
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.BadRequest,
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            payload = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = exception.Message,
                data = (object?)null
            };
        }

        context.Response.ContentType = CommonConstants.ContentTypes.ApplicationJson;
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
