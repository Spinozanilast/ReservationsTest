using Microsoft.AspNetCore.Diagnostics;
using ReservationBackend.Contracts;

namespace ReservationBackend.Exceptions;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ReservationApiException apiException)
            return false;

        httpContext.Response.StatusCode = apiException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiError(apiException.Code, apiException.Message),
            cancellationToken);

        return true;
    }
}