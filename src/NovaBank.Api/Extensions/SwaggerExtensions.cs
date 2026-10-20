namespace NovaBank.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        // TODO: Swagger paketini ekledikten sonra OpenAPI servislerini kaydet.
        return services;
    }

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        // TODO: Development ortamında OpenAPI belgesini ve Swagger UI'ı aç.
        return app;
    }
}
