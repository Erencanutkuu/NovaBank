using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Contracts.Responses;

namespace NovaBank.Api.Extensions;

public static class ControllerExtensions
{
    private const string InvalidValueMessage = "Geçersiz değer.";

    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                // Zorunlu alan kontrolü FluentValidation'ın işi; MVC kendi İngilizce [Required] hatasını eklemesin.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(
                    () => "İstek gövdesi zorunludur.");

                // Sorgu parametresi yanlış tipteyse (ör. page=abc) MVC'nin İngilizce mesajı yerine kullanılır.
                options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
                    (_, _) => InvalidValueMessage);
                options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(
                    _ => InvalidValueMessage);
            })
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse);

        return services;
    }

    // Controller'a ulaşamadan oluşan hatalar (bozuk JSON, yanlış tip, boş gövde).
    private static BadRequestObjectResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var failures = context.ModelState.SelectMany(entry =>
            entry.Value!.Errors.Select(error => (entry.Key, Message: GetSafeMessage(entry.Key, error.ErrorMessage))));

        return new BadRequestObjectResult(ValidationErrorResponse.Create(context.HttpContext, failures))
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    // JSON okuma hatalarının ("$" ile başlar) özgün mesajı C# tip adı ve satır bilgisi içerir; dışarı verilmez.
    private static string GetSafeMessage(string key, string message) =>
        key.StartsWith('$') || string.IsNullOrEmpty(message) ? InvalidValueMessage : message;
}
