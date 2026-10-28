using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NovaBank.Api.ExceptionHandlers;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(  //asenkron işlemin true veya false dönmesi için
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // .NET 8'de exception ayrıntılarını UseExceptionHandler middleware'i loglar.
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Beklenmeyen bir hata oluştu.",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = ErrorCodes.UnexpectedError,
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        // Yanıtı oluşturduk; exception işlendi.
        return true;
    }
}
