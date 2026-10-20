namespace NovaBank.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        // TODO: ASP.NET Core health check servislerini kaydet.
        return services;
    }

    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        // TODO: /health adresini health check endpoint'ine bağla.
        // İlk adımda yalnızca API'nin çalışmasını kontrol edeceğiz.
        // PostgreSQL bağlantısının kontrolü ayrıca eklenmelidir.
        return app;
    }
}
