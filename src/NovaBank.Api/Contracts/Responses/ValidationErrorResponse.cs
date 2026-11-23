using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NovaBank.Application.Constants;

namespace NovaBank.Api.Contracts.Responses;

// Doğrulama hatalarının ortak gövdesi: ProblemDetails + errors { alan: [mesajlar] }.
// FluentValidation ve MVC model binding hataları aynı gövdeyi kullanır.
public static class ValidationErrorResponse
{
    public static ValidationProblemDetails Create(
        HttpContext httpContext,
        IEnumerable<(string Field, string Message)> failures)
    {
        var errors = failures
            .GroupBy(failure => ToJsonPath(failure.Field))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.Message).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Gönderilen bilgiler geçersiz.",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = ErrorCodes.ValidationFailed,
                ["traceId"] = httpContext.TraceIdentifier
            }
        };
    }

    // "Address.City" -> "address.city". JsonNamingPolicy kültürden bağımsızdır (Türkçe I/ı sorunu olmaz).
    private static string ToJsonPath(string field)
    {
        // Alana bağlı olmayan hatalar (boş veya bozuk gövde).
        if (field is "" or "$")
        {
            return "body";
        }

        // System.Text.Json alan yolunu "$.id" biçiminde verir.
        var path = field.StartsWith("$.", StringComparison.Ordinal) ? field[2..] : field;

        return string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}
