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
            var validationMsg = valEx.Errors != null && valEx.Errors.Any()
                ? string.Join(", ", valEx.Errors.Select(e => e.ErrorMessage))
                : CommonMessages.Validation.ValidationFailed;

            payload = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = validationMsg,
                data = valEx.Errors?.Select(e => new { e.PropertyName, e.ErrorMessage })
            };
        }
        else if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            statusCode = HttpStatusCode.Conflict;
            payload = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = "The record was modified or deleted by another operation. Please refresh the page and try again.",
                data = (object?)null
            };
        }
        else if (exception is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            statusCode = HttpStatusCode.BadRequest;
            var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
            string msg;
            if (innerMsg.Contains("23503") || innerMsg.IndexOf("foreign key", StringComparison.OrdinalIgnoreCase) >= 0 || innerMsg.IndexOf("reference", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                msg = CommonMessages.General.RecordInUse;
            }
            else if (innerMsg.Contains("23505") || innerMsg.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0 || innerMsg.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                msg = "A record with this information already exists.";
            }
            else
            {
                msg = "Unable to save changes to the database. Please verify your data and try again.";
            }

            payload = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = msg,
                data = (object?)null
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

            var msg = !string.IsNullOrWhiteSpace(exception.Message)
                ? exception.Message
                : exception.InnerException?.Message ?? CommonMessages.General.Failure;

            payload = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = msg,
                data = (object?)null
            };
        }

        context.Response.ContentType = CommonConstants.ContentTypes.ApplicationJson;
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
