using Microsoft.AspNetCore.Diagnostics; //exception yakalama ve işleme için gerekli namespace
using Microsoft.AspNetCore.Mvc; //problem details için gerekli namespace
using NovaBank.Application.Constants; //hata kodları için gerekli namespace
using NovaBank.Application.Exceptions; //iş hataları için gerekli namespace
namespace NovaBank.Api.ExceptionHandlers;

public sealed class GlobalExceptionHandler : IExceptionHandler  //http hata döndürme
{
    public async ValueTask<bool> TryHandleAsync(  //asenkron işlemin true veya false dönmesi için valuetask sayesinde bu işlem true false donmesi zorunlu olur
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // .NET 8'de exception ayrıntılarını UseExceptionHandler middleware'i loglar.
        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "Beklenmeyen bir hata oluştu.";
        var errorCode = ErrorCodes.UnexpectedError;

        // İş hatalarında gönderilen kodu ve mesajı kullan.
        if (exception is BusinessException businessException)
        {
            errorCode = businessException.ErrorCode;
            title = businessException.Message;

            statusCode = errorCode switch   // erorrcode bakar bi statuscode seçer
            {
                ErrorCodes.UserNotFound => StatusCodes.Status404NotFound,  // => 404  olayı bu olay gerçeklesirse sagındakini seç demek
                ErrorCodes.InsufficientBalance or ErrorCodes.AccountClosed =>
                    StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = httpContext.Request.Path,  //hangi endpointte hata oluştuğunu gösterir
            Extensions =
            {
                ["errorCode"] = errorCode,
                ["traceId"] = httpContext.TraceIdentifier  //sağdaki değer sarttdır böyle yazılır
            }
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync( //json şekline çevirmek için
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        // Yanıtı oluşturduk; exception işlendi.
        return true;
    }
}
