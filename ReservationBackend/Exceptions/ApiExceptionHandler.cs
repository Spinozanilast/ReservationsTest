using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using ReservationBackend.Contracts;

namespace ReservationBackend.Exceptions;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ReservationApiException apiException:
                logger.LogWarning(
                    apiException,
                    "Request failed with {Code} ({StatusCode}): {Message}",
                    apiException.Code, apiException.StatusCode, apiException.Message);
                httpContext.Response.StatusCode = apiException.StatusCode;
                await httpContext.Response.WriteAsJsonAsync(
                    new ApiError(apiException.Code, apiException.Message),
                    cancellationToken);
                return true;

            case BadHttpRequestException:
                logger.LogWarning("Invalid request body on {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new ApiError("invalid_request_body", "The request body is missing or could not be parsed."),
                    cancellationToken);
                return true;

            case OperationCanceledException:
                // Client closed the connection before the response completed.
                logger.LogDebug("Request aborted by client on {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                return true;

            default:
                logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(
                    new ApiError("internal_error", "An unexpected error occurred."),
                    cancellationToken);
                return true;
        }
    }
}