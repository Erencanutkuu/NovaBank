using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NovaBank.Infrastructure.Persistence;

namespace NovaBank.Api.Extensions;

public static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        // Varsayılan kontrol CanConnectAsync'tir: yalnızca bağlantıyı dener, iş sorgusu çalıştırmaz.
        services.AddHealthChecks()
            .AddDbContextCheck<NovaBankDbContext>("postgres", tags: [ReadyTag]);

        return services;
    }

    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        // Liveness: API ayakta mı? Hiçbir kontrol çalışmaz, veritabanı kapalıyken de Healthy döner.
        var liveness = new HealthCheckOptions { Predicate = _ => false };
        app.MapHealthChecks("/health", liveness);
        app.MapHealthChecks("/health/live", liveness);

        // Readiness: istek karşılamaya hazır mı? PostgreSQL'e erişilemezse 503 döner.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag)
        });

        return app;
    }
}
