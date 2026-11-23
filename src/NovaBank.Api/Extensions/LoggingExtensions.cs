using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace NovaBank.Api.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddApiLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog(
            (provider, logger) => logger
                .ReadFrom.Configuration(configuration) // Seviyeler ve sink'ler appsettings içindeki "Serilog" bölümünden gelir.
                .ReadFrom.Services(provider)
                .Enrich.FromLogContext()
                .Filter.ByExcluding(IsDuplicateExceptionLog),
            preserveStaticLogger: true);

        return services;
    }

    // Her HTTP isteği için tek satır log: method, path, status ve süre. Sorgu dizesi ve gövde yazılmaz.
    public static WebApplication UseApiRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();

            // RequestId, cevap gövdesindeki traceId ile aynı değerdir.
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
        });

        return app;
    }

    // Middleware'in "unhandled exception" kaydı (EventId 1). Aynı hatayı GlobalExceptionHandler zaten logluyor;
    // middleware'in diğer uyarıları (ör. handler'ın kendisi hata verirse) korunur.
    private static bool IsDuplicateExceptionLog(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out var source) &&
        source is ScalarValue { Value: string name } &&
        name == typeof(ExceptionHandlerMiddleware).FullName &&
        logEvent.Properties.TryGetValue("EventId", out var eventId) &&
        eventId is StructureValue structure &&
        structure.Properties.Any(property => property is { Name: "Id", Value: ScalarValue { Value: 1 } });
}
