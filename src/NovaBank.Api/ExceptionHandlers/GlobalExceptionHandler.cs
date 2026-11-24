using FluentValidation; //doğrulama hataları için gerekli namespace
using Microsoft.AspNetCore.Diagnostics; //exception yakalama ve işleme için gerekli namespace
using Microsoft.AspNetCore.Mvc; //problem details için gerekli namespace
using NovaBank.Api.Contracts.Responses; //doğrulama hata gövdesi için gerekli namespace
using NovaBank.Application.Constants; //hata kodları için gerekli namespace
using NovaBank.Application.Exceptions; //iş hataları için gerekli namespace
namespace NovaBank.Api.ExceptionHandlers;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler  //http hata döndürme
{
    public async ValueTask<bool> TryHandleAsync(  //asenkron işlemin true veya false dönmesi için valuetask sayesinde bu işlem true false donmesi zorunlu olur
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Exception'ı burada logluyoruz; middleware'in aynı kaydı LoggingExtensions içinde eleniyor.
        // Doğrulama hataları alan bazlı gövdeyle döner.
        if (exception is ValidationException validationException)
        {
            // Alan değerleri kişisel veri olabilir; yalnızca kod loglanır.
            LogExpectedError(ErrorCodes.ValidationFailed, StatusCodes.Status400BadRequest);

            var validationProblem = ValidationErrorResponse.Create(
                httpContext,
                validationException.Errors.Select(error => (error.PropertyName, error.ErrorMessage)));

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await httpContext.Response.WriteAsJsonAsync(
                validationProblem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: cancellationToken);

            return true;
        }

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
                ErrorCodes.UserNotFound or ErrorCodes.CustomerNotFound or ErrorCodes.AccountNotFound => StatusCodes.Status404NotFound,  // => 404  olayı bu olay gerçeklesirse sagındakini seç demek
                ErrorCodes.InsufficientBalance or ErrorCodes.AccountClosed or ErrorCodes.AccountHasBalance or ErrorCodes.EmailAlreadyExists =>
                    StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            LogExpectedError(errorCode, statusCode);
        }
        else
        {
            logger.LogError(exception, "Beklenmeyen hata {ErrorCode}", errorCode);
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

    // Beklenen hatalar (iş kuralı, doğrulama) stack trace olmadan Information seviyesinde yazılır.
    private void LogExpectedError(string errorCode, int statusCode) =>
        logger.LogInformation("İstek {ErrorCode} koduyla reddedildi ({StatusCode})", errorCode, statusCode);
}
